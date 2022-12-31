using System;

namespace XPT.Core.Scripting.Base {
    class Token {
        internal readonly int Type;
        internal readonly int Line;

        private readonly string _Source;
        private readonly int _SrcStart;
        private readonly int _SrcLength;

        internal string Lexeme => _Source?.Substring(_SrcStart, _SrcLength) ?? null;

        internal int LiteralAsNumber {
            get {
                if (Lexeme.Length > 2 && Lexeme.StartsWith("0x")) {
                    int value = int.Parse(_Source.Substring(_SrcStart + 2, _SrcLength - 2), System.Globalization.NumberStyles.HexNumber);
                    return value;
                }
                else {
                    try {
                        return int.Parse(_Source.Substring(_SrcStart, _SrcLength));
                    }
                    catch (Exception e) {
                        return 0;
                    }
                }
            }
    }

        internal string LiteralAsString => _Source.Substring(_SrcStart + 1, _SrcLength - 2);

        internal Token(int type, int line) : this(type, line, null, 0, 0) { }

        internal Token(int type, int line, string source) : this(type, line, source, 0, source.Length) { }

        internal Token(int type, int line, string source, int srcStart, int srcLength) {
            Type = type;
            Line = line;
            _Source = source;
            _SrcStart = srcStart;
            _SrcLength = srcLength;
        }

        /// <summary>
        /// TTokenType must have enum value of 0 equal to EOF.
        /// </summary>
        internal bool IsEOF => Type == TokenTypes.EOF;

        public override string ToString() => $"{Type} {Lexeme} @{Line}";
    }
}
