using System.Collections.Generic;

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

        protected internal override ExprAst Accept(ExprVisitor visitor)
        {
            return visitor.VisitCallExprAst(this);
        }
    }
}