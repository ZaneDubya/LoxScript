using LoxScript.Grammar;
using LoxScript.Scanning;
using System;
using static LoxScript.Scanning.TokenType;
using static LoxScript.VirtualMachine.EGearsOpCode;

namespace LoxScript.VirtualMachine {
    /// <summary>
    /// Compiler parses a TokenList and compiles it into a GearsChunk: bytecode executed by Gears.
    /// </summary>
    class Compiler {
        /// <summary>
        /// Attempts to compile the passed list of tokens.
        /// If compilation is successful, compiler.Chunk will be set.
        /// If compilation fails, status will be the error message.
        /// </summary>
        public static bool TryCompile(string name, TokenList tokens, out GearsChunk chunk, out string status) {
            chunk = null;
            Compiler compiler = new Compiler();
            if (compiler.Compile(name, tokens)) {
                chunk = compiler.Chunk;
            }
            status = compiler.Message;
            return chunk != null;
        }

        // === Instance ==============================================================================================
        // ===========================================================================================================

        public GearsChunk Chunk;
        public string Message = null;

        // tokens, errors, and can assign:
        private TokenList _Tokens;
        private int _CurrentToken = 0;
        private bool _HadError = false;
        private bool _CanAssign = false;

        // scope and locals:
        private const int SCOPE_GLOBAL = 0;
        private const int SCOPE_NONE = -1;
        private const int MAX_LOCALS = 256;
        private CompilerLocal[] Locals = new CompilerLocal[MAX_LOCALS];
        private int _LocalCount = 0;
        private int _ScopeDepth = SCOPE_GLOBAL;

        private Compiler() {
            Message = null;
        }

        // === Declarations and Statements ===========================================================================
        // ===========================================================================================================

        /// <summary>
        /// program     → declaration* EOF ;
        /// </summary>
        internal bool Compile(string name, TokenList tokens) {
            _Tokens = tokens;
            Chunk = new GearsChunk(name);
            while (!IsAtEnd()) {
                try {
                    Declaration();
                }
                catch (CompilerException e) {
                    _HadError = true;
                    e.Print();
                    Synchronize();
                }
            }
            EndCompiler();
            return !_HadError;
        }

        private void EndCompiler() {
            EmitReturn();
        }

        // === Declarations ==========================================================================================
        // ===========================================================================================================

        /// <summary>
        /// declaration → "class" class
        ///             | "fun" function
        ///             | varDecl
        ///             | statement ;
        /// </summary>
        private void Declaration() {
            try {
                if (Match(CLASS)) {
                    ClassDeclaration();
                    return;
                }
                if (Match(FUNCTION)) {
                    Function("function");
                    return;
                }
                if (Match(VAR)) {
                    VarDeclaration();
                    return;
                }
                Statement();
            }
            catch (CompilerException e) {
                Synchronize();
                throw e;
            }
        }

        /// <summary>
        /// classDecl   → "class" IDENTIFIER ( "&lt;" IDENTIFIER )? 
        ///               "{" function* "}" ;
        /// </summary>
        private void ClassDeclaration() {
            Token name = Consume(IDENTIFIER, "Expect class name.");
            Expr.Variable superClass = null;
            if (Match(LESS)) {
                Consume(IDENTIFIER, "Expect superclass name.");
                superClass = new Expr.Variable(Previous());
            }
            Consume(LEFT_BRACE, "Expect '{' before class body.");
            while (!Check(RIGHT_BRACE) && !IsAtEnd()) {
                // !!! we are adding methods to the class...
                Function("method");
            }
            Consume(RIGHT_BRACE, "Expect '}' after class body.");
            return; // !!! return new Stmt.Class(name, superClass, methods);
        }

