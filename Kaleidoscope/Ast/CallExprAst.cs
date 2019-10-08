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
            var function = ctx.GetFunction(this.Callee);

            var args = this.Arguments.Select( x => x.CodeGen( ctx ) ).ToArray( );
            return ctx.InstructionBuilder.Call( function, args ).RegisterName( "calltmp" );
        }
    }
}