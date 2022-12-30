using System;

namespace XPT.Core.Scripting.Base {
    class TokenList {
        private Token[] _Tokens = null;
        private int _Next = 0;

        private int Capacity => _Tokens?.Length ?? 0;

        public int Count => _Next;

        public Token this[int index] {
            get {
                if (index < 0 || index >= Capacity) {
                    return default;
                }
                return _Tokens[index];
            }
        }

        public Token Add(Token token) {
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
            return token;
        }

        public void AddedRewind(int count = 1) {
            for (int i = 0; i < count; i++) {
                _Tokens[--_Next] = default;
            }
        }

        public Token AddedLast => _Next > 0 ? _Tokens[_Next - 1] : default;

        // === Infrastructure, should only be used by Compiler =======================================================
        // ===========================================================================================================

        private int _CurrentToken = 0;

        public Token Current => _Tokens[_CurrentToken];

        /// <summary>
        /// Checks to see if the next token is of the expected type.
        /// If so, it consumes it and everything is groovy.
        /// If some other token is there, then we’ve hit an error.
        /// </summary>
        public Token Consume(int type, string message) {
            if (Check(type)) {
                return Advance();
            }
            throw new CompilerException(Peek(), message);
        }

        /// <summary>
        /// Checks to see if the next token is of the expected type and lexeme.
        /// If so, it consumes it and everything is groovy.
        /// If some other token is there, then we’ve hit an error.
        /// </summary>
        public Token Consume(int type, string lexeme, string message) {
            if (Check(type) && Current.Lexeme == lexeme) {
                return Advance();
            }
            throw new CompilerException(Peek(), message);
        }

        /// <summary>
        /// Checks if the current token is any of the given types.
        /// If so, consumes the token and returns true.
        /// Otherwise, returns false and leaves the token as the current one.
        /// </summary>
        public bool Match(params int[] types) {
            foreach (int type in types) {
                if (Check(type)) {
                    Advance();
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns true if the current token is of the given type
        /// </summary>
        public bool Check(int type) {
            if (IsAtEnd()) {
                return false;
            }
            return Peek().Type.Equals(type);
        }

        /// <summary>
        /// consumes the current token and returns it.
        /// </summary>
        public Token Advance() {
            if (!IsAtEnd()) {
                _CurrentToken++;
            }
            return Previous();
        }

        /// <summary>
        /// checks if we’ve run out of tokens to parse. ETokenType must have enum value of 0 equal to EOF.
        /// </summary>
        public bool IsAtEnd() {
            return Peek().IsEOF;
        }

        /// <summary>
        /// returns the current token we have yet to consume
        /// </summary>
        public Token Peek() {
            return _Tokens[_CurrentToken];
        }

        /// <summary>
        /// returns a token with offset from the next token that will be consumed. Peek() is equivalent to Peek(0).
        /// </summary>
        public Token Peek(int offset) {
            if (_CurrentToken + offset >= _Tokens.Length || _CurrentToken + offset < 0) {
                return null;
            }
            return _Tokens[_CurrentToken + offset];
        }

        /// <summary>
        /// returns the most recently consumed token.
        /// </summary>
        public Token Previous() {
            return _Tokens[_CurrentToken - 1];
        }

        public void Rewind(int count = 1) {
            _CurrentToken -= count;
            if (_CurrentToken < 0) {
                _CurrentToken = 0;
            }
        }
    }
}
