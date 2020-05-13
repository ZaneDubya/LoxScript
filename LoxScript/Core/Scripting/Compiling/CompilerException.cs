using System;

namespace XPT.Core.Scripting.Compiling {
    /// <summary>
    /// Throw this when the parser is in a confused state and needs to panic and synchronize.
    /// </summary>
    public class CompilerException : Exception {
        private readonly Token _Token;
        private readonly string _Message;

        internal CompilerException(Token token, string message) {
            _Token = token;
            _Message = message;
        }

        internal void Print() {
            Program.Error(_Token, _Message);
        }
    }
}