        /// <summary>
        /// function    → IDENTIFIER "(" parameters? ")" block ;
        /// parameters  → IDENTIFIER( "," IDENTIFIER )* ;
        /// </summary>
        private void Function(string kind) {
            Token name = Consume(IDENTIFIER, $"Expect {kind} name.");
            Consume(LEFT_PAREN, $"Expect '(' after {kind} name.");
            // parameter list:
            int paramCount = 0;
            if (!Check(RIGHT_PAREN)) {
                do {
                    if (paramCount >= 255) {
                        throw new CompilerException(Peek(), "Cannot have more than 255 parameters.");
                    }
                    // !!! add parameter:
                    Consume(IDENTIFIER, "Expect parameter name.");
                    paramCount += 1;
                } while (Match(COMMA));
            }
            Consume(RIGHT_PAREN, "Expect ')' after parameters.");
            // body:
            Consume(LEFT_BRACE, "Expect '{' before " + kind + " body.");
            Block();
            // !!! return new Stmt.Function(name, parameters, body);
        }

        private void VarDeclaration() {
            byte global = ParseVariable("Expect variable name.");
            if (Match(EQUAL)) {
                Expression(); // var x = value;
            }
            else {
                EmitBytes((byte)OP_NIL); // equivalent to var x = nil;
            }
            Consume(SEMICOLON, "Expect ';' after variable declaration.");
            DefineVariable(global);
            return;
        }

        private byte ParseVariable(string errorMsg) {
            Token name = Consume(IDENTIFIER, errorMsg);
            DeclareVariable(name);
            if (_ScopeDepth > SCOPE_GLOBAL) {
                return 0;
            }
            return IdentifierConstant(name);
        }

        /// <summary>
        /// Adds the given token's lexeme to the chunk's constant table as a string.
        /// Returns the index of that constant in the cosntant table.
        /// </summary>
        private byte IdentifierConstant(Token name) {
            return MakeConstant(name.Lexeme);
        }

        private void DeclareVariable(Token name) {
            if (_ScopeDepth == SCOPE_GLOBAL) {
                return;
            }
            for (int i = _LocalCount - 1; i >= 0; i--) {
                if (Locals[i].Depth != SCOPE_NONE && Locals[i].Depth < _ScopeDepth) {
                    break;
                }
                if (name.Lexeme == Locals[i].Name.Lexeme) {
                    throw new CompilerException(name, "Cannot redefine a local variable in the same scope.");
                }
            }
            AddLocal(name);
        }

        private void DefineVariable(byte global) {
            if (_ScopeDepth > SCOPE_GLOBAL) {
                MakeInitialized();
                return;
            }
            EmitBytes((byte)OP_DEFINE_GLOBAL, global);
        }

        private void MakeInitialized() {
            Locals[_LocalCount - 1].Depth = _ScopeDepth;
        }

        // === Statements ============================================================================================
        // ===========================================================================================================

        /// <summary>
        /// statement   → exprStmt
        ///             | forStmt
        ///             | ifStmt
        ///             | printStmt
        ///             | returnStmt
        ///             | whileStmt
        ///             | block ;
        /// </summary>
        private void Statement() {
            if (Match(FOR)) {
                ForStatement();
                return;
            }
            if (Match(IF)) {
                IfStatement();
                return;
            }
            if (Match(PRINT)) {
                PrintStatement();
                return;
            }
            if (Match(RETURN)) {
                ReturnStatement();
                return;
            }
            if (Match(WHILE)) {
                WhileStatement();
                return;
            }
            if (Match(LEFT_BRACE)) {
                BeginScope();
                Block();
                EndScope();
                return;
            }
            ExpressionStatement();
        }

