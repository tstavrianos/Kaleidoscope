using System.Collections.Generic;

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

        protected internal override ExprAst Accept(ExprVisitor visitor)
        {
            return visitor.VisitPrototypeAst(this);
        }
    }
}