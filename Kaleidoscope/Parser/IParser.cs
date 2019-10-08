using Kaleidoscope.Ast;

namespace Kaleidoscope.Parser
{
    public interface IParser
    {
        FunctionAst ParseDefinition();

        PrototypeAst ParseExtern();

        FunctionAst ParseTopLevelExpr();
    }

}