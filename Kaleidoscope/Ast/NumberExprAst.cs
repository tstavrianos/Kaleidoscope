using Kaleidoscope.Compiler;
using Llvm.NET.Values;

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

        public override Value CodeGen(Context ctx)
        {
            return ctx.Ctx.CreateConstant( this.Value );
        }
    }
}