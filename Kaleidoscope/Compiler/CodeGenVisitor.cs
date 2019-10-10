using System;
using System.Collections.Generic;
using System.Linq;
using Kaleidoscope.Ast;
using Llvm.NET;
using Llvm.NET.Instructions;
using Llvm.NET.Transforms;
using Llvm.NET.Values;

namespace Kaleidoscope.Compiler
{
    public sealed class CodeGenVisitor: Expr.IVisitor<Value>, IDisposable
    {
        private readonly Context _ctx;
        private readonly InstructionBuilder _instructionBuilder;
        private readonly ScopeStack<Alloca> _namedValues = new ScopeStack<Alloca>( );
        internal BitcodeModule Module;
        private FunctionPassManager _functionPassManager;
        private readonly bool _disableOptimizations;
        internal readonly KaleidoscopeJit Jit = new KaleidoscopeJit( );
        internal readonly IDictionary<string, Expr.Prototype> FunctionProtos = new Dictionary<string, Expr.Prototype>();
        private readonly Session _session;
        private readonly TargetMachine _machine;

        public CodeGenVisitor(bool disableOptimizations, Session session, TargetMachine machine)
        {
            this._disableOptimizations = disableOptimizations;
            this._ctx = new Context( );
            this._machine = machine;
            this.InitializeModuleAndPassManager( );
            this._instructionBuilder = new InstructionBuilder( this._ctx );
            this._session = session;
        }

        private IrFunction GetOrDeclareFunction(Expr.Prototype prototype )
        {
            var function = this.Module.GetFunction( prototype.Name );
            if( function != null )
            {
                return function;
            }

            var llvmSignature = this._ctx.GetFunctionType( this._ctx.DoubleType, prototype.Arguments.Select( _ => this._ctx.DoubleType ) );
            var retVal = this.Module.AddFunction( prototype.Name, llvmSignature );

            int index = 0;
            foreach( var argId in prototype.Arguments )
            {
                retVal.Parameters[ index ].Name = argId;
                ++index;
            }

            return retVal;
        }

        public IrFunction GetFunction(string name)
        {
            var ret = this.Module.GetFunction(name);
            if (ret != null) return ret;

            if (!this.FunctionProtos.TryGetValue(name, out var proto)) return null;
            return (IrFunction)proto.Accept(this);
        }

        internal void InitializeModuleAndPassManager( )
        {
            this.Module = this._ctx.CreateBitcodeModule( );
            this.Module.TargetTriple = this._machine.Triple;
            this.Module.Layout = this._machine.TargetData;
            this._functionPassManager = new FunctionPassManager( this.Module )
                .AddPromoteMemoryToRegisterPass( );

            if( !this._disableOptimizations )
            {
                this._functionPassManager.AddInstructionCombiningPass( )
                    .AddReassociatePass( )
                    .AddGVNPass( )
                    .AddCFGSimplificationPass( );
            }

            this._functionPassManager.Initialize( );
        }


        public void Dispose()
        {
            this._ctx?.Dispose();
            this._functionPassManager?.Dispose();
            this.Jit?.Dispose();
            this.Module?.Dispose();
        }
        
        public Value Visit(Expr.Binary expr)
        {
            if (expr.Op == '=')
            {
                if (!(expr.Lhs is Expr.Variable lhse))
                {
                    Console.WriteLine("destination of '=' must be a variable");
                    return null;
                }
                var val = expr.Rhs.Accept(this);
                if (val == null) return null;

                if(!this._namedValues.TryGetValue(lhse.Name, out var variable))
                {
                    Console.WriteLine("Unknown variable name");
                    return null;
                }

                this._instructionBuilder.Store(val, variable);
                return val;
            }
            
            var l = expr.Lhs.Accept(this);
            var r = expr.Rhs.Accept(this);
            if (l == null || r == null) return null;

            switch (expr.Op)
            {
                case '+':
                    return this._instructionBuilder.FAdd( l, r).RegisterName( "addtmp" );
                case '-':
                    return this._instructionBuilder.FSub( l, r).RegisterName( "subtmp" );
                case '*':
                    return this._instructionBuilder.FMul( l, r).RegisterName( "multmp" );
                case '<':
                {
                    var tmp = this._instructionBuilder.Compare( RealPredicate.UnorderedOrLessThan, l, r).RegisterName( "cmptmp" );
                    return this._instructionBuilder.UIToFPCast( tmp, this._instructionBuilder.Context.DoubleType ).RegisterName( "booltmp" );
                }
            }

            var f = this.GetFunction("binary" + expr.Op);
            if (f == null)
            {
                throw new Exception("invalid binary operator");
            }

            return this._instructionBuilder.Call(f, l, r).RegisterName("binop");
        }

