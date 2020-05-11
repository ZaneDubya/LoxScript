using System.Collections.Generic;

namespace XPT.Compiling {
    static class Keywords {
        internal const string This = "this";
        internal const string Ctor = "init";

        private readonly static Dictionary<string, TokenType> _Keywords = new Dictionary<string, TokenType>();

        static Keywords() {
            _Keywords["and"] = TokenType.AND;
            _Keywords["class"] = TokenType.CLASS;
            _Keywords["else"] = TokenType.ELSE;
            _Keywords["false"] = TokenType.FALSE;
            _Keywords["for"] = TokenType.FOR;
            _Keywords["fun"] = TokenType.FUNCTION;
            _Keywords["if"] = TokenType.IF;
            _Keywords["nil"] = TokenType.NIL;
            _Keywords["or"] = TokenType.OR;
            _Keywords["print"] = TokenType.PRINT;
            _Keywords["return"] = TokenType.RETURN;
            _Keywords["super"] = TokenType.SUPER;
            _Keywords["this"] = TokenType.THIS;
            _Keywords["true"] = TokenType.TRUE;
            _Keywords["var"] = TokenType.VAR;
            _Keywords["while"] = TokenType.WHILE;
        }

        internal static TokenType? Get(string text) {
            if (_Keywords.TryGetValue(text, out TokenType type)) {
                return type;
            }
            return null;
        }
    }
}
