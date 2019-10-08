namespace Kaleidoscope.Ast
{
    public abstract class ExprAst
    {
        public abstract ExprType NodeType { get; protected set; }

        protected internal virtual ExprAst VisitChildren(ExprVisitor visitor)
        {
            return visitor.Visit(this);
        }

        protected internal virtual ExprAst Accept(ExprVisitor visitor)
        {
            return visitor.VisitExtension(this);
        }
    }
}