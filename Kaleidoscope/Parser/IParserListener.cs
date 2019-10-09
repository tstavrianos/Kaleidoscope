using Kaleidoscope.Ast;

namespace Kaleidoscope.Parser
{
    public interface IParserListener
    {
        void EnterHandleDefinition(Expr.Function data);

        void ExitHandleDefinition(Expr.Function data);

        void EnterHandleExtern(Expr.Prototype data);

        void ExitHandleExtern(Expr.Prototype data);

        void EnterHandleTopLevelExpression(Expr.Function data);

        void ExitHandleTopLevelExpression(Expr.Function data);
    }
}