using System;
using Kaleidoscope.Compiler;
using Llvm.NET.Instructions;
using Llvm.NET.Values;

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
        
        public override Value CodeGen(Context ctx)
        {
            switch (this.NodeType)
            {
                case ExprType.AddExpr:
                    return ctx.InstructionBuilder.FAdd( this.Lhs.CodeGen(ctx), this.Rhs.CodeGen(ctx)).RegisterName( "addtmp" );
                case ExprType.SubtractExpr:
                    return ctx.InstructionBuilder.FSub( this.Lhs.CodeGen(ctx), this.Rhs.CodeGen(ctx)).RegisterName( "addtmp" );
                case ExprType.MultiplyExpr:
                    return ctx.InstructionBuilder.FMul( this.Lhs.CodeGen(ctx), this.Rhs.CodeGen(ctx)).RegisterName( "addtmp" );
                case ExprType.LessThanExpr:
                {
                    var tmp = ctx.InstructionBuilder.Compare( RealPredicate.UnorderedOrLessThan, this.Lhs.CodeGen(ctx), this.Rhs.CodeGen(ctx)).RegisterName( "cmptmp" );
                    return ctx.InstructionBuilder.UIToFPCast( tmp, ctx.InstructionBuilder.Context.DoubleType ).RegisterName( "booltmp" );
                }
                default:
                    throw new Exception("invalid binary operator");
            }
        }
    }
}