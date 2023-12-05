namespace XPT.Core.Scripting.Base {
    internal static class TokenTypes {
        // End of file.
        internal const int
            EOF = 0,
            // Error.
            ERROR = 1,
            // One or two character tokens.               
            LEFT_BRACE = 2, RIGHT_BRACE = 3, LEFT_PAREN = 4, RIGHT_PAREN = 5,
            COMMA = 6, DOT = 7, MINUS = 8, PLUS = 9, SEMICOLON = 10, SLASH = 11, STAR = 12,
            BANG = 13, BANG_EQUAL = 14, EQUAL = 15, EQUAL_EQUAL = 16,
            GREATER = 17, GREATER_EQUAL = 18, LESS = 19, LESS_EQUAL = 20,
            AMPERSAND = 21, PIPE = 22, LEFT_BRACKET = 23, RIGHT_BRACKET = 24, TILDE = 25,
            PERCENT = 26, INCREMENT = 27, DECREMENT = 28, COLON = 29,
            // Literals.
            IDENTIFIER = 81, STRING = 82, NUMBER = 83;
    }
}
