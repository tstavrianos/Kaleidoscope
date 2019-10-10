using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Kaleidoscope.Ast;

namespace Kaleidoscope.Compiler
{
    public sealed class IlGeneratorVisitor: Expr.IVisitor
    {
        private interface ISymbol
        {
            
        }

        private sealed class ArgSymbol : ISymbol
        {
            public ushort Index;
        }

        private sealed class VarSymbol : ISymbol
        {
            public LocalBuilder Local;
        }
        
        private ILGenerator _emitter;
        private readonly ScopeStack<ISymbol> _symbols = new ScopeStack<ISymbol>();
        internal readonly Dictionary<string, (MethodInfo methodInfo, bool isExtern)> Methods = new Dictionary<string, (MethodInfo methodInfo, bool isExtern)>();
        private readonly Session _session;
        internal bool NextIsExtern;

        public static double putchard(double d)
        {
            Console.Write((char)d);
            return 0.0;
        }
        
        public IlGeneratorVisitor(Session session)
        {
            this._session = session;
        }
       
        public void Visit(Expr.Number expr)
        {
            this._emitter.Emit(OpCodes.Ldc_R8, expr.Value);
        }

        public void Visit(Expr.Variable expr)
        {
            if (this._symbols.TryGetValue(expr.Name, out var symbol))
            {
                switch (symbol)
                {
                    case ArgSymbol a:
                        this._emitter.Emit(OpCodes.Ldarg, a.Index);
                        break;
                    case VarSymbol v:
                        this._emitter.Emit(OpCodes.Ldloc, v.Local);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
            else
            {
                throw new Exception($"Variable {expr.Name} not found");
            }
        }

        public void Visit(Expr.Unary expr)
        {
            expr.Operand.Accept(this);
            if (!this.Methods.TryGetValue($"unary{expr.Opcode}", out var method))
                throw new Exception($"Method unary{expr.Opcode} not found");
            this._emitter.EmitCall(OpCodes.Call, method.methodInfo, null);
            if (method.methodInfo.ReturnType == typeof(void))
            {
                this._emitter.Emit(OpCodes.Ldc_R8, 0.0);
            }
        }

        public void Visit(Expr.Binary expr)
        {
            if (expr.Op == '=')
            {
                if (!(expr.Lhs is Expr.Variable variable)) throw new Exception("destination of '=' must be a variable");
                expr.Rhs.Accept(this);
                if (!this._symbols.TryGetValue(variable.Name, out var symbol))
                    throw new Exception($"Variable {variable.Name} was not found");
                switch (symbol)
                {
                    case VarSymbol v:
                        this._emitter.Emit(OpCodes.Stloc, v.Local);
                        break;
                    case ArgSymbol a:
                        this._emitter.Emit(OpCodes.Starg, a.Index);
                        break;
                    default:
                        throw new NotImplementedException();
                }

                return;
            }

            expr.Lhs.Accept(this);
            expr.Rhs.Accept(this);
            var builtin = true;

            switch (expr.Op)
            {
                case '+':
                    this._emitter.Emit(OpCodes.Add);
                    break;
                case '-':
                    this._emitter.Emit(OpCodes.Sub);
                    break;
                case '*':
                    this._emitter.Emit(OpCodes.Mul);
                    break;
                case '<':
                    var endLabel = this._emitter.DefineLabel();
                    var trueLabel = this._emitter.DefineLabel();

                    this._emitter.Emit(OpCodes.Blt, trueLabel);
                    this._emitter.Emit(OpCodes.Ldc_R8, 0.0);
                    this._emitter.Emit(OpCodes.Br, endLabel);
                    this._emitter.MarkLabel(trueLabel);
                    this._emitter.Emit(OpCodes.Ldc_R8, 1.0);
                    this._emitter.MarkLabel(endLabel);
                    break;
                default:
                    builtin = false;
                    break;
            }

            if (builtin) return;
            if (!this.Methods.TryGetValue($"binary{expr.Op}", out var method)) throw new Exception($"Method binary{expr.Op} not found");

            this._emitter.EmitCall(OpCodes.Call, method.methodInfo, null);
            if (method.methodInfo.ReturnType == typeof(void))
            {
                this._emitter.Emit(OpCodes.Ldc_R8, 0.0);
            }
        }

        public void Visit(Expr.Call expr)
        {
            foreach (var arg in expr.Arguments)
            {
                arg.Accept(this);
            }

            if (!this.Methods.TryGetValue(expr.Callee, out var method))
                throw new Exception($"Method {expr.Callee} not found");
            this._emitter.Emit(OpCodes.Call, method.methodInfo);

            if (method.methodInfo.ReturnType == typeof(void))
            {
                this._emitter.Emit(OpCodes.Ldc_R8, 0.0);
            }
        }

        public void Visit(Expr.If expr)
        {
            var endLabel = this._emitter.DefineLabel();
            var elseLabel = this._emitter.DefineLabel();

            expr.Cond.Accept(this);
            this._emitter.Emit(OpCodes.Ldc_R8, 0.0);
            this._emitter.Emit(OpCodes.Beq, elseLabel);

            expr.ThenExpr.Accept(this);
            this._emitter.Emit(OpCodes.Br, endLabel);

            this._emitter.MarkLabel(elseLabel);
            expr.ElseExpr.Accept(this);
            this._emitter.MarkLabel(endLabel);        
        }

        public void Visit(Expr.For expr)
        {
            var  loopVar = this._emitter.DeclareLocal(typeof(double));
            expr.Start.Accept(this);
            this._emitter.Emit(OpCodes.Stloc, loopVar);
			
            //Add the loop variable
            using (this._symbols.EnterScope())
            {
                this._symbols[expr.VarName] = new VarSymbol {Local = loopVar};

                var loopStart = this._emitter.DefineLabel();
                var end = this._emitter.DefineLabel();

                //Emit the loop condition
                this._emitter.MarkLabel(loopStart);
                expr.End.Accept(this);
                this._emitter.Emit(OpCodes.Ldc_R8, 0.0);
                this._emitter.Emit(OpCodes.Beq, end);

                //The body
                expr.Body.Accept(this);

                //Pop any value returned from the body
                this._emitter.Emit(OpCodes.Pop);

                //The step
                this._emitter.Emit(OpCodes.Ldloc, loopVar);
                expr.Step.Accept(this);
                this._emitter.Emit(OpCodes.Add);
                this._emitter.Emit(OpCodes.Stloc, loopVar);
                this._emitter.Emit(OpCodes.Br, loopStart);
                this._emitter.MarkLabel(end);
                this._emitter.Emit(OpCodes.Ldc_R8, 0.0);
            }
        }

        public void Visit(Expr._VarName expr)
        {
        }

        public void Visit(Expr.Var expr)
        {
            using (this._symbols.EnterScope())
            {
                foreach (var t in expr.VarNames)
                {
                    var local = this._emitter.DeclareLocal(typeof(double));
                    this._symbols[t.Name] = new VarSymbol{Local = local};
                    if (t.Expression != null)
                    {
                        t.Expression.Accept(this);
                    }
                    else
                    {
                        this._emitter.Emit(OpCodes.Ldc_R8, 0.0);
                    }
                    this._emitter.Emit(OpCodes.Stloc, local);
                }

                expr.Body.Accept(this);
            }
        }

        public void Visit(Expr.Prototype expr)
        {
            if (this.Methods.ContainsKey(expr.Name)) return;

            var method =
                typeof(IlGeneratorVisitor).GetMethod(expr.Name, expr.Arguments.Select(x => typeof(double)).ToArray());

            if (method != null)
            {
                this.Methods.Add(expr.Name, (method, true));
            }
            else if (this.NextIsExtern)
            {
                Console.WriteLine($"prototype {expr.Name} was not found in the bindings");
            }

            this.NextIsExtern = false;
        }

        public void Visit(Expr.Function expr)
        {
            expr.Proto.Accept(this);
            if (this.Methods.TryGetValue(expr.Proto.Name, out var p) && !p.isExtern)
            {
                throw new Exception($"{expr.Proto.Name} has already been defined");
            }
            
            var args = expr.Proto.Arguments.Select(x => typeof(double)).ToArray();
            
            var function = new DynamicMethod(expr.Proto.Name, typeof(double), args);
            var generator = function.GetILGenerator();
            using (this._symbols.EnterScope())
            {
                for (ushort i = 0; i < expr.Proto.Arguments.Length; i++)
                {
                    this._symbols[expr.Proto.Arguments[i]] = new ArgSymbol{Index = i};
                }

                this.Methods[expr.Proto.Name] = (function, false);
                var prevEmit = this._emitter;
                this._emitter = generator;
                expr.Body.Accept(this);
                generator.Emit(OpCodes.Ret);
                this._emitter = prevEmit;
                
                if (expr.Proto.IsOperator && expr.Proto.Arguments.Length == 2)
                {
                    this._session.AddBinOpPrecedence(expr.Proto.Name[^1], expr.Proto.Precedence);
                }
            }
        }
    }
}