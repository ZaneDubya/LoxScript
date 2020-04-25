using LoxScript.Scanning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static LoxScript.Scanning.TokenType;
using static LoxScript.VirtualMachine.EGearsOpCode;

namespace LoxScript.VirtualMachine {
    /// <summary>
    /// Compiler compiles a TokenList into bytecode in the fom of a GearsChunk that is executed by Gears.
    /// (c.f. Parser compiles a TokenList into an AST of Stmts which is interpreted by Engine.)
    /// </summary>
    class CompilerOld {
        /// <summary>
        /// Attempts to compile the passed list of tokens.
        /// If compilation is successful, compiler.Chunk will be set.
        /// If compilation fails, compiler.Error will be set.
        /// </summary>
        public static bool Compile(string name, TokenList tokens, out GearsChunk chunk) {
            CompilerOld compiler = new CompilerOld(name, tokens);
            if (compiler.Compile()) {
                chunk = compiler.Chunk;
                return true;
            }
            chunk = null;
            return false;
        }

        // === Instance ==============================================================================================
        // ===========================================================================================================

        public GearsChunk Chunk { get; private set; }
        public string ErrorMsg { get; private set; }

        private TokenList _Tokens;
        private int _Current;
        // private bool _HadError;
        // private bool _PanicMode;

        private CompilerOld(string name, TokenList tokens) {
            Chunk = new GearsChunk(name);
            _Tokens = tokens;
            _Current = 0;
        }

        private bool Compile() {
            try {
                Expression();
                Consume(EOF, "Expect end of expression.");
                EndCompiler();
                /*while (true) {
                    Token token = ScanToken();
                    if (token.Line != _Line) {
                        _Line = token.Line;
                        Console.Write($"{_Line:D4} ");
                    }
                    else {
                        Console.Write($"   | ");
                    }
                    Console.WriteLine($"{token.Type} {token.Lexeme}");
                    if (token.Type == EOF) {
                        break;
                    }
                }*/
                return true;
            }
            catch (CompilerException e) {
                ErrorMsg = e.Message;
                return false;
            }
        }

        private void EndCompiler() {
            EmitReturn();
        }

        private void DoPrecedence(EPrecedence precedence) {
            throw new NotImplementedException();
        }

        // === Expressions ===========================================================================================
        // ===========================================================================================================

        private void Binary() {
            // Remember the operator.                                
            TokenType operatorType = Previous().Type;

            // Emit the operator instruction.                        
            switch (operatorType) {
                case TokenType.PLUS: EmitBytes(OP_ADD); break;
                case TokenType.MINUS: EmitBytes(OP_SUBTRACT); break;
                case TokenType.STAR: EmitBytes(OP_MULTIPLY); break;
                case TokenType.SLASH: EmitBytes(OP_DIVIDE); break;
                default:
                    return; // Unreachable.                              
            }
        }

        private void Expression() {
            DoPrecedence(EPrecedence.PREC_ASSIGNMENT);
        }

        private void Grouping() {
            Expression();
            Consume(RIGHT_PAREN, "Expect ')' after expression.");
        }

        private void Number() {
            double value = Peek().LiteralAsNumber;
            EmitConstant(value);
        }

        private void Unary() {
            TokenType operatorType = Previous().Type;
            DoPrecedence(EPrecedence.PREC_UNARY);
            // Emit the operator instruction.
            switch (operatorType) {
                case MINUS:
                    EmitBytes((byte)OP_NEGATE);
                    break;
                default:
                    return; // unreachable
            }
        }

        // === Emit Infrastructure ===================================================================================
        // ===========================================================================================================

        private void EmitReturn() {
            EmitBytes(OP_RETURN);
        }

        private void EmitBytes(EGearsOpCode value) {
            EmitBytes((byte)value);
        }

        private void EmitBytes(params byte[] bytes) {
            foreach (byte b in bytes) {
                Chunk.Write(b);
            }
        }

        private void EmitConstant(double value) {
            EmitBytes(MakeConstant(value));
        }

        // todo - make this a part of emit constant?
        private byte MakeConstant(double value) {
            int index = Chunk.AddConstant(value);
            if (index > byte.MaxValue) {
                throw new CompilerException(Peek(), "Too many constants in one chunk.");
            }
            return (byte)index;
        }

        // === Parser Infrastructure =================================================================================
        // ===========================================================================================================

        /// <summary>
        /// Checks to see if the next token is of the expected type.
        /// If so, it consumes it and everything is groovy.
        /// If some other token is there, then we’ve hit an error.
        /// </summary>
        private Token Consume(TokenType type, string message) {
            if (Check(type)) {
                return Advance();
            }
            throw new CompilerException(Peek(), message);
        }

        /// <summary>
        /// Checks if the current token is any of the given types.
        /// If so, consumes the token and returns true.
        /// Otherwise, returns false and leaves the token as the current one.
        /// </summary>
        private bool Match(params TokenType[] types) {
            foreach (TokenType type in types) {
                if (Check(type)) {
                    Advance();
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns true if the current token is of the given type
        /// </summary>
        private bool Check(TokenType type) {
            if (IsAtEnd()) {
                return false;
            }
            return Peek().Type == type;
        }

        /// <summary>
        /// consumes the current token and returns it.
        /// </summary>
        /// <returns></returns>
        private Token Advance() {
            if (!IsAtEnd()) {
                _Current++;
            }
            return Previous();
        }

        /// <summary>
        /// checks if we’ve run out of tokens to parse.
        /// </summary>
        private bool IsAtEnd() {
            return Peek().Type == EOF;
        }

        /// <summary>
        /// returns the current token we have yet to consume
        /// </summary>
        /// <returns></returns>
        private Token Peek() {
            return _Tokens[_Current];
        }

        /// <summary>
        /// returns the most recently consumed token.
        /// </summary>
        private Token Previous() {
            return _Tokens[_Current - 1];
        }

        // === Parse Rules ===========================================================================================
        // ===========================================================================================================

        // === Precedence ============================================================================================
        // ===========================================================================================================

        /// <summary>
        /// All of Lox’s precedence levels in order from lowest to highest.
        /// </summary>
        private enum EPrecedence {
            PREC_NONE,
            PREC_ASSIGNMENT,  // =        
            PREC_OR,          // or       
            PREC_AND,         // and      
            PREC_EQUALITY,    // == !=    
            PREC_COMPARISON,  // < > <= >=
            PREC_TERM,        // + -      
            PREC_FACTOR,      // * /      
            PREC_UNARY,       // ! -      
            PREC_CALL,        // . ()     
            PREC_PRIMARY
        };  

        // === Error Handling ========================================================================================
        // ===========================================================================================================

        /// <summary>
        /// Discards tokens until it thinks it found a statement boundary. After catching a ParseError, we’ll call this
        /// and then we are hopefully back in sync. When it works well, we have discarded tokens that would have likely
        /// caused cascaded errors anyway and now we can parse the rest of the file starting at the next statement.
        /// </summary>
        private void Synchronize() {
            Advance();
            while (!IsAtEnd()) {
                if (Previous().Type == SEMICOLON) {
                    return;
                }
                switch (Peek().Type) {
                    case CLASS:
                    case FUNCTION:
                    case VAR:
                    case FOR:
                    case IF:
                    case WHILE:
                    case PRINT:
                    case RETURN:
                        return;
                }
                Advance();
            }
        }

        /// <summary>
        /// Throw this when the parser is in a confused state and needs to panic and synchronize.
        /// </summary>
        class CompilerException : Exception {
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
}