        /// <summary>
        /// forStmt   → "for" "(" ( varDecl | exprStmt | ";" )
        ///                         expression? ";"
        ///                         expression? ")" statement ;
        /// </summary>
        private void ForStatement() {
            Consume(LEFT_PAREN, "Expect '(' after 'for'.");
            // initializer:
            if (Match(SEMICOLON)) {
                // !!! initializer = null;
            }
            else if (Match(VAR)) {
                VarDeclaration(); // !!! initializer = 
            }
            else {
                ExpressionStatement(); // !!! initializer = 
            }
            // condition:
            if (!Check(SEMICOLON)) {
                Expression(); // !!! condition = 
            }
            Consume(SEMICOLON, "Expect ';' after loop condition.");
            // increment:
            if (!Check(RIGHT_PAREN)) {
                Expression(); // !!! increment = 
            }
            Consume(RIGHT_PAREN, "Expect ')' after for clauses.");
            // body:
            Statement(); // !!! Stmt body = 
            // add increment, which must execute following the body in every iterlation of the loop:
            // !!! if (increment != null) {
                // !!! add increment to the very end of body:
                // !!! body = new Stmt.Block(new List<Stmt>() { body, new Stmt.Expres(increment) });
            // !!! }
            // add condition that will be checked for every iteration of the loop. If no condition, force to true:
            // !!! if (condition == null) {
                new Expr.Literal(true); // !!! default condition
            // !!! }
            // !!! body = new Stmt.While(condition, body);
            // add initializer, which runs once before the entire loop:
            // !!! if (initializer != null) {
            // !!!    body = new Stmt.Block(new List<Stmt>() { initializer, body });
            // !!! }
            // !!!! return body;
        }

        /// <summary>
        /// Follows 'if' token.
        /// </summary>
        private void IfStatement() {
            Consume(LEFT_PAREN, "Expect '(' after 'if'.");
            Expression();
            Consume(RIGHT_PAREN, "Expect ')' after if condition.");
            int thenJump = EmitJump(OP_JUMP_IF_FALSE);
            EmitBytes((byte)OP_POP);
            Statement(); // then statement
            int elseJump = EmitJump(OP_JUMP);
            PatchJump(thenJump);
            EmitBytes((byte)OP_POP);
            if (Match(ELSE)) {
                Statement();
            }
            PatchJump(elseJump);
        }

        /// <summary>
        /// printStmt → "print" expression ";" ;
        /// </summary>
        private void PrintStatement() {
            Expression();
            Consume(SEMICOLON, "Expect ';' after value.");
            EmitBytes((byte)OP_PRINT);
        }

        /// <summary>
        /// returnStmt → "return" expression? ";" ;
        /// </summary>
        private void ReturnStatement() {
            Previous(); // !!! Token keyword = 
            if (!Check(SEMICOLON)) {
                Expression(); // !!! value = 
            }
            Consume(SEMICOLON, "Expect ';' after return value.");
            return; // !!! return new Stmt.Return(keyword, value);
        }

        private void WhileStatement() {
            Consume(LEFT_PAREN, "Expect '(' after 'while'.");
            Expression(); // !!! Expr condition = 
            Consume(RIGHT_PAREN, "Expect ')' after condition.");
            Statement(); // !!! Stmt body = 
            return; // !!! return new Stmt.While(condition, body);
        }

        private void Block() {
            while (!Check(RIGHT_BRACE) && !IsAtEnd()) {
                Declaration();
            }
            Consume(RIGHT_BRACE, "Expect '}' after block.");
            return;
        }

        /// <summary>
        /// exprStmt  → expression ";" ;
        /// </summary>
        private void ExpressionStatement() {
            // semantically, an expression statement evaluates the expression and discards the result.
            Expression();
            Consume(SEMICOLON, "Expect ';' after expression.");
            EmitBytes((byte)OP_POP);
            return;
        }

        // === Expressions ===========================================================================================
        // ===========================================================================================================

        /// <summary>
        /// expression  → assignment ;
        /// </summary>
        private void Expression() {
            Assignment();
        }

        /// <summary>
        /// assignment  → ( call "." )? IDENTIFIER "=" assignment
        ///             | logic_or ;
        /// </summary>
        private void Assignment() {
            _CanAssign = true;
            Or(); // Expr expr = 
            /*if (Match(EQUAL)) {
                Token equals = Previous();
                Assignment();
                // Make sure the left-hand expression is a valid assignment target. If not, fail with a syntax error.
                if (target.Type == IDENTIFIER) {

                    return;
                }
                // !!! if (expr is Expr.Variable varExpr) {
                // !!!    Token name = varExpr.Name;
                    return; // !!! return new Expr.Assign(name, value);
                // !!! }
                // !!! else if (expr is Expr.Get getExpr) {
                // !!!     return new Expr.Set(getExpr.Obj, getExpr.Name, value);
                // !!! }
                throw new CompilerException(equals, "Invalid assignment target.");
            }*/
            //!!! return expr;
        }

