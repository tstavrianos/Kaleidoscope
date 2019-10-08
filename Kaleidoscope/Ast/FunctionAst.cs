using System;
using Kaleidoscope.Compiler;
using LLVMSharp;

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
        public override void CodeGen(Context ctx)
        {
            var function = LLVM.GetNamedFunction(ctx.Module, this.Proto.Name);
            if (function.Pointer == IntPtr.Zero)
            {
                this.Proto.CodeGen(ctx);
                function = ctx.ValueStack.Pop();
            }

            if (function.Pointer == IntPtr.Zero)
            {
                ctx.ValueStack.Push(default);
                return;
            }
            
            ctx.NamedValues.Clear();

            // Create a new basic block to start insertion into.
            LLVM.PositionBuilderAtEnd(ctx.Builder, LLVM.AppendBasicBlock(function, "entry"));

            try
            {
                this.Body.CodeGen(ctx);
            }
            catch (Exception)
            {
                LLVM.DeleteFunction(function);
                throw;
            }

            // Finish off the function.
            LLVM.BuildRet(ctx.Builder, ctx.ValueStack.Pop());

            // Validate the generated code, checking for consistency.
            LLVM.VerifyFunction(function, LLVMVerifierFailureAction.LLVMPrintMessageAction);

            ctx.ValueStack.Push(function);   
        }
    }
}