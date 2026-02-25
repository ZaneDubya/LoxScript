using System;

namespace XPT.Core.Scripting.Base {
    /// <summary>
    /// The parser is in a confused state and needs to panic and synchronize.
    /// </summary>
    class CompilerException : Exception {
        internal readonly Token Token;

        internal CompilerException(Token token, string message) : base(message) {
            Token = token;
        }

        public override string ToString() {
            if (Token.IsEOF) {
                return $"Compiler error at line {Token.Line} at EOF: {base.Message}";
            }
            else {
                return $"Compiler error at line {Token.Line} at '{Token.Lexeme}': {base.Message}";
            }
        }
    }
}
