using System;
using Kaleidoscope.Ast;
using Kaleidoscope.Parser;
using Llvm.NET.JIT;

namespace Kaleidoscope.Compiler
{
    public sealed class CodeGenParserListener: IParserListener
    {
        private readonly CodeGenVisitor _visitor;

        public CodeGenParserListener(CodeGenVisitor visitor)
        {
            this._visitor = visitor;
        }

        public void EnterHandleDefinition(Expr.Function data)
        {
        }

        public void ExitHandleDefinition(Expr.Function data)
        {
            var value = this._visitor.Visit(data);
            if (value == null) return;
            this._visitor.Jit.AddEagerlyCompiledModule(this._visitor.Module);
            this._visitor.InitializeModuleAndPassManager();
        }

        public void EnterHandleExtern(Expr.Prototype data)
        {
        }

        public void ExitHandleExtern(Expr.Prototype data)
        {
            var value = this._visitor.Visit(data);
            if (value == null) return;
            this._visitor.FunctionProtos[data.Name] = data;
        }

        public void EnterHandleTopLevelExpression(Expr.Function data)
        {
        }

        public void ExitHandleTopLevelExpression(Expr.Function data)
        {
            var value = this._visitor.Visit(data);
            if (value == null) return;
            var jitHandle = this._visitor.Jit.AddEagerlyCompiledModule(this._visitor.Module);
            this._visitor.InitializeModuleAndPassManager();
            var nativeFunc = this._visitor.Jit.GetFunctionDelegate<KaleidoscopeJit.CallbackHandler0>(data.Proto.Name);
            Console.WriteLine($"Evaluated to {nativeFunc()}");
            this._visitor.Jit.RemoveModule(jitHandle);
        }
    }
}