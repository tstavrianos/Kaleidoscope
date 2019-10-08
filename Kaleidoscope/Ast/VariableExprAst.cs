using System;
using Kaleidoscope.Compiler;

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

        public override void CodeGen(Context ctx)
        {
            if (ctx.NamedValues.TryGetValue(this.Name, out var value))
            {
                ctx.ValueStack.Push(value);
            }
            else
            {
                throw new Exception($"Unknown variable name {this.Name}");
            }
        }
    }
}