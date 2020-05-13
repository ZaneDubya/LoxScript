namespace XPT.Core.Scripting.Compiling {
    /// <summary>
    /// Tokenizer is a scanner that transforms an input source file into TokenList.
    /// Tokens defined by reserved keywords are recognized by checking against Grammar/Keywords.cs
    /// </summary>
    internal class Tokenizer {
        private string _Source;
        private int _Start = 0;
        private int _Current = 0;
        private int _Line = 1;
        private TokenList _Tokens = new TokenList();
        private readonly bool _PrintIsKeyword;

        private bool IsAtEnd => _Current >= _Source.Length;

        public Tokenizer(string source, bool printIsKeyword = false) {
            _Source = source;
            _PrintIsKeyword = printIsKeyword;
        }

        internal TokenList ScanTokens() {
            while (!IsAtEnd) {
                // we are at the beginning of the next lexeme
                _Start = _Current;
                ScanToken();
            }
            AddToken(TokenType.EOF);
            return _Tokens;
        }

        private void ScanToken() {
            char c = Advance();
            switch (c) {
                case '(':
                    AddToken(TokenType.LEFT_PAREN);
                    break;
                case ')':
                    AddToken(TokenType.RIGHT_PAREN);
                    break;
                case '{':
                    AddToken(TokenType.LEFT_BRACE);
                    break;
                case '}':
                    AddToken(TokenType.RIGHT_BRACE);
                    break;
                case ',':
                    AddToken(TokenType.COMMA);
                    break;
                case '.':
                    AddToken(TokenType.DOT);
                    break;
                case '-':
                    AddToken(TokenType.MINUS);
                    break;
                case '+':
                    AddToken(TokenType.PLUS);
                    break;
                case ';':
                    AddToken(TokenType.SEMICOLON);
                    break;
                case '*':
                    AddToken(TokenType.STAR);
                    break;
                case '!':
                    AddToken(Match('=') ? TokenType.BANG_EQUAL : TokenType.BANG);
                    break;
                case '=':
                    AddToken(Match('=') ? TokenType.EQUAL_EQUAL : TokenType.EQUAL);
                    break;
                case '<':
                    AddToken(Match('=') ? TokenType.LESS_EQUAL : TokenType.LESS);
                    break;
                case '>':
                    AddToken(Match('=') ? TokenType.GREATER_EQUAL : TokenType.GREATER);
                    break;
                case '/':
                    if (Match('/')) {
                        // A comment goes until the end of the line.                
                        while (Peek() != '\n' && !IsAtEnd) {
                            Advance();
                        }
                        _Start = _Current;
                    }
                    else if (Match('*')) {
                        while (!IsAtEnd) {
                            if (Peek() == '*' && PeekNext() == '/') {
                                Advance();
                                Advance();
                                break;
                            }
                            else if (Peek() == '\n') {
                                _Line += 1;
                            }
                            Advance();
                        }
                    }
                    else {
                        AddToken(TokenType.SLASH);
                    }
                    break;
                case ' ':
                case '\r':
                case '\t':
                    // Ignore whitespace.                      
                    break;
                case '\n':
                    _Line += 1;
                    break;
                case '"':
                    String();
                    break;
                default:
                    if (IsDigit(c)) {
                        Number();
                    }
                    else if (IsAlphaOrUnderscore(c)) {
                        Identifier();
                    }
                    else {
                        Program.Error(_Line, $"Unexpected character '{c}'.");
                    }
                    break;
            }
        }

        private void Identifier() {
            while (IsAlphaUnderscoreOrNumeric(Peek())) {
                Advance();
            } 
            // See if the identifier is a reserved word.   
            string text = _Source.Substring(_Start, _Current - _Start);
            TokenType? type = Keywords.Get(text);
            if (type == null) {
                if (_PrintIsKeyword && text == "print") {
                    type = TokenType.PRINT;
                }
                else {
                    type = TokenType.IDENTIFIER;
                }
            }
            AddToken(type.Value);
        }

        private bool IsAlphaOrUnderscore(char c) {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_';
        }

        private bool IsAlphaUnderscoreOrNumeric(char c) {
            return IsAlphaOrUnderscore(c) || IsDigit(c);
        }

        private bool IsDigit(char c) {
            return c >= '0' && c <= '9';
        }

        /// <summary>
        /// Consumes as many digits as it finds for the integer part of the literal. Then it looks for a fractional part, which is a decimal point (.) followed by at least one digit.
        /// </summary>
        private void Number() {
            while (IsDigit(Peek())) {
                Advance();
            }
            // Look for a fractional part.                            
            if (Peek() == '.' && IsDigit(PeekNext())) {
                // Consume the "."                                      
                Advance();
                while (IsDigit(Peek())) {
                    Advance();
                }
            }
            AddToken(TokenType.NUMBER);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private char PeekNext() {
            if (_Current + 1 >= _Source.Length) {
                return '\0';
            }
            return _Source[_Current + 1];
        }

        private void String() {
            while (Peek() != '"' && !IsAtEnd) {
                if (Peek() == '\n') {
                    _Line++;
                }
                Advance();
            }
            // Unterminated string.                                 
            if (IsAtEnd) {
                Program.Error(_Line, "Unterminated string.");
                return;
            }
            // The closing ".                                       
            Advance();
            AddToken(TokenType.STRING);
        }

        /// <summary>
        /// consumes the next character in the source file and returns it.
        /// </summary>
        private char Advance() {
            _Current += 1;
            return _Source[_Current - 1];
        }

        /// <summary>
        /// a conditional advance(), only consumes the current character if it’s what we’re looking for.
        /// </summary>
        private bool Match(char expected) {
            if (IsAtEnd) return false;
            if (_Source[_Current] != expected) {
                return false;
            }
            _Current += 1;
            return true;
        }

        /// <summary>
        /// only looks at the current unconsumed character.
        /// </summary>
        private char Peek() {
            if (IsAtEnd) {
                return '\0';
            }
            return _Source[_Current];
        }

        /// <summary>
        /// grabs the text of the current lexeme and creates a new token for it.
        /// </summary>
        private void AddToken(TokenType type) {
            string text = _Source.Substring(_Start, _Current - _Start);
            // bool srcHasParens = text[0] == '\"';
            _Tokens.Add(new Token(type, _Line, _Source, _Start, _Current - _Start));
        }
    }
}