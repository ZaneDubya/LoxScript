using System;

namespace XPT.Compiling {
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

        // === Infrastructure, should only be used by Compiler =======================================================
        // ===========================================================================================================

        private int _CurrentToken = 0;

        /// <summary>
        /// Checks to see if the next token is of the expected type.
        /// If so, it consumes it and everything is groovy.
        /// If some other token is there, then we’ve hit an error.
        /// </summary>
        public Token Consume(TokenType type, string message) {
            if (Check(type)) {
                return Advance();
            }
            throw new CompilerException(Peek(), message);
        }

        /// <summary>
        /// Checks if the current token is any of the given types.
        /// If so, consumes the token and returns true.
        /// Otherwise, returns false and leaves the token as the current one.
        /// </summary>
        public bool Match(params TokenType[] types) {
            foreach (TokenType type in types) {
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
        public bool Check(TokenType type) {
            if (IsAtEnd()) {
                return false;
            }
            return Peek().Type == type;
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
        /// checks if we’ve run out of tokens to parse.
        /// </summary>
        public bool IsAtEnd() {
            return Peek().Type == TokenType.EOF;
        }

        /// <summary>
        /// returns the current token we have yet to consume
        /// </summary>
        public Token Peek() {
            return _Tokens[_CurrentToken];
        }

        /// <summary>
        /// returns the most recently consumed token.
        /// </summary>
        public Token Previous() {
            return _Tokens[_CurrentToken - 1];
        }
    }
}