        /// <summary>
        /// logic_or    → logic_and ( "or" logic_and)* ;
        /// </summary>
        /// <returns></returns>
        private void Or() {
            And(); // Expr expr = 
            while (Match(OR)) {
                _CanAssign = false;
                Token op = Previous();
                And(); // Expr right = 
                // !!! expr = new Expr.Logical(expr, op, right);
            }
            return; // !!! return expr;
        }

        /// <summary>
        /// logic_and   → equality ( "and" equality )* ;
        /// </summary>
        /// <returns></returns>
        private void And() {
            Equality(); // !!! Expr expr = 
            while (Match(AND)) {
                _CanAssign = false;
                Token op = Previous();
                Equality(); // !!! Expr right = 
                // !!! expr = new Expr.Logical(expr, op, right);
            }
            return; // !!! return expr;
        }

        /// <summary>
        /// equality    → comparison ( ( "!=" | "==" ) comparison )* ;
        /// </summary>
        private void Equality() {
            Comparison();
            while (Match(BANG_EQUAL, EQUAL_EQUAL)) {
                _CanAssign = false;
                Token op = Previous();
                Comparison();
                switch (op.Type) {
                    case BANG_EQUAL: EmitBytes((byte)OP_EQUAL, (byte)OP_NOT); break;
                    case EQUAL_EQUAL: EmitBytes((byte)OP_EQUAL); break;
                }
            }
            return;
        }

        /// <summary>
        /// comparison → addition ( ( ">" | ">=" | "&lt;" | "&lt;=" ) addition )* ;
        /// </summary>
        private void Comparison() {
            Addition();
            while (Match(GREATER, GREATER_EQUAL, LESS, LESS_EQUAL)) {
                _CanAssign = false;
                Token op = Previous();
                Addition();
                switch (op.Type) {
                    case GREATER: EmitBytes((byte)OP_GREATER); break;
                    case GREATER_EQUAL: EmitBytes((byte)OP_LESS, (byte)OP_NOT); break;
                    case LESS: EmitBytes((byte)OP_LESS); break; 
                    case LESS_EQUAL: EmitBytes((byte)OP_GREATER, (byte)OP_NOT); break; 
                }
            }
            return;
        }

        /// <summary>
        /// addition → multiplication ( ( "-" | "+" ) multiplication )* ;
        /// </summary>
        private void Addition() {
            Multiplication(); // gets the left operand
            while (Match(MINUS, PLUS)) {
                _CanAssign = false;
                Token op = Previous();
                Multiplication(); // gets the right operand
                switch (op.Type) {
                    case PLUS: EmitBytes((byte)OP_ADD); break;
                    case MINUS: EmitBytes((byte)OP_SUBTRACT); break;
                }
            }
            return;
        }

        /// <summary>
        /// multiplication → unary ( ( "/" | "*" ) unary )* ;
        /// </summary>
        private void Multiplication() {
            Unary(); // !!! Expr expr = 
            while (Match(SLASH, STAR)) {
                _CanAssign = false;
                Token op = Previous();
                Unary();
                switch (op.Type) {
                    case SLASH: EmitBytes((byte)OP_DIVIDE); break;
                    case STAR: EmitBytes((byte)OP_MULTIPLY); break;
                }
            }
            return; // !!! return expr;
        }

        /// <summary>
        /// unary → ( "!" | "-" ) unary | call ;
        /// </summary>
        private void Unary() {
            if (Match(BANG, MINUS)) {
                _CanAssign = false;
                Token op = Previous();
                Unary();
                switch (op.Type) {
                    case MINUS: EmitBytes((byte)OP_NEGATE); break;
                    case BANG: EmitBytes((byte)OP_NOT); break;
                }
                return; // !!! return new Expr.Unary(op, right);
            }
            Call(); // !!! return Call();
        }

