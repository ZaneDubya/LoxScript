using LoxScript.Grammar;
using LoxScript.Scanning;
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
        public static bool TryCompile(TokenList tokens, out GearsObjFunction fn, out string status) {
            Compiler compiler = new Compiler(tokens, EFunctionType.TYPE_SCRIPT, null);
            if (compiler.Compile(out fn)) {
                status = null;
                return true;
            }
            fn = null;
            status = compiler._ErrorMessage;
            return false;
        }

        // === Instance ==============================================================================================
        // ===========================================================================================================

        // input and error:
        private TokenList _Tokens;
        private string _ErrorMessage = null;
        private bool _HadError = false;

        // Compiling code to:
        private GearsObjFunction _Function;
        private EFunctionType _FunctionType;
        private bool _CanAssign = false;

        // scope and locals (locals are references to variables in scope; these are stored on the stack at runtime):
        private const int SCOPE_GLOBAL = 0;
        private const int SCOPE_NONE = -1;
        private const int MAX_LOCALS = 256;
        private CompilerLocal[] LocalVarData = new CompilerLocal[MAX_LOCALS];
        private int _LocalCount = 0;
        private int _ScopeDepth = SCOPE_GLOBAL;

        private Compiler(TokenList tokens, EFunctionType type, string name) {
            _Tokens = tokens;
            _FunctionType = type;
            _Function = new GearsObjFunction(name, 0);
            // reserve stack slow zero for VM's own internal use.
            // Give it an empty name so the user can't write an identifier that refers to it:
            LocalVarData[_LocalCount++] = new CompilerLocal(string.Empty, 0);
        }

        private GearsChunk Chunk => _Function.Chunk;

        // === Declarations and Statements ===========================================================================
        // ===========================================================================================================

        /// <summary>
        /// program     → declaration* EOF ;
        /// Called from TryCompile.
        /// </summary>
        internal bool Compile(out GearsObjFunction fn) {
            while (!_Tokens.IsAtEnd()) {
                try {
                    Declaration();
                }
                catch (CompilerException e) {
                    _HadError = true;
                    e.Print();
                    Synchronize();
                }
            }
            fn = EndCompiler();
            return !_HadError;
        }

        private GearsObjFunction EndCompiler() {
            EmitReturn();
            return _Function;
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
                if (_Tokens.Match(CLASS)) {
                    ClassDeclaration();
                    return;
                }
                if (_Tokens.Match(FUNCTION)) {
                    FunctionDeclaration("function");
                    return;
                }
                if (_Tokens.Match(VAR)) {
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
            Token name = _Tokens.Consume(IDENTIFIER, "Expect class name.");
            Expr.Variable superClass = null;
            if (_Tokens.Match(LESS)) {
                _Tokens.Consume(IDENTIFIER, "Expect superclass name.");
                superClass = new Expr.Variable(_Tokens.Previous());
            }
            _Tokens.Consume(LEFT_BRACE, "Expect '{' before class body.");
            while (!_Tokens.Check(RIGHT_BRACE) && !_Tokens.IsAtEnd()) {
                // !!! we are adding methods to the class...
                FunctionDeclaration("method");
            }
            _Tokens.Consume(RIGHT_BRACE, "Expect '}' after class body.");
            return; // !!! return new Stmt.Class(name, superClass, methods);
        }

        /// <summary>
        /// function    → IDENTIFIER "(" parameters? ")" block ;
        /// parameters  → IDENTIFIER( "," IDENTIFIER )* ;
        /// </summary>
        private void FunctionDeclaration(string kind) {
            int global = ParseVariable($"Expect {kind} name.");
            MarkInitialized();
            Compiler fnCompiler = new Compiler(_Tokens, EFunctionType.TYPE_FUNCTION, _Tokens.Previous().Lexeme);
            fnCompiler.Function();
            GearsObjFunction fn = fnCompiler.EndCompiler();
            EmitConstant(fn);
            Emit(OP_CLOSURE);
            DefineVariable(global);
        }

        private void Function() {
            BeginScope();
            _Tokens.Consume(LEFT_PAREN, $"Expect '(' after function name.");
            // parameter list:
            if (!_Tokens.Check(RIGHT_PAREN)) {
                do {
                    int paramConstant = ParseVariable("Expect parameter name.");
                    DefineVariable(paramConstant);
                    if (++_Function.Arity >= 255) {
                        throw new CompilerException(_Tokens.Peek(), "Cannot have more than 255 parameters.");
                    }
                } while (_Tokens.Match(COMMA));
            }
            _Tokens.Consume(RIGHT_PAREN, "Expect ')' after parameters.");
            // body:
            _Tokens.Consume(LEFT_BRACE, "Expect '{' before function body.");
            Block();
            // no need for end scope, as functions are each compiled by their own compiler object.
        }

        private void VarDeclaration() {
            int global = ParseVariable("Expect variable name.");
            if (_Tokens.Match(EQUAL)) {
                Expression(); // var x = value;
            }
            else {
                Emit(OP_NIL); // equivalent to var x = nil;
            }
            _Tokens.Consume(SEMICOLON, "Expect ';' after variable declaration.");
            DefineVariable(global);
            return;
        }

        private int ParseVariable(string errorMsg) {
            Token name = _Tokens.Consume(IDENTIFIER, errorMsg);
            DeclareVariable(name);
            if (_ScopeDepth > SCOPE_GLOBAL) {
                return 0;
            }
            return IdentifierConstant(name);
        }

        /// <summary>
        /// Adds the given token's lexeme to the chunk's constant table as a string.
        /// Returns the index of that constant in the constant table.
        /// </summary>
        private int IdentifierConstant(Token name) {
            return MakeConstant(name.Lexeme);
        }

        private void DeclareVariable(Token name) {
            if (_ScopeDepth == SCOPE_GLOBAL) {
                return;
            }
            for (int i = _LocalCount - 1; i >= 0; i--) {
                if (LocalVarData[i].Depth != SCOPE_NONE && LocalVarData[i].Depth < _ScopeDepth) {
                    break;
                }
                if (name.Lexeme == LocalVarData[i].Name) {
                    throw new CompilerException(name, "Cannot redefine a local variable in the same scope.");
                }
            }
            AddLocal(name);
        }

        private void DefineVariable(int global) {
            if (_ScopeDepth > SCOPE_GLOBAL) {
                MarkInitialized();
                return;
            }
            Emit(OP_DEFINE_GLOBAL, (byte)((global >> 8) & 0xff), (byte)(global & 0xff));
        }

        /// <summary>
        /// Indicates that the last parsed local variable is initialized with a value.
        /// </summary>
        private void MarkInitialized() {
            if (_ScopeDepth == SCOPE_GLOBAL) {
                return;
            }
            LocalVarData[_LocalCount - 1].Depth = _ScopeDepth;
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
            if (_Tokens.Match(FOR)) {
                ForStatement();
                return;
            }
            if (_Tokens.Match(IF)) {
                IfStatement();
                return;
            }
            if (_Tokens.Match(PRINT)) {
                PrintStatement();
                return;
            }
            if (_Tokens.Match(RETURN)) {
                ReturnStatement();
                return;
            }
            if (_Tokens.Match(WHILE)) {
                WhileStatement();
                return;
            }
            if (_Tokens.Match(LEFT_BRACE)) {
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
            BeginScope();
            _Tokens.Consume(LEFT_PAREN, "Expect '(' after 'for'.");
            // initializer:
            if (_Tokens.Match(SEMICOLON)) {
                // no initializer.
            }
            else if (_Tokens.Match(VAR)) {
                VarDeclaration();
            }
            else {
                ExpressionStatement();
            }
            // loop just before the condition:
            int loopStart = Chunk.CodeSize;
            // loop condition:
            int exitJump = -1;
            if (!_Tokens.Match(SEMICOLON)) {
                Expression();
                _Tokens.Consume(SEMICOLON, "Expect ';' after loop condition.");
                // jump out of the for loop if the condition if false.
                exitJump = EmitJump(OP_JUMP_IF_FALSE);
                Emit(OP_POP);
            }
            // increment:
            if (!_Tokens.Match(RIGHT_PAREN)) {
                int bodyJump = EmitJump(OP_JUMP);
                int incrementStart = Chunk.CodeSize;
                Expression();
                Emit(OP_POP);
                _Tokens.Consume(RIGHT_PAREN, "Expect ')' after for clauses.");
                EmitLoop(loopStart);
                loopStart = incrementStart;
                PatchJump(bodyJump);
            }
            // body:
            Statement();
            EmitLoop(loopStart);
            if (exitJump != -1) {
                // we only do this if there is a condition clause that might skip the for loop entirely.
                PatchJump(exitJump);
                Emit(OP_POP);
            }
            EndScope();
        }

        /// <summary>
        /// Follows 'if' token.
        /// </summary>
        private void IfStatement() {
            _Tokens.Consume(LEFT_PAREN, "Expect '(' after 'if'.");
            Expression();
            _Tokens.Consume(RIGHT_PAREN, "Expect ')' after if condition.");
            int thenJump = EmitJump(OP_JUMP_IF_FALSE);
            Emit(OP_POP);
            Statement(); // then statement
            int elseJump = EmitJump(OP_JUMP);
            PatchJump(thenJump);
            Emit(OP_POP);
            if (_Tokens.Match(ELSE)) {
                Statement();
            }
            PatchJump(elseJump);
        }

        /// <summary>
        /// printStmt → "print" expression ";" ;
        /// </summary>
        private void PrintStatement() {
            Expression();
            _Tokens.Consume(SEMICOLON, "Expect ';' after value.");
            Emit(OP_PRINT);
        }

        /// <summary>
        /// returnStmt → "return" expression? ";" ;
        /// </summary>
        private void ReturnStatement() {
            if (_FunctionType == EFunctionType.TYPE_SCRIPT) {
                throw new CompilerException(_Tokens.Previous(), "Can't return from top-level code.");
            }
            if (_Tokens.Match(SEMICOLON)) {
                EmitReturn();
            }
            else {
                Expression();
                _Tokens.Consume(SEMICOLON, "Expect ';' after return value.");
                Emit(OP_RETURN);
            }
        }

        private void WhileStatement() {
            int loopStart = Chunk.CodeSize; // code point just before the condition
            _Tokens.Consume(LEFT_PAREN, "Expect '(' after 'while'.");
            Expression();
            _Tokens.Consume(RIGHT_PAREN, "Expect ')' after condition.");
            int exitJump = EmitJump(OP_JUMP_IF_FALSE);
            Emit(OP_POP);
            Statement();
            EmitLoop(loopStart);
            PatchJump(exitJump);
            Emit(OP_POP);
        }

        private void Block() {
            while (!_Tokens.Check(RIGHT_BRACE) && !_Tokens.IsAtEnd()) {
                Declaration();
            }
            _Tokens.Consume(RIGHT_BRACE, "Expect '}' after block.");
            return;
        }

        /// <summary>
        /// exprStmt  → expression ";" ;
        /// </summary>
        private void ExpressionStatement() {
            // semantically, an expression statement evaluates the expression and discards the result.
            Expression();
            _Tokens.Consume(SEMICOLON, "Expect ';' after expression.");
            Emit(OP_POP);
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
            /*if (_Tokens.Match(EQUAL)) {
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
        private void Or() {
            And();
            while (_Tokens.Match(OR)) {
                _CanAssign = false;
                int elseJump = EmitJump(OP_JUMP_IF_FALSE);
                int endJump = EmitJump(OP_JUMP);
                PatchJump(elseJump);
                Emit(OP_POP);
                And();
                PatchJump(endJump);
            }
        }

        /// <summary>
        /// logic_and   → equality ( "and" equality )* ;
        /// </summary>
        private void And() {
            Equality();
            while (_Tokens.Match(AND)) {
                _CanAssign = false;
                int endJump = EmitJump(OP_JUMP_IF_FALSE);
                Emit(OP_POP);
                Equality();
                PatchJump(endJump);
            }
        }

        /// <summary>
        /// equality    → comparison ( ( "!=" | "==" ) comparison )* ;
        /// </summary>
        private void Equality() {
            Comparison();
            while (_Tokens.Match(BANG_EQUAL, EQUAL_EQUAL)) {
                _CanAssign = false;
                Token op = _Tokens.Previous();
                Comparison();
                switch (op.Type) {
                    case BANG_EQUAL: Emit(OP_EQUAL, OP_NOT); break;
                    case EQUAL_EQUAL: Emit(OP_EQUAL); break;
                }
            }
        }

        /// <summary>
        /// comparison → addition ( ( ">" | ">=" | "&lt;" | "&lt;=" ) addition )* ;
        /// </summary>
        private void Comparison() {
            Addition();
            while (_Tokens.Match(GREATER, GREATER_EQUAL, LESS, LESS_EQUAL)) {
                _CanAssign = false;
                Token op = _Tokens.Previous();
                Addition();
                switch (op.Type) {
                    case GREATER: Emit(OP_GREATER); break;
                    case GREATER_EQUAL: Emit(OP_LESS, OP_NOT); break;
                    case LESS: Emit(OP_LESS); break; 
                    case LESS_EQUAL: Emit(OP_GREATER, OP_NOT); break; 
                }
            }
        }

        /// <summary>
        /// addition → multiplication ( ( "-" | "+" ) multiplication )* ;
        /// </summary>
        private void Addition() {
            Multiplication();
            while (_Tokens.Match(MINUS, PLUS)) {
                _CanAssign = false;
                Token op = _Tokens.Previous();
                Multiplication();
                switch (op.Type) {
                    case PLUS: Emit(OP_ADD); break;
                    case MINUS: Emit(OP_SUBTRACT); break;
                }
            }
        }

        /// <summary>
        /// multiplication → unary ( ( "/" | "*" ) unary )* ;
        /// </summary>
        private void Multiplication() {
            Unary();
            while (_Tokens.Match(SLASH, STAR)) {
                _CanAssign = false;
                Token op = _Tokens.Previous();
                Unary();
                switch (op.Type) {
                    case SLASH: Emit(OP_DIVIDE); break;
                    case STAR: Emit(OP_MULTIPLY); break;
                }
            }
        }

        /// <summary>
        /// unary → ( "!" | "-" ) unary | call ;
        /// </summary>
        private void Unary() {
            if (_Tokens.Match(BANG, MINUS)) {
                _CanAssign = false;
                Token op = _Tokens.Previous();
                Unary();
                switch (op.Type) {
                    case MINUS: Emit(OP_NEGATE); break;
                    case BANG: Emit(OP_NOT); break;
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
            Primary();
            while (true) {
                if (_Tokens.Match(LEFT_PAREN)) {
                    FinishCall(); // !!! expr = FinishCall(expr)
                }
                else if (_Tokens.Match(DOT)) {
                    Token name = _Tokens.Consume(IDENTIFIER, "Expect a property name after '.'.");
                    // !!! expr = new Expr.Get(expr, name);
                }
                else {
                    break;
                }
            }
        }

        /// <summary>
        /// arguments → expression ( "," expression )* ;
        /// 
        /// Requires one or more argument expressions, followed by zero or more expressions each preceded by a comma.
        /// To handle zero-argument calls, the call rule itself considers the entire arguments production optional.
        /// </summary>
        private void FinishCall() {
            int argumentCount = 0;
            if (!_Tokens.Check(RIGHT_PAREN)) {
                do {
                    if (argumentCount >= 255) {
                        throw new CompilerException(_Tokens.Peek(), "Cannot have more than 255 arguments.");
                    }
                    Expression();
                    argumentCount += 1;
                } while (_Tokens.Match(COMMA));
            }
            Token paren = _Tokens.Consume(RIGHT_PAREN, "Expect ')' ending call operator parens (following any arguments).");
            Emit(OP_CALL, (byte)argumentCount);
        }

        /// <summary>
        /// primary → "false" | "true" | "nil" | "this"
        ///         | NUMBER | STRING | IDENTIFIER | "(" expression ")" 
        ///         | "super" "." IDENTIFIER ;
        /// </summary>
        private void Primary() {
            if (_Tokens.Match(FALSE)) {
                Emit(OP_FALSE);
                return;
            }
            if (_Tokens.Match(TRUE)) {
                Emit(OP_TRUE);
                return;
            }
            if (_Tokens.Match(NIL)) {
                Emit(OP_NIL);
                return;
            }
            if (_Tokens.Match(NUMBER)) {
                EmitConstant(_Tokens.Previous().LiteralAsNumber);
                return;
            }
            if (_Tokens.Match(STRING)) {
                EmitConstant(_Tokens.Previous().LiteralAsString);
                return;
            }
            if (_Tokens.Match(SUPER)) {
                Token keyword = _Tokens.Previous();
                _Tokens.Consume(DOT, "Expect '.' after 'super'.");
                Token method = _Tokens.Consume(IDENTIFIER, "Expect superclass method name.");
                return; // !!! return new Expr.Super(keyword, method);
            }
            if (_Tokens.Match(THIS)) {
                return; // !!! return new Expr.This(Previous());
            }
            if (_Tokens.Match(IDENTIFIER)) {
                NamedVariable(_Tokens.Previous());
                return; // !!! return new Expr.Variable(Previous());
            }
            if (_Tokens.Match(LEFT_PAREN)) {
                Expression();
                _Tokens.Consume(RIGHT_PAREN, "Expect ')' after expression.");
                return;
            }
            throw new CompilerException(_Tokens.Peek(), "Expect expression.");
        }

        private void NamedVariable(Token name) {
            EGearsOpCode getOp, setOp;
            int index = ResolveLocal(name);
            if (index != -1) {
                getOp = OP_GET_LOCAL;
                setOp = OP_SET_LOCAL;
            }
            else {
                index = IdentifierConstant(name);
                getOp = OP_GET_GLOBAL;
                setOp = OP_SET_GLOBAL;
            }
            if (_Tokens.Match(EQUAL)) {
                if (!_CanAssign) {
                    throw new CompilerException(name, $"Invalid assignment target '{name}'.");
                }
                Expression();
                Emit((byte)setOp, (byte)((index >> 8) & 0xff), (byte)(index & 0xff));
            }
            else {
                Emit((byte)getOp, (byte)((index >> 8) & 0xff), (byte)(index & 0xff));
            }
        }

        // === Emit Infrastructure ===================================================================================
        // ===========================================================================================================

        private void Emit(EGearsOpCode opcode0, EGearsOpCode opcode1, params byte[] data) {
            Chunk.Write(opcode0);
            Chunk.Write(opcode1);
            Emit(data);
        }

        private void Emit(EGearsOpCode opcode, params byte[] data) {
            Chunk.Write(opcode);
            Emit(data);
        }

        private void Emit(params byte[] data) {
            foreach (byte i in data) {
                Chunk.Write(i);
            }
        }

        private void EmitLoop(int loopStart) {
            Emit(OP_LOOP);
            int offset = Chunk.CodeSize - loopStart + 2;
            if (offset > ushort.MaxValue) {
                throw new CompilerException(_Tokens.Peek(), "Loop body too large.");
            }
            Emit((byte)((offset >> 8) & 0xff), (byte)(offset & 0xff));
        }

        private int EmitJump(EGearsOpCode instruction) {
            Emit((byte)instruction, 0xff, 0xff);
            return Chunk.CodeSize - 2;
        }

        private void PatchJump(int offset) {
            // We adjust by two for the jump offset
            int jump = Chunk.CodeSize - offset - 2;
            if (jump > ushort.MaxValue) {
                throw new CompilerException(_Tokens.Peek(), "Too much code to jump over.");
            }
            Chunk.WriteAt(offset, (byte)((jump >> 8) & 0xff));
            Chunk.WriteAt(offset + 1, (byte)(jump & 0xff));
        }

        /// <summary>
        /// Lox implicitly returns nil when no return value is specified.
        /// </summary>
        private void EmitReturn() {
            /*if (_FunctionType == EFunctionType.TYPE_INITIALIZER) {
                Emit(OP_GET_LOCAL, 0);
            }
            else {*/
                Emit(OP_NIL);
            // }
            Emit(OP_RETURN);
        }

        // === Constants =============================================================================================
        // ===========================================================================================================

        private void EmitConstant(GearsValue value) {
            int index = MakeConstant(value);
            Emit(OP_CONSTANT, (byte)((index >> 8) & 0xff), (byte)(index & 0xff));
        }

        private void EmitConstant(string value) {
            int index = MakeConstant(value);
            Emit(OP_STRING, (byte)((index >> 8) & 0xff), (byte)(index & 0xff));
        }

        private void EmitConstant(GearsObjFunction fn) {
            int index = fn.Serialize(Chunk);
            Emit(OP_FUNCTION, (byte)((index >> 8) & 0xff), (byte)(index & 0xff));
        }

        private int MakeConstant(GearsValue value) {
            int index = Chunk.WriteConstantValue(value);
            if (index > short.MaxValue) {
                throw new CompilerException(_Tokens.Peek(), "Too many constants in one chunk.");
            }
            return index;
        }

        private int MakeConstant(string value) {
            int index = Chunk.WriteConstantString(value);
            if (index > short.MaxValue) {
                throw new CompilerException(_Tokens.Peek(), "Too many constants in one chunk.");
            }
            return index;
        }

        // === Scope and Locals ======================================================================================
        // ===========================================================================================================

        struct CompilerLocal {
            public readonly string Name;
            public int Depth;

            public CompilerLocal(string name, int depth) {
                Name = name;
                Depth = depth;
            }

            public override string ToString() => $"{Name}";
        }

        private void BeginScope() {
            _ScopeDepth += 1;
        }

        private void EndScope() {
            _ScopeDepth -= 1;
            while (_LocalCount > 0 && LocalVarData[_LocalCount - 1].Depth > _ScopeDepth) {
                Emit(OP_POP); // todo: pop many at once?
                _LocalCount -= 1;
            }
        }

        private void AddLocal(Token name) {
            if (_LocalCount >= MAX_LOCALS) {
                throw new CompilerException(name, "Too many local variables in scope.");
            }
            LocalVarData[_LocalCount++] = new CompilerLocal(name.Lexeme, SCOPE_NONE);
        }

        private int ResolveLocal(Token name) {
            for (int i = _LocalCount - 1; i >= 0; i--) {
                if (LocalVarData[i].Name == name.Lexeme) {
                    if (LocalVarData[i].Depth == -1) {
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
            // TYPE_INITIALIZER,
            // TYPE_METHOD,
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
            _Tokens.Advance();
            while (!_Tokens.IsAtEnd()) {
                if (_Tokens.Previous().Type == SEMICOLON) {
                    return;
                }
                switch (_Tokens.Peek().Type) {
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
                _Tokens.Advance();
            }
        }
    }
}
