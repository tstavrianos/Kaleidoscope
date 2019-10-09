namespace Kaleidoscope.Lexer
{
    public interface ILexer
    {
        int CurrentToken { get; }

        string LastIdentifier { get; }

        double LastNumber { get; }

        int GetNextToken();
    }
}