        /// <summary>
        /// call → primary ( "(" arguments? ")" | "." IDENTIFIER )* ;
        /// 
        /// Matches a primary expression followed by zero or more function calls. If there are no parentheses, this
        /// parses a bare primary expression. Otherwise, each call is recognized by a pair of parentheses with an
        /// optional list of arguments inside.
        /// </summary>
        private void Call() {
            Primary(); // !!! Expr expr = 
            while (true) {
                if (Match(LEFT_PAREN)) {
                    FinishCall(); // !!! expr = FinishCall(expr)
                }
                else if (Match(DOT)) {
                    Token name = Consume(IDENTIFIER, "Expect a property name after '.'.");
                    // !!! expr = new Expr.Get(expr, name);
                }
                else {
                    break;
                }
            }
            // !!! return expr;
        }

        /// <summary>
        /// arguments → expression ( "," expression )* ;
        /// 
        /// Requires one or more argument expressions, followed by zero or more expressions each preceded by a comma.
        /// To handle zero-argument calls, the call rule itself considers the entire arguments production optional.
        /// </summary>
        private void FinishCall() { // !!! parameter Expr callee
            // List<Expr> arguments = new List<Expr>();
            int argumentCount = 0;
            if (!Check(RIGHT_PAREN)) {
                do {
                    if (argumentCount >= 255) {
                        throw new CompilerException(Peek(), "Cannot have more than 255 arguments.");
                    }
                    Expression(); // arguments.Add(Expression());
                    argumentCount += 1;
                } while (Match(COMMA));
            }
            Token paren = Consume(RIGHT_PAREN, "Expect ')' ending call operator parens (following any arguments).");
            return; // !!! return new Expr.Call(callee, paren, arguments);
        }

        /// <summary>
        /// primary → "false" | "true" | "nil" | "this"
        ///         | NUMBER | STRING | IDENTIFIER | "(" expression ")" 
        ///         | "super" "." IDENTIFIER ;
        /// </summary>
        private void Primary() {
            if (Match(FALSE)) {
                EmitBytes((byte)OP_FALSE);
                return;
            }
            if (Match(TRUE)) {
                EmitBytes((byte)OP_TRUE);
                return;
            }
            if (Match(NIL)) {
                EmitBytes((byte)OP_NIL);
                return;
            }
            if (Match(NUMBER)) {
                EmitConstant(Previous().LiteralAsNumber);
                return;
            }
            if (Match(STRING)) {
                EmitString(Previous().LiteralAsString);
                return; // !!! return new Expr.Literal(Previous().LiteralAsString);
            }
            if (Match(SUPER)) {
                Token keyword = Previous();
                Consume(DOT, "Expect '.' after 'super'.");
                Token method = Consume(IDENTIFIER, "Expect superclass method name.");
                return; // !!! return new Expr.Super(keyword, method);
            }
            if (Match(THIS)) {
                return; // !!! return new Expr.This(Previous());
            }
            if (Match(IDENTIFIER)) {
                NamedVariable(Previous());
                return; // !!! return new Expr.Variable(Previous());
            }
            if (Match(LEFT_PAREN)) {
                Expression(); // Expr expr = 
                Consume(RIGHT_PAREN, "Expect ')' after expression.");
                return; // !!! return new Expr.Grouping(expr);
            }
            throw new CompilerException(Peek(), "Expect expression.");
        }

        private void NamedVariable(Token name) {
            EGearsOpCode getOp, setOp;
            int arg = ResolveLocal(name);
            if (arg != -1) {
                getOp = OP_GET_LOCAL;
                setOp = OP_SET_LOCAL;
            }
            else {
                arg = IdentifierConstant(name);
                getOp = OP_GET_GLOBAL;
                setOp = OP_SET_GLOBAL;
            }
            if (Match(EQUAL)) {
                if (!_CanAssign) {
                    throw new CompilerException(name, $"Invalid assignment target '{name}'.");
                }
                Expression();
                EmitBytes((byte)setOp, (byte)arg);
            }
            else {
                EmitBytes((byte)getOp, (byte)arg);
            }
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
                _CurrentToken++;
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
            return _Tokens[_CurrentToken];
        }

