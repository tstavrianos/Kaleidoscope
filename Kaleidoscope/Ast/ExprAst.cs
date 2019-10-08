using Kaleidoscope.Compiler;
using Llvm.NET.Values;

namespace Kaleidoscope.Ast
{
    public abstract class ExprAst
    {
        public abstract ExprType NodeType { get; protected set; }

        public abstract Value CodeGen(Context ctx);
    }
}