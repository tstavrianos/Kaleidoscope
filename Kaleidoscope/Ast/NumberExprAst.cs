namespace Kaleidoscope.Ast
{
    public sealed class NumberExprAst : ExprAst
    {
        public NumberExprAst(double value)
        {
            this.Value = value;
            this.NodeType = ExprType.NumberExpr;
        }

        public double Value { get; }

        public override ExprType NodeType { get; protected set; }

        protected internal override ExprAst Accept(ExprVisitor visitor)
        {
            return visitor.VisitNumberExprAst(this);
        }
    }
}