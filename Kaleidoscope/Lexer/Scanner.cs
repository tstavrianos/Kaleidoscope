using System.IO;
using System.Text;

namespace Kaleidoscope.Lexer
{
    public class Scanner
    {
        public string Identifier { get; private set; }
        public double NumVal { get; private set; }
        private readonly TextReader _source;
        private int _lastChar;
        public int CurTok { get; private set; }

        private readonly StringBuilder _numberSb;
        private readonly StringBuilder _identifierSb;

        public Scanner(TextReader source)
        {
            this._source = source;
            this._lastChar = ' ';
            this._numberSb = new StringBuilder();
            this._identifierSb = new StringBuilder();
        }

        private int GetTok()
        {
            while (char.IsWhiteSpace((char) this._lastChar))
            {
                this._lastChar = this._source.Read();
            }

            if (char.IsLetter((char) this._lastChar))
            {
                this._identifierSb.Append((char) this._lastChar);
                while (char.IsLetterOrDigit((char) (this._lastChar = this._source.Read())))
                {
                    this._identifierSb.Append((char) this._lastChar);
                }

                this.Identifier = this._identifierSb.ToString();
                this._identifierSb.Clear();

                switch (this.Identifier)
                {
                    case "def":
                        return Token.Def;
                    case "extern":
                        return Token.Extern;
                    default:
                        return Token.Identifier;
                }
            }

            if (char.IsDigit((char) this._lastChar) || this._lastChar == '.')
            {
                do
                {
                    this._numberSb.Append((char) this._lastChar);
                    this._lastChar = this._source.Read();
                } while (char.IsDigit((char) this._lastChar) || this._lastChar == '.');

                this.NumVal = double.Parse(this._numberSb.ToString());
                this._numberSb.Clear();
                return Token.Number;
            }

            if (this._lastChar == '#')
            {
                do
                {
                    this._lastChar = this._source.Read();
                } while (this._lastChar != Token.Eof && this._lastChar != '\n' && this._lastChar != '\r');

                if (this._lastChar != Token.Eof)
                {
                    return this.GetTok();
                }
            }

            if (this._lastChar == Token.Eof) return this._lastChar;
            var thisChar = this._lastChar;
            this._lastChar = this._source.Read();
            return thisChar;
        }

        public int GetNextToken()
        {
            return this.CurTok = this.GetTok();
        }
    }
}