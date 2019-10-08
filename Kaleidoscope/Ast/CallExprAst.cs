using System;
using System.Collections.Generic;
using System.Linq;
using Kaleidoscope.Compiler;
using Llvm.NET.Instructions;
using Llvm.NET.Values;

namespace Kaleidoscope.Ast
{
    public sealed class CallExprAst : ExprAst
    {
        public CallExprAst(string callee, List<ExprAst> args)
        {
            this.Callee = callee;
            this.Arguments = args;
            this.NodeType = ExprType.CallExpr;
        }

        public string Callee { get; }

        public List<ExprAst> Arguments { get; }

        public override ExprType NodeType { get; protected set; }

        public override Value CodeGen(Context ctx)
        {
            IrFunction function;

            // try for an extern function declaration
            if( ctx.FunctionDeclarations.TryGetValue( this.Callee, out var target ) )
            {
                function = ctx.GetOrDeclareFunction( target );
            }
            else
            {
                function = ctx.Module.GetFunction( this.Callee ) ?? throw new Exception( $"Definition for function {this.Callee} not found" );
            }

            var args = this.Arguments.Select( x => x.CodeGen( ctx ) ).ToArray( );
            return ctx.InstructionBuilder.Call( function, args ).RegisterName( "calltmp" );
        }
    }
}