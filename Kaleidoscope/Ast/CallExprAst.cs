using System;
using System.Collections.Generic;
using Kaleidoscope.Compiler;
using LLVMSharp;

namespace Kaleidoscope.Ast
{
    public sealed class CallExprAst : ExprAst
    {
        public CallExprAst(string callee, List<ExprAst> args)
        {
            this.Callee = callee;
            this.Arguments = args;
            this.NodeType = ExprType.CallExpr;
        }

        public string Callee { get; }

        public List<ExprAst> Arguments { get; }

        public override ExprType NodeType { get; protected set; }

        public override void CodeGen(Context ctx)
        {
            var calleeF = LLVM.GetNamedFunction(ctx.Module, this.Callee);
            if (calleeF.Pointer == IntPtr.Zero)
            {
                throw new Exception("Unknown function referenced");
            }

            if (LLVM.CountParams(calleeF) != this.Arguments.Count)
            {
                throw new Exception("Incorrect # arguments passed");
            }

            var argumentCount = (uint)this.Arguments.Count;
            var argsV = new LLVMValueRef[Math.Max(argumentCount, 1)];
            for (var i = 0; i < argumentCount; ++i)
            {
                this.Arguments[i].CodeGen(ctx);
                argsV[i] = ctx.ValueStack.Pop();
            }

            ctx.ValueStack.Push(LLVM.BuildCall(ctx.Builder, calleeF, argsV, "calltmp"));
        }
    }
}