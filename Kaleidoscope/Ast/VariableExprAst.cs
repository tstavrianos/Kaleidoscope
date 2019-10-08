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

        protected internal override ExprAst Accept(ExprVisitor visitor)
        {
            return visitor.VisitVariableExprAst(this);
        }
    }
}