        /// <summary>
        /// returns the most recently consumed token.
        /// </summary>
        private Token Previous() {
            return _Tokens[_CurrentToken - 1];
        }

        // === Emit Infrastructure ===================================================================================
        // ===========================================================================================================

        private void EmitBytes(params byte[] bytes) {
            foreach (byte b in bytes) {
                Chunk.Write(b);
            }
        }

        private void EmitConstant(double value) {
            EmitBytes((byte)OP_CONSTANT, MakeConstant(value));
        }

        private void EmitLoop(int loopStart) {
            EmitBytes((byte)OP_LOOP);
            int offset = Chunk.CodeSize - loopStart + 2;
            if (offset > ushort.MaxValue) {
                throw new CompilerException(Peek(), "Loop body too large.");
            }
            EmitBytes((byte)((offset >> 8) & 0xff), (byte)(offset & 0xff));
        }

        private int EmitJump(EGearsOpCode instruction) {
            EmitBytes((byte)instruction, 0xff, 0xff);
            return Chunk.CodeSize - 2;
        }

        private void EmitReturn() {
            /*if (_Type == EFunctionType.TYPE_INITIALIZER) {
                EmitBytes((byte)OP_GET_LOCAL, 0);
            }
            else {
                EmitBytes((byte)OP_NIL);
            }*/
            EmitBytes((byte)OP_RETURN);
        }

        private void EmitString(string value) {
            EmitBytes((byte)OP_STRING, MakeConstant(value));
        }

        private byte MakeConstant(double value) {
            int index = Chunk.AddConstant(value);
            if (index > byte.MaxValue) {
                throw new CompilerException(Peek(), "Too many constants in one chunk.");
            }
            return (byte)index;
        }

        private byte MakeConstant(string value) {
            int index = Chunk.AddConstant(value);
            if (index > byte.MaxValue) {
                throw new CompilerException(Peek(), "Too many constants in one chunk.");
            }
            return (byte)index;
        }

        private void PatchJump(int offset) {
            // -2 to adjust for the bytecode for the jump offset itself.
            int jump = Chunk.CodeSize - offset - 2;
            if (jump > ushort.MaxValue) {
                throw new CompilerException(Peek(), "Too much code to jump over.");
            }
            Chunk.WriteAt(offset, (byte)((jump >> 8) & 0xff));
            Chunk.WriteAt(offset + 1, (byte)(jump & 0xff));
        }

        // === Scope and Locals ======================================================================================
        // ===========================================================================================================

        struct CompilerLocal {
            public readonly Token Name;
            public int Depth;

            public CompilerLocal(Token name, int depth) {
                Name = name;
                Depth = depth;
            }

            public override string ToString() => $"{Name.Lexeme}";
        }

        private void BeginScope() {
            _ScopeDepth += 1;
        }

        private void EndScope() {
            _ScopeDepth -= 1;
            while (_LocalCount > 0 && Locals[_LocalCount - 1].Depth > _ScopeDepth) {
                EmitBytes((byte)OP_POP); // todo: pop many at once?
                _LocalCount -= 1;
            }
        }

        private void AddLocal(Token name) {
            if (_LocalCount >= MAX_LOCALS) {
                throw new CompilerException(name, "Too many local variables in scope.");
            }
            Locals[_LocalCount++] = new CompilerLocal(name, SCOPE_NONE);
        }

        private int ResolveLocal(Token name) {
            for (int i = _LocalCount - 1; i >= 0; i--) {
                if (Locals[i].Name.Lexeme == name.Lexeme) {
                    if (Locals[i].Depth == -1) {
                        throw new CompilerException(name, "Cannot read local variable in its own initializer.");
                    }
                    return i;
                }
            }
            return -1;
        }

        // === What type of function are we compiling? ===============================================================
        // ===========================================================================================================

        private enum EFunctionType {
            TYPE_FUNCTION,
            TYPE_INITIALIZER,
            TYPE_METHOD,
            TYPE_SCRIPT
        }

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
        private class CompilerException : Exception {
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
