namespace LoxScript.Grammar {
    internal class Token {
        internal TokenType Type;
        internal string Lexeme;
        internal object Literal;
        internal int Line;

        internal Token(TokenType type, string lexeme, object literal, int line) {
            Type = type;
            Lexeme = lexeme;
            Literal = literal;
            Line = line;
        }

        public override string ToString() {
            return $"{Type} {Lexeme} {Literal}";
        }
    }
}