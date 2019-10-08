using System;
using System.Collections.Generic;
using System.Linq;
using Kaleidoscope.Compiler;
using Llvm.NET.Values;

namespace Kaleidoscope.Ast
{
    public sealed class PrototypeAst : ExprAst
    {
        public PrototypeAst(string name, List<string> args)
        {
            this.Name = name;
            this.Arguments = args;
            this.NodeType = ExprType.PrototypeExpr;
        }

        public string Name { get; }

        public List<string> Arguments { get; }

        public override ExprType NodeType { get; protected set; }

        public override Value CodeGen(Context ctx)
        {
            return ctx.GetOrDeclareFunction(this);
        }
    }
}