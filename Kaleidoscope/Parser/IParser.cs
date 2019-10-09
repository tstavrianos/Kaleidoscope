namespace Kaleidoscope.Parser
{
    public interface IParser
    {
        void HandleDefinition();

        void HandleExtern();

        void HandleTopLevelExpression();
    }
}