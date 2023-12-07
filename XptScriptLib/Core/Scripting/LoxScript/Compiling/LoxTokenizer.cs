using System.Collections.Generic;
using System.IO;
using XPT.Core.Scripting.Base;

namespace XPT.Core.Scripting.LoxScript.Compiling {
    /// <summary>
    /// Tokenizer is a scanner that transforms an input source file into TokenList.
    /// Tokens defined by reserved keywords are recognized by checking against Grammar/Keywords.cs
    /// </summary>
    internal class LoxTokenizer : ATokenizer {

        private readonly Dictionary<string, string> _PreProcessorDefines = new Dictionary<string, string>();

        public LoxTokenizer(string path, string source) : base(path, source) { }

        public override void Reset(string path, string source, int line = 1) {
            _PreProcessorDefines.Clear();
            base.Reset(path, source, line);
        }

        // === These must be implemented for each language. ==========================================================
        // ===========================================================================================================

        protected bool IsFloatingPointPermitted => false;

        /// <summary>
        /// Grabs the text of the current lexeme and creates a new token for it.
        /// </summary>
        protected override void AddToken(int type) {
            Tokens.Add(new Token(type, Line, Source, Start, Current - Start));
        }

        protected override void ScanToken() {
            char c = Advance();
            switch (c) {
                case '(':
                    AddToken(TokenTypes.LEFT_PAREN);
                    break;
                case ')':
                    AddToken(TokenTypes.RIGHT_PAREN);
                    break;
                case '{':
                    AddToken(TokenTypes.LEFT_BRACE);
                    break;
                case '}':
                    AddToken(TokenTypes.RIGHT_BRACE);
                    break;
                case '[':
                    AddToken(TokenTypes.LEFT_BRACKET);
                    break;
                case ']':
                    AddToken(TokenTypes.RIGHT_BRACKET);
                    break;
                case ',':
                    AddToken(TokenTypes.COMMA);
                    break;
                case '.':
                    AddToken(TokenTypes.DOT);
                    break;
                case '-':
                    AddToken(Match('-') ? TokenTypes.DECREMENT : TokenTypes.MINUS);
                    break;
                case '+':
                    AddToken(Match('+') ? TokenTypes.INCREMENT : TokenTypes.PLUS);
                    break;
                case ';':
                    AddToken(TokenTypes.SEMICOLON);
                    break;
                case '*':
                    AddToken(TokenTypes.STAR);
                    break;
                case '!':
                    AddToken(Match('=') ? TokenTypes.BANG_EQUAL : TokenTypes.BANG);
                    break;
                case '=':
                    AddToken(Match('=') ? TokenTypes.EQUAL_EQUAL : TokenTypes.EQUAL);
                    break;
                case '<':
                    AddToken(Match('=') ? TokenTypes.LESS_EQUAL : TokenTypes.LESS);
                    break;
                case '>':
                    AddToken(Match('=') ? TokenTypes.GREATER_EQUAL : TokenTypes.GREATER);
                    break;
                case '&':
                    AddToken(Match('&') ? LoxTokenTypes.AND : TokenTypes.AMPERSAND);
                    break;
                case '|':
                    AddToken(Match('|') ? LoxTokenTypes.OR : TokenTypes.PIPE);
                    break;
                case ':':
                    AddToken(TokenTypes.COLON);
                    break;
                case '~':
                    AddToken(TokenTypes.TILDE);
                    break;
                case '/':
                    if (Match('/')) {
                        // A comment goes until the end of the line.                
                        while (Peek() != '\n' && !IsAtEnd) {
                            Advance();
                        }
                        Start = Current;
                    }
                    else if (Match('*')) {
                        while (!IsAtEnd) {
                            if (Peek() == '*' && PeekNext() == '/') {
                                Advance();
                                Advance();
                                break;
                            }
                            else if (Peek() == '\n') {
                                Line += 1;
                            }
                            Advance();
                        }
                    }
                    else {
                        AddToken(TokenTypes.SLASH);
                    }
                    break;
                case '%':
                    AddToken(TokenTypes.PERCENT);
                    break;
                case ' ':
                case '\r':
                case '\t':
                    // Ignore whitespace.                      
                    break;
                case '\n':
                    Line += 1;
                    break;
                case '"':
                    String();
                    break;
                case '#':
                    PreProcessor();
                    break;
                default:
                    if (IsDigit(c)) {
                        Number();
                    }
                    else if (IsAlphaOrUnderscore(c)) {
                        Identifier();
                    }
                    else {
                        throw new CompilerException(new Token(TokenTypes.ERROR, Line, c.ToString()), $"Unexpected character '{c}'.");
                    }
                    break;
            }
        }

        private void PreProcessor() {
            while (IsAlphaUnderscoreOrNumeric(Peek())) {
                Advance();
            }
            string preprocessor = Source.Substring(Start, Current - Start);
            switch (preprocessor) {
                case "#define":
                    PreProcessorDefine();
                    break;
                case "#include":
                    PreProcessorInclude();
                    break;
                default:
                    throw new CompilerException(new Token(TokenTypes.ERROR, Line), $"Unrecognized preprocessor '{preprocessor}'.");
            }
        }

        private void PreProcessorDefine() {
            while (Peek() == ' ' || Peek() == '\t') {
                Advance();
            }
            int nameStart = Current;
            while (IsAlphaUnderscoreOrNumeric(Peek())) {
                Advance();
            }
            // See if the name is a reserved word.   
            string name = Source.Substring(nameStart, Current - nameStart);
            int? keyword = LoxTokenTypes.Get(name);
            if (keyword != null) {
                throw new CompilerException(new Token(TokenTypes.ERROR, Line), $"Cannot redefine keyword '{name}'.");
            }
            while (Peek() == ' ' || Peek() == '\t') {
                Advance();
            }
            int contentStart = Current;
            while (Peek() != '\r' && Peek() != '\n') {
                Advance();
            }
            string content = Source.Substring(contentStart, Current - contentStart);
            _PreProcessorDefines[name] = content;
        }

        private void PreProcessorInclude() {
            // allow whitespace after #include keyword
            while (Peek() == ' ' || Peek() == '\t') {
                Advance();
            }
            // get content
            int filenameStart = Current;
            bool inQuotes = Match('\"');
            while ((inQuotes && !Match('\"')) || (!inQuotes && (Peek() != ' ' && Peek() != '\t' && Peek() != '\r' && Peek() != '\n'))) {
                Advance();
            }
            string filename = Source.Substring(filenameStart + (inQuotes ? 1 : 0), Current - filenameStart - (inQuotes ? 2 : 0));
            try {
                string filepath = Path.Combine(Path.GetDirectoryName(CurrentFile.Path), filename);
                string filesource = File.ReadAllText(filepath);
                SourceBegin(filepath, filesource);
            }
            catch {
                throw new CompilerException(new Token(TokenTypes.ERROR, Line), $"Error including '{filename}'.");
            }
        }

        private void Identifier() {
            while (IsAlphaUnderscoreOrNumeric(Peek())) {
                Advance();
            }
            string text = Source.Substring(Start, Current - Start);
            // See if the identifier is a reserved word.   
            int? keyword = LoxTokenTypes.Get(text);
            if (keyword != null) {
                AddToken(keyword.Value);
            }
            else if (_PreProcessorDefines.TryGetValue(text, out string replace)) {
                SourceBegin(CurrentFile.Path, replace, Line);
                // Source = Source.Remove(Start, text.Length).Insert(Start, replace);
                // Current = Start;
            }
            else {
                AddToken(TokenTypes.IDENTIFIER);
            }
        }

        /// <summary>
        /// Consumes as many digits as it finds for the integer part of the literal. Then it looks for a fractional part, 
        /// which is a decimal point (.), followed by at least one digit.
        /// </summary>
        private void Number() {
            if (Peek() == 'x' && IsDigit(PeekNext(), allowHex: true)) {
                // Consume the "x"
                Advance();
                while (IsDigit(Peek(), allowHex: true)) {
                    Advance();
                }
            }
            else {
                while (IsDigit(Peek())) {
                    Advance();
                }
                // Look for a fractional part, if this language supports floating pt numbers.
                if (IsFloatingPointPermitted && Peek() == '.' && IsDigit(PeekNext())) {
                    // Consume the "."
                    Advance();
                    while (IsDigit(Peek())) {
                        Advance();
                    }
                }
            }
            AddToken(TokenTypes.NUMBER);
        }

        private void String() {
            while (Peek() != '"' && !IsAtEnd) {
                if (Peek() == '\n') {
                    Line++;
                }
                Advance();
            }
            // Unterminated string.                                 
            if (IsAtEnd) {
                throw new CompilerException(new Token(TokenTypes.EOF, Line), "Unterminated string.");
            }
            // The closing ".                                       
            Advance();
            AddToken(TokenTypes.STRING);
        }

        // === Post Processing =======================================================================================
        // ===========================================================================================================

        protected override void PostProcessTokens() {
            TransformSwitchStatementsToIfStatements();
        }

        private void TransformSwitchStatementsToIfStatements() {
            for (int i = 0; i < Tokens.Count; i++) {
                if (Tokens[i].Type == LoxTokenTypes.SWITCH) {

                }
            }
        }
    }
}