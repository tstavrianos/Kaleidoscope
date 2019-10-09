using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Kaleidoscope.Lexer
{
    public sealed class DefaultLexer: ILexer
    {
        private const int Eof = -1;

        private readonly TextReader _reader;

        private readonly StringBuilder _identifierBuilder = new StringBuilder();

        private readonly StringBuilder _numberBuilder = new StringBuilder();

        private int _lastChar = ' ';

        public int CurrentToken { get; private set; }

        public string LastIdentifier { get; private set; }

        public double LastNumber { get; private set; }

        public DefaultLexer(TextReader reader)
        {
            this._reader = reader;
        }

        public int GetNextToken()
        {
            // Skip any whitespace.
            while (char.IsWhiteSpace((char)this._lastChar))
            {
                this._lastChar = this._reader.Read();
            }

            if (char.IsLetter((char)this._lastChar)) // identifier: [a-zA-Z][a-zA-Z0-9]*
            {
                this._identifierBuilder.Append((char)this._lastChar);
                while (char.IsLetterOrDigit((char)(this._lastChar = this._reader.Read())))
                {
                    this._identifierBuilder.Append((char)this._lastChar);
                }

                this.LastIdentifier = this._identifierBuilder.ToString();
                this._identifierBuilder.Clear();

                switch (this.LastIdentifier)
                {
                    case "def":
                        this.CurrentToken = (int)Token.Def;
                        break;
                    case "extern":
                        this.CurrentToken = (int)Token.Extern;
                        break;
                    case "if":
                        this.CurrentToken = (int)Token.If;
                        break;
                    case "then":
                        this.CurrentToken = (int)Token.Then;
                        break;
                    case "else":
                        this.CurrentToken = (int)Token.Else;
                        break;
                    case "for":
                        this.CurrentToken = (int)Token.For;
                        break;
                    case "in":
                        this.CurrentToken = (int)Token.In;
                        break;
                    case "binary":
                        this.CurrentToken = (int)Token.Binary;
                        break;
                    case "unary":
                        this.CurrentToken = (int)Token.Unary;
                        break;
                    default:
                        this.CurrentToken = (int)Token.Identifier;
                        break;
                }

                return this.CurrentToken;
            }

            // Number: [0-9.]+
            if (char.IsDigit((char)this._lastChar) || this._lastChar == '.')
            {
                do
                {
                    this._numberBuilder.Append((char)this._lastChar);
                    this._lastChar = this._reader.Read();
                } while (char.IsDigit((char)this._lastChar) || this._lastChar == '.');
                
                this.LastNumber = double.Parse(this._numberBuilder.ToString());
                this._numberBuilder.Clear();
                this.CurrentToken = (int)Token.Number;

                return this.CurrentToken;
            }

            if (this._lastChar == '#')
            {
                // Comment until end of line.
                do
                {
                    this._lastChar = this._reader.Read();
                } while (this._lastChar != Eof && this._lastChar != '\n' && this._lastChar != '\r');

                if (this._lastChar != Eof)
                {
                    return this.GetNextToken();
                }
            }

            // Check for end of file.  Don't eat the EOF.
            if (this._lastChar == Eof)
            {
                this.CurrentToken = this._lastChar;
                return (int)Token.Eof;
            }

            this.CurrentToken = this._lastChar;
            this._lastChar = this._reader.Read();
            return this._lastChar;
        }
    }
}