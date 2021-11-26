using System;

namespace XPT.Core.Scripting.Base {
    /// <summary>
    /// The parser is in a confused state and needs to panic and synchronize.
    /// </summary>
    class CompilerException : Exception {
        private readonly Token _Token;

        internal CompilerException(Token token, string message) : base(message) {
            _Token = token;
        }

        public override string ToString() {
            if (_Token.IsEOF) {
                return $"[line {_Token.Line}] Error at EOF: {base.Message}";
            }
            else {
                return $"[line {_Token.Line}] Error at '{_Token.Lexeme}': {base.Message}";
            }
        }
    }
}
