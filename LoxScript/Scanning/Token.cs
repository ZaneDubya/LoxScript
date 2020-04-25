namespace LoxScript.Scanning {
    /// <summary>
    /// Optimized Lox Token, 20 bytes.
    /// </summary>
    internal struct Token {
        internal readonly TokenType Type;
        internal readonly int Line;

        private readonly string _Source;
        private readonly int _SrcStart;
        private readonly int _SrcLength;

        internal string Lexeme => _Source?.Substring(_SrcStart, _SrcLength) ?? null;

        internal double LiteralAsNumber => double.Parse(_Source.Substring(_SrcStart, _SrcLength));

        internal string LiteralAsString => _Source.Substring(_SrcStart + 1, _SrcLength - 2);

        internal Token(TokenType type, int line) : this(type, line, null, 0, 0) { }

        internal Token(TokenType type, int line, string source, int srcStart, int srcLength) {
            Type = type;
            _Source = source;
            _SrcStart = srcStart;
            _SrcLength = srcLength;
            Line = line;
        }

        public override string ToString() => $"{Type} {Lexeme}";
    }
}
