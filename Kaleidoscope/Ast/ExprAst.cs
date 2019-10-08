using Kaleidoscope.Compiler;

namespace Kaleidoscope.Ast
{
    public abstract class ExprAst
    {
        public abstract ExprType NodeType { get; protected set; }

        public abstract void CodeGen(Context ctx);
    }
}