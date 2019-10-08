using System;

namespace Kaleidoscope.Ast
{
    public sealed class BinaryExprAst : ExprAst
    {
        public BinaryExprAst(char op, ExprAst lhs, ExprAst rhs)
        {
            switch (op)
            {
                case '+':
                    this.NodeType = ExprType.AddExpr;
                    break;
                case '-':
                    this.NodeType = ExprType.SubtractExpr;
                    break;
                case '*':
                    this.NodeType = ExprType.MultiplyExpr;
                    break;
                case '<':
                    this.NodeType = ExprType.LessThanExpr;
                    break;
                default:
                    throw new ArgumentException("op " + op + " is not a valid operator");
            }

            this.Lhs = lhs;
            this.Rhs = rhs;
        }

        public ExprAst Lhs { get; }

        public ExprAst Rhs { get; }

        public override ExprType NodeType { get; protected set; }

        protected internal override ExprAst Accept(ExprVisitor visitor)
        {
            return visitor.VisitBinaryExprAst(this);
        }
    }
}