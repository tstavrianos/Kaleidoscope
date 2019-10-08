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
            ctx.FunctionProtos[this.Proto.Name] = this.Proto;
            var function = ctx.GetFunction(this.Proto.Name);
            if (function == null) return null;
            
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
            if (funcReturn != null)
            {
                ctx.InstructionBuilder.Return(funcReturn);
                function.Verify();
                ctx.FunctionPassManager.Run(function);
                return function;
            }

            function.EraseFromParent( );
            return null;
        }
    }
}