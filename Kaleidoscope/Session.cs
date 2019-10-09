using System.Collections.Generic;

namespace Kaleidoscope
{
    public class Session
    {
        private readonly Dictionary<char, int> _binOpPrecedence;

        public Session()
        {
            this._binOpPrecedence = new Dictionary<char, int>
            {
                ['<'] = 10,
                ['+'] = 20,
                ['-'] = 20,
                ['*'] = 40
            };
        }

        public int GetTokPrecedence(int token)
        {
            if (!token.IsAscii())
            {
                return -1;
            }
            
            // Make sure it's a declared binop.
            if (this._binOpPrecedence.TryGetValue((char)token, out var tokPrec))
            {
                return tokPrec;
            }

            return -1;
        }

        internal void AddBinOpPrecedence(char token, int precedence)
        {
            this._binOpPrecedence[token] = precedence;
        }
    }
}