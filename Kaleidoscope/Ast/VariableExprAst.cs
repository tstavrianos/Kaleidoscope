using System;
using Kaleidoscope.Compiler;
using Llvm.NET.Values;

namespace Kaleidoscope.Ast
{
    public sealed class VariableExprAst : ExprAst
    {
        public VariableExprAst(string name)
        {
            this.Name = name;
            this.NodeType = ExprType.VariableExpr;
        }

        public string Name { get; }

        public override ExprType NodeType { get; protected set; }

        public override Value CodeGen(Context ctx)
        {
            if (ctx.NamedValues.TryGetValue(this.Name, out var value))
            {
                return value;
            }
            else
            {
                throw new Exception($"Unknown variable name {this.Name}");
            }
        }
    }
}