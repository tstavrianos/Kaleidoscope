using System;
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

        private readonly Dictionary<char, int> _binopPrecedence;

        private int _lastChar = ' ';

        public int CurrentToken { get; private set; }

        public string LastIdentifier { get; private set; }

        public double LastNumber { get; private set; }

        public DefaultLexer(TextReader reader, Dictionary<char, int> binOpPrecedence)
        {
            this._reader = reader;
            this._binopPrecedence = binOpPrecedence;
        }

        public int GetTokPrecedence()
        {
            // Make sure it's a declared binop.
            if (this._binopPrecedence.TryGetValue((char)this.CurrentToken, out var tokPrec))
            {
                return tokPrec;
            }

            return -1;
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

                if (string.Equals(this.LastIdentifier, "def", StringComparison.Ordinal))
                {
                    this.CurrentToken = (int)Token.Def;
                }
                else if (string.Equals(this.LastIdentifier, "extern", StringComparison.Ordinal))
                {
                    this.CurrentToken = (int)Token.Extern;
                }
                else
                {
                    this.CurrentToken = (int)Token.Identifier;
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