using System;

namespace LoxScript.Scanning {
    /// <summary>
    /// A list of tokens.
    /// </summary>
    class TokenList {
        private Token[] _Tokens = null;
        private int _Next = 0;

        private int Capacity => _Tokens?.Length ?? 0;

        public int Count => _Next;

        public Token this[int index] {
            get {
                if (index < 0 || index >= Capacity) {
                    return default(Token);
                }
                return _Tokens[index];
            }
        }

        public void Add(Token token) {
            if (_Next >= Capacity) {
                if (_Tokens == null) {
                    _Tokens = new Token[16];
                }
                else {
                    Token[] newTokens = new Token[_Tokens.Length * 2];
                    Array.Copy(_Tokens, newTokens, Capacity);
                    _Tokens = newTokens;
                }
            }
            _Tokens[_Next++] = token;
        }
    }
}