        public Value Visit(Expr.Call expr)
        {
            var function = this.GetFunction(expr.Callee);

            var args = new Value[Math.Max(expr.Arguments.Length, 0)];
            for (var i = 0; i < args.Length; i++)
            {
                args[i] = expr.Arguments[i].Accept(this);
            }
            
            return this._instructionBuilder.Call(function, args).RegisterName("calltmp");
        }

        public Value Visit(Expr.Number expr)
        {
            return this._ctx.CreateConstant(expr.Value);
        }

        public Value Visit(Expr.Variable expr)
        {
            var value = this.LookupVariable(expr.Name);
            return this._instructionBuilder.Load(value.ElementType, value).RegisterName(expr.Name);
        }

        public Value Visit(Expr.Var expr)
        {
            using (this._namedValues.EnterScope())
            {
                var function = this._instructionBuilder.InsertBlock.ContainingFunction;
                foreach (var t in expr.VarNames)
                {
                    var varName = t.Name;
                    var init = t.Expression;

                    Value initVal;
                    if (init != null)
                    {
                        initVal = init.Accept(this);
                        if (initVal == null) return null;
                    }
                    else
                    {
                        initVal = this._ctx.CreateConstant(0.0);
                    }

                    var alloca = this._instructionBuilder.Alloca(function.Context.DoubleType).RegisterName(varName);
                    this._instructionBuilder.Store(initVal, alloca);
                    this._namedValues[varName] = alloca;
                }

                var body = expr.Body.Accept(this);

                return body;
            }
        }

        public Value Visit(Expr.Prototype stmt)
        {
            return this.GetOrDeclareFunction(stmt);
        }

        public Value Visit(Expr.Function stmt)
        {
            this.FunctionProtos[stmt.Proto.Name] = stmt.Proto;
            var function = this.GetFunction(stmt.Proto.Name);
            if (function == null)
            {
                return null;
            }

            if (stmt.Proto.IsOperator && stmt.Proto.Arguments.Length == 2)
            {
                this._session.AddBinOpPrecedence(stmt.Proto.Name[^1], stmt.Proto.Precedence);
            }

            try
            {
                var entryBlock = function.AppendBasicBlock("entry");
                this._instructionBuilder.PositionAtEnd(entryBlock);
                using (this._namedValues.EnterScope())
                {
                    foreach (var arg in function.Parameters)
                    {
                        var alloca = this._instructionBuilder.Alloca(function.Context.DoubleType).RegisterName(arg.Name);
                        this._instructionBuilder.Store(arg, alloca);
                        this._namedValues[arg.Name] = alloca;
                    }

                    var funcReturn = stmt.Body.Accept(this);
                    this._instructionBuilder.Return(funcReturn);
                    function.Verify();
                    this._functionPassManager.Run(function);
                    return function;
                }
            }
            catch
            {
                function.EraseFromParent();
                throw ;
            }
        }

        public Value Visit(Expr.If expr)
        {
            var condV  = expr.Cond.Accept(this);
            if (condV  == null) return null;

            condV = this._instructionBuilder.Compare(RealPredicate.OrderedAndNotEqual, condV , this._ctx.CreateConstant(0.0)).RegisterName("ifcond");
            
            var theFunction = this._instructionBuilder.InsertBlock.ContainingFunction;
            var thenBb  = theFunction.AppendBasicBlock( "then" );
            var elseBb  = theFunction.AppendBasicBlock( "else" );
            var mergeBb  = theFunction.AppendBasicBlock( "ifcont" );
            this._instructionBuilder.Branch( condV, thenBb , elseBb  );
            
            this._instructionBuilder.PositionAtEnd( thenBb  );
            var thenV  = expr.ThenExpr.Accept( this );
            if( thenV  == null )
            {
                return null;
            }

            this._instructionBuilder.Branch( mergeBb  );

            // capture the insert in case generating else adds new blocks
            thenBb   = this._instructionBuilder.InsertBlock;

            // generate else block
            this._instructionBuilder.PositionAtEnd( elseBb  );
            var elseV  = expr.ElseExpr.Accept( this );
            if( elseV  == null )
            {
                return null;
            }
            
            this._instructionBuilder.Branch( mergeBb  );
            elseBb = this._instructionBuilder.InsertBlock;

            // generate continue block
            this._instructionBuilder.PositionAtEnd( mergeBb );
            var pn = this._instructionBuilder.PhiNode( theFunction.Context.DoubleType )
                .RegisterName( "ifresult" );

            pn.AddIncoming(( thenV , thenBb  ),( elseV , elseBb ));
            return pn;
        }

        public Value Visit(Expr.For expr)
        {
            var function = this._instructionBuilder.InsertBlock.ContainingFunction;
            var alloca = this._instructionBuilder.Alloca(function.Context.DoubleType).RegisterName(expr.VarName);
            var startVal = expr.Start.Accept(this);
            if (startVal == null) return null;
            this._instructionBuilder.Store(startVal, alloca);
            
            var loopBb = function.AppendBasicBlock("loop");

            this._instructionBuilder.Branch(loopBb);
            this._instructionBuilder.PositionAtEnd( loopBb );

            using (this._namedValues.EnterScope())
            {
                this._namedValues[expr.VarName] = alloca;

                var body = expr.Body.Accept(this);
                if (body == null) return null;

                Value stepVal = null;
                if (expr.Step != null)
                {
                    stepVal = expr.Step.Accept(this);
                    if (stepVal == null) return null;
                }
                else
                {
                    stepVal = this._ctx.CreateConstant(1.0);
                }

                var endCond = expr.End.Accept(this);
                if (endCond == null) return null;
                var curVal = this._instructionBuilder.Load(function.Context.DoubleType, alloca);
                var nextVar = this._instructionBuilder.FAdd(curVal, stepVal).RegisterName("nextvar");
                this._instructionBuilder.Store(nextVar, alloca);

                endCond = this._instructionBuilder
                    .Compare(RealPredicate.OrderedAndNotEqual, endCond, this._ctx.CreateConstant(0.0))
                    .RegisterName("loopcond");

                var afterBb = function.AppendBasicBlock("afterloop");
                this._instructionBuilder.Branch(endCond, loopBb, afterBb);
                this._instructionBuilder.PositionAtEnd(afterBb);

                return this._ctx.DoubleType.GetNullValue();
            }
        }

        public Value Visit(Expr._VarName expr) => null;

        public Value Visit(Expr.Unary expr)
        {
            var op = expr.Operand.Accept(this);
            if (op == null) return null;

            var f = this.GetFunction($"unary{expr.Opcode}");
            if (f != null) return this._instructionBuilder.Call(f, op).RegisterName("unop");
            Console.WriteLine("Unknown unary operator");
            return null;
        }
        
        private Alloca LookupVariable( string name )
        {
            if( !this._namedValues.TryGetValue( name, out var value ) )
            {
                // Source input is validated by the parser and AstBuilder, therefore
                // this is the result of an internal error in the generator rather
                // then some sort of user error.
                throw new Exception( $"ICE: Unknown variable name: {name}" );
            }

            return value;
        }

    }
}