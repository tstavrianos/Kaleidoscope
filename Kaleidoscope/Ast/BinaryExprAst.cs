using System;
using Kaleidoscope.Compiler;
using LLVMSharp;

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
        
        public override void CodeGen(Context ctx)
        {
            this.Lhs.CodeGen(ctx);
            this.Rhs.CodeGen(ctx);
            var l = ctx.ValueStack.Pop();
            var r = ctx.ValueStack.Pop();
            
            LLVMValueRef n;

            switch (this.NodeType)
            {
                case ExprType.AddExpr:
                    n = LLVM.BuildFAdd(ctx.Builder, l, r, "addtmp");
                    break;
                case ExprType.SubtractExpr:
                    n = LLVM.BuildFSub(ctx.Builder, l, r, "subtmp");
                    break;
                case ExprType.MultiplyExpr:
                    n = LLVM.BuildFMul(ctx.Builder, l, r, "multmp");
                    break;
                case ExprType.LessThanExpr:
                    // Convert bool 0/1 to double 0.0 or 1.0
                    n = LLVM.BuildUIToFP(ctx.Builder, LLVM.BuildFCmp(ctx.Builder, LLVMRealPredicate.LLVMRealULT, l, r, "cmptmp"), LLVM.DoubleType(), "booltmp");
                    break;
                default:
                    throw new Exception("invalid binary operator");
            }

            ctx.ValueStack.Push(n);
        }
    }
}