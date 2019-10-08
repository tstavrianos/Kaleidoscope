using System;
using Kaleidoscope.Compiler;
using Llvm.NET.Interop;
using Llvm.NET.Values;

namespace Kaleidoscope.Ast
{
    public sealed class FunctionAst : ExprAst
    {
        public FunctionAst(PrototypeAst proto, ExprAst body)
        {
            this.Proto = proto;
            this.Body = body;
            this.NodeType = ExprType.FunctionExpr;
        }

        public PrototypeAst Proto { get; }

        public ExprAst Body { get; }

        public override ExprType NodeType { get; protected set; }
        public override Value CodeGen(Context ctx)
        {
            var function = ctx.GetOrDeclareFunction( this.Proto );
            if( !function.IsDeclaration )
            {
                throw new Exception( $"Function {function.Name} cannot be redefined in the same module" );
            }

            try
            {
                var entryBlock = function.AppendBasicBlock( "entry" );
                ctx.InstructionBuilder.PositionAtEnd( entryBlock );
                ctx.NamedValues.Clear( );
                var index = 0;
                foreach( var param in this.Proto.Arguments )
                {
                    ctx.NamedValues[ param ] = function.Parameters[ index ];
                    ++index;
                }

                var funcReturn = this.Body.CodeGen( ctx );
                ctx.InstructionBuilder.Return( funcReturn );
                function.Verify( );
                return function;
            }
            catch( Exception )
            {
                function.EraseFromParent( );
                throw;
            }
        }
    }
}