using Kaleidoscope.Compiler;
using LLVMSharp;

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

        public override void CodeGen(Context ctx)
        {
            ctx.ValueStack.Push(LLVM.ConstReal(LLVM.DoubleType(), this.Value));
        }
    }
}