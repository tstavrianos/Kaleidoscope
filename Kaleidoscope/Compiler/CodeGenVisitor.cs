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
        private readonly ScopeStack<Value> _namedValues = new ScopeStack<Value>( );
        internal BitcodeModule Module;
        private FunctionPassManager _functionPassManager;
        private readonly bool _disableOptimizations;
        internal readonly KaleidoscopeJit Jit = new KaleidoscopeJit( );
        internal readonly IDictionary<string, Expr.Prototype> FunctionProtos = new Dictionary<string, Expr.Prototype>();

        public CodeGenVisitor(bool disableOptimizations)
        {
            this._disableOptimizations = disableOptimizations;
            this._ctx = new Context( );
            this.InitializeModuleAndPassManager( );
            this._instructionBuilder = new InstructionBuilder( this._ctx );
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
            this.Module.Layout = this.Jit.TargetMachine.TargetData;
            this._functionPassManager = new FunctionPassManager(this.Module );

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
            var l = expr.Lhs.Accept(this);
            var r = expr.Rhs.Accept(this);

            Value n;
            switch (expr.Op)
            {
                case '+':
                    n = this._instructionBuilder.FAdd( l, r).RegisterName( "addtmp" );
                    break;
                case '-':
                    n = this._instructionBuilder.FSub( l, r).RegisterName( "subtmp" );
                    break;
                case '*':
                    n = this._instructionBuilder.FMul( l, r).RegisterName( "multmp" );
                    break;
                case '<':
                {
                    var tmp = this._instructionBuilder.Compare( RealPredicate.UnorderedOrLessThan, l, r).RegisterName( "cmptmp" );
                    n = this._instructionBuilder.UIToFPCast( tmp, this._instructionBuilder.Context.DoubleType ).RegisterName( "booltmp" );
                    break;
                }
                default:
                    throw new Exception("invalid binary operator");
            }
            return n;
        }

        public Value Visit(Expr.Call expr)
        {
            var function = this.GetFunction(expr.Callee);

            var args = expr.Arguments.Select( x => x.Accept(this)).ToArray( );
            return this._instructionBuilder.Call(function, args).RegisterName("calltmp");
        }

        public Value Visit(Expr.Number expr)
        {
            return this._ctx.CreateConstant(expr.Value);
        }

        public Value Visit(Expr.Variable expr)
        {
            if (!this._namedValues.TryGetValue(expr.Name, out var value))
                throw new Exception($"Unknown variable name {expr.Name}");
            return value;

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

            try
            {
                var entryBlock = function.AppendBasicBlock("entry");
                this._instructionBuilder.PositionAtEnd(entryBlock);
                using (this._namedValues.EnterScope())
                {

                    var index = 0;
                    foreach (var param in stmt.Proto.Arguments)
                    {
                        this._namedValues[param] = function.Parameters[index];
                        ++index;
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
            var startVal = expr.Start.Accept(this);
            if (startVal == null) return null;

            var function = this._instructionBuilder.InsertBlock.ContainingFunction;
            var preHeaderBb = this._instructionBuilder.InsertBlock;
            var loopBb = function.AppendBasicBlock("loop");

            this._instructionBuilder.Branch(loopBb);
            this._instructionBuilder.PositionAtEnd( loopBb );

            var variable = this._instructionBuilder.PhiNode(function.Context.DoubleType).RegisterName(expr.VarName);
            variable.AddIncoming( startVal, preHeaderBb );
            using (this._namedValues.EnterScope())
            {
                this._namedValues[expr.VarName] = variable;

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

                var nextVar = this._instructionBuilder.FAdd(variable, stepVal);

                var endCond = expr.End.Accept(this);
                if (endCond == null) return null;

                endCond = this._instructionBuilder
                    .Compare(RealPredicate.OrderedAndNotEqual, endCond, this._ctx.CreateConstant(0.0))
                    .RegisterName("loopcond");

                var loopEndBb = this._instructionBuilder.InsertBlock;
                var afterBb = function.AppendBasicBlock("afterloop");
                this._instructionBuilder.Branch(endCond, loopBb, afterBb);
                this._instructionBuilder.PositionAtEnd(afterBb);
                variable.AddIncoming(nextVar, loopEndBb);

                return this._ctx.DoubleType.GetNullValue();
            }
        }
    }
}