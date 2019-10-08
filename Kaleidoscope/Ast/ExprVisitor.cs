namespace Kaleidoscope.Ast
{
    public abstract class ExprVisitor
    {
        public virtual ExprAst Visit(ExprAst node)
        {
            return node?.Accept(this);
        }

        protected internal virtual ExprAst VisitExtension(ExprAst node)
        {
            return node.VisitChildren(this);
        }

        protected internal virtual ExprAst VisitBinaryExprAst(BinaryExprAst node)
        {
            this.Visit(node.Lhs);
            this.Visit(node.Rhs);

            return node;
        }

        protected internal virtual ExprAst VisitCallExprAst(CallExprAst node)
        {
            foreach (var argument in node.Arguments)
            {
                this.Visit(argument);
            }

            return node;
        }

        protected internal virtual ExprAst VisitFunctionAst(FunctionAst node)
        {
            this.Visit(node.Proto);
            this.Visit(node.Body);

            return node;
        }

        protected internal virtual ExprAst VisitVariableExprAst(VariableExprAst node)
        {
            return node;
        }

        protected internal virtual ExprAst VisitPrototypeAst(PrototypeAst node)
        {
            return node;
        }

        protected internal virtual ExprAst VisitNumberExprAst(NumberExprAst node)
        {
            return node;
        }
    }
}