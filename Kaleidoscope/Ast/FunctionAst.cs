namespace Kaleidoscope.Ast
{
    public sealed class FunctionAst : ExprAst
    {
        public FunctionAst(PrototypeAst proto, ExprAst body)
        {
            this.Proto = proto;
            this.Body = body;
            this.NodeType = ExprType.FunctionExpr;
        }

        public PrototypeAst Proto { get; }

        public ExprAst Body { get; }

        public override ExprType NodeType { get; protected set; }

        protected internal override ExprAst Accept(ExprVisitor visitor)
        {
            return visitor.VisitFunctionAst(this);
        }
    }
}