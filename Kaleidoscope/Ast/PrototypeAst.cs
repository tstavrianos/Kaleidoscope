using System;
using System.Collections.Generic;
using Kaleidoscope.Compiler;
using LLVMSharp;

namespace Kaleidoscope.Ast
{
    public sealed class PrototypeAst : ExprAst
    {
        public PrototypeAst(string name, List<string> args)
        {
            this.Name = name;
            this.Arguments = args;
            this.NodeType = ExprType.PrototypeExpr;
        }

        public string Name { get; }

        public List<string> Arguments { get; }

        public override ExprType NodeType { get; protected set; }

        public override void CodeGen(Context ctx)
        {
            // Make the function type:  double(double,double) etc.
            var argumentCount = (uint)this.Arguments.Count;
            var arguments = new LLVMTypeRef[Math.Max(argumentCount, 1)];

            var function = LLVM.GetNamedFunction(ctx.Module, this.Name);

            // If F conflicted, there was already something named 'Name'.  If it has a
            // body, don't allow redefinition or reextern.
            if (function.Pointer != IntPtr.Zero)
            {
                // If F already has a body, reject this.
                if (LLVM.CountBasicBlocks(function) != 0)
                {
                    throw new Exception("redefinition of function.");
                }

                // If F took a different number of args, reject.
                if (LLVM.CountParams(function) != argumentCount)
                {
                    throw new Exception("redefinition of function with different # args");
                }
            }
            else
            {
                for (var i = 0; i < argumentCount; ++i)
                {
                    arguments[i] = LLVM.DoubleType();
                }

                function = LLVM.AddFunction(ctx.Module, this.Name, LLVM.FunctionType(LLVM.DoubleType(), arguments, Context.LLVMBoolFalse));
                LLVM.SetLinkage(function, LLVMLinkage.LLVMExternalLinkage);
            }

            for (var i = 0; i < argumentCount; ++i)
            {
                var argumentName = this.Arguments[i];

                var param = LLVM.GetParam(function, (uint)i);
                LLVM.SetValueName(param, argumentName);

                ctx.NamedValues[argumentName] = param;
            }

            ctx.ValueStack.Push(function);        
        }
    }
}