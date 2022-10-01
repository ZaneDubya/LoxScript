using System.Collections.Generic;

namespace XPT.Core.Scripting.Base {
    abstract class ATokenizer {

        protected int Start {
            get => CurrentFile.Start;
            set => CurrentFile.Start = value;
        }

        protected int Current {
            get => CurrentFile.Current; 
            set => CurrentFile.Current = value;
        }

        protected int Line {
            get => CurrentFile.Line;
            set => CurrentFile.Line = value;
        }

        protected string Source => CurrentFile.Source;

        protected bool IsAtEnd => CurrentFile.Current >= CurrentFile.Source.Length;

        protected TokenList Tokens = new TokenList();

        protected TokenizerContext CurrentFile => _Files[_Files.Count - 1];

        private List<TokenizerContext> _Files = new List<TokenizerContext>();

        protected ATokenizer(string path, string source) {
            SourceBegin(path, source);
        }

        protected void SourceBegin(string filepath, string filesource, int line = 1) {
            _Files.Add(new TokenizerContext(filepath, filesource, line));
        }

        /// <summary>
        /// returns true if we should continue tokenizing, false if this is EOF.
        /// </summary>
        /// <returns></returns>
        protected bool SourceEnd() {
            _Files.RemoveAt(_Files.Count - 1);
            return _Files.Count > 0;
        }

        internal TokenList ScanTokens() {
            int lastLine;
            while (true) {
                while (!IsAtEnd) {
                    // we are at the beginning of the next lexeme
                    Start = Current;
                    ScanToken();
                }
                lastLine = Line;
                if (!SourceEnd()) {
                    break;
                }
            }
            Tokens.Add(new Token(TokenTypes.EOF, lastLine));
            return Tokens;
        }

        protected abstract void ScanToken();

        protected abstract void AddToken(int type);

        // === Support and consumption routines ===
        // ========================================

        /// <summary>
        /// consumes the next character in the source file and returns it.
        /// </summary>
        protected char Advance() {
            Current += 1;
            return Source[Current - 1];
        }

        protected bool IsAlphaOrUnderscore(char c) {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_';
        }

        protected bool IsAlphaUnderscoreOrNumeric(char c) {
            return IsAlphaOrUnderscore(c) || IsDigit(c);
        }

        protected bool IsDigit(char c, bool allowHex = false) {
            return (c >= '0' && c <= '9') || (allowHex && ((c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')));
        }

        /// <summary>
        /// a conditional advance(), only consumes the current character if it’s what we’re looking for.
        /// </summary>
        protected bool Match(char expected) {
            if (IsAtEnd) return false;
            if (Source[Current] != expected) {
                return false;
            }
            Current += 1;
            return true;
        }

        /// <summary>
        /// only looks at the current unconsumed character.
        /// </summary>
        protected char Peek() {
            if (IsAtEnd) {
                return '\0';
            }
            return Source[Current];
        }

        /// <summary>
        /// Returns the next character.
        /// </summary>
        protected char PeekNext() {
            if (Current + 1 >= Source.Length) {
                return '\0';
            }
            return Source[Current + 1];
        }
    }
}
