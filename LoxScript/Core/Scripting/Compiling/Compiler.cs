using System;
using System.Collections.Generic;
using XPT.Core.Scripting.VirtualMachine;
using static XPT.Core.Scripting.Compiling.TokenType;
using static XPT.Core.Scripting.VirtualMachine.EGearsOpCode;

namespace XPT.Core.Scripting.Compiling {
    /// <summary>
    /// Compiler parses a TokenList and compiles it into a GearsChunk: bytecode executed by Gears.
    /// </summary>
    class Compiler {
        /// <summary>
        /// Attempts to compile the passed list of tokens.
        /// If compilation is successful, compiler.Chunk will be set.
        /// If compilation fails, status will be the error message.
        /// </summary>
        public static bool TryCompile(TokenList tokens, out GearsChunk chunk, out string status) {
            Compiler compiler = new Compiler(tokens, EFunctionType.TYPE_SCRIPT, null, null, null);
            if (compiler.Compile()) {
                chunk = compiler._Chunk;
                status = null;
                return true;
            }
            chunk = null;
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
        private readonly string Name;
        private int Arity;
        private readonly GearsChunk _Chunk;
        private readonly EFunctionType _FunctionType;
        private CompilerClass _CurrentClass;
        private bool _CanAssign = false;
        private int _OriginAddress = 0;
        private readonly List<Compiler> _FixupFns = new List<Compiler>();
        private readonly List<CompilerFixup> _FixupConstants = new List<CompilerFixup>();
        private readonly List<CompilerFixup> _FixupStrings = new List<CompilerFixup>();
        private readonly Dictionary<ulong, string> _OptimizedStrings = new Dictionary<ulong, string>();

        // scope and locals (locals are references to variables in scope; these are stored on the stack at runtime):
        private readonly Compiler _EnclosingCompiler;
        private const int SCOPE_GLOBAL = 0;
        private const int SCOPE_NONE = -1;
        private const int MAX_LOCALS = 256;
        private const int MAX_UPVALUES = 256;
        private int _ScopeDepth = SCOPE_GLOBAL;
        private int _LocalCount = 0;
        private int _UpvalueCount = 0;
        private readonly CompilerLocal[] _LocalVarData = new CompilerLocal[MAX_LOCALS];
        private readonly CompilerUpvalue[] _UpvalueData = new CompilerUpvalue[MAX_UPVALUES];

        private Compiler(TokenList tokens, EFunctionType type, string name, Compiler enclosing, CompilerClass enclosingClass) {
            _Tokens = tokens;
            _FunctionType = type;
            Name = name;
            Arity = 0;
            _Chunk = new GearsChunk();
            _EnclosingCompiler = enclosing;
            _CurrentClass = enclosingClass;
            // stack slot zero is used for 'this' reference in methods, and is empty for script/functions:
            if (type != EFunctionType.TYPE_FUNCTION) {
                _LocalVarData[_LocalCount++] = new CompilerLocal("this", 0);
            }
            else {
                _LocalVarData[_LocalCount++] = new CompilerLocal(string.Empty, 0);
            }
        }

        private void EndCompiler() {
            EmitReturn();
            if (_EnclosingCompiler == null) {
                DoFixups(_Chunk, 0, MakeValueConstant, MakeStringConstant, _FixupFns);
            }
        }

        private static void DoFixups(GearsChunk chunk, int origin, Func<GearsValue, int> makeConstant, Func<string, int> makeConstant2, List<Compiler> fns) {
            foreach (Compiler fn in fns) {
                int codeBase = chunk.CodeSize;
                fn._Chunk.Compress();
                chunk.WriteCode(fn._Chunk._Code);
                chunk.WriteCodeAt(origin + fn._OriginAddress, (byte)(codeBase >> 8));
                chunk.WriteCodeAt(origin + fn._OriginAddress + 1, (byte)(codeBase & 0xff));
                foreach (CompilerFixup fixup in fn._FixupConstants) {
                    GearsValue value = fn._Chunk.ReadConstantValue(fixup.Value);
                    int constantFixup = makeConstant(value); // as fixup
                    chunk.WriteCodeAt(codeBase + fixup.Address, (byte)(constantFixup >> 8));
                    chunk.WriteCodeAt(codeBase + fixup.Address + 1, (byte)(constantFixup & 0xff));
                }
                foreach (CompilerFixup fixup in fn._FixupStrings) {
                    string value = fn._Chunk.ReadStringConstant(fixup.Value);
                    int constantFixup = makeConstant2(value); // as fixup
                    chunk.WriteCodeAt(codeBase + fixup.Address, (byte)(constantFixup >> 8));
                    chunk.WriteCodeAt(codeBase + fixup.Address + 1, (byte)(constantFixup & 0xff));
                }
                DoFixups(chunk, codeBase, makeConstant, makeConstant2, fn._FixupFns);
                chunk.Compress();
            }
        }

        private void AddFixup(Compiler fn) {
            _FixupFns.Add(fn);
            fn._OriginAddress = _Chunk.CodeSize;
            EmitData(0, 0); // fn jump - to fix up
        }

        public override string ToString() => $"Compiling {Name}";

        // === Declarations and Statements ===========================================================================
        // ===========================================================================================================

        /// <summary>
        /// program     → declaration* EOF ;
        /// Called from TryCompile.
        /// </summary>
        internal bool Compile() {
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
            EndCompiler();
            return !_HadError;
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
                    FunctionDeclaration("function", EFunctionType.TYPE_FUNCTION);
                    return;
                }
                if (_Tokens.Match(VAR)) {
                    GlobalVarDeclaration();
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
            Token className = _Tokens.Consume(IDENTIFIER, "Expect class name.");
            int nameConstant = MakeStringConstant(className.Lexeme); // no fixup needed
            DeclareVariable(className); // The class name binds the class object type to a variable of the same name.
            // todo? make the class declaration an expression, require explicit binding of class to variable (like var Pie = new Pie()); 27.2
            EmitOpcode(OP_CLASS);
            EmitConstantIndex(nameConstant, _FixupStrings);
            DefineVariable(MakeBitStrConstant(className.Lexeme));
            _CurrentClass = new CompilerClass(className, _CurrentClass);
            // superclass:
            if (_Tokens.Match(LESS)) { 
                _Tokens.Consume(IDENTIFIER, "Expect superclass name.");
                if (_Tokens.Previous().Lexeme == className.Lexeme) {
                    throw new CompilerException(_Tokens.Previous(), "A class cannot inherit from itself.");
                }
                NamedVariable(_Tokens.Previous(), false); // push super class onto stack
                BeginScope();
                AddLocal(MakeSyntheticToken(SUPER, "super", _Tokens.Previous().Line));
                DefineVariable(0);
                NamedVariable(className); // push class onto stack
                EmitOpcode(OP_INHERIT);
                _CurrentClass.HasSuperClass = true;
            }
            NamedVariable(className); // push class onto stack
            // body:
            _Tokens.Consume(LEFT_BRACE, "Expect '{' before class body.");
            while (!_Tokens.Check(RIGHT_BRACE) && !_Tokens.IsAtEnd()) {
                // lox doesn't have field declarations, so anything before the brace that ends the class body must be a method.
                // initializer is a special method
                MethodDeclaration("method", EFunctionType.TYPE_METHOD);
            }
            _Tokens.Consume(RIGHT_BRACE, "Expect '}' after class body.");
            EmitOpcode(OP_POP); // pop class from stack
            if (_CurrentClass.HasSuperClass) {
                EndScope();
            }
            _CurrentClass = _CurrentClass.Enclosing;
            return;
        }

        /// <summary>
        /// function    → IDENTIFIER "(" parameters? ")" block ;
        /// parameters  → IDENTIFIER( "," IDENTIFIER )* ;
        /// </summary>
        private void FunctionDeclaration(string fnType, EFunctionType fnType2) {
            int global = ParseVariable($"Expect {fnType} name.");
            MarkInitialized();
            string fnName = _Tokens.Previous().Lexeme;
            Compiler fnCompiler = new Compiler(_Tokens, fnType2, fnName, this, _CurrentClass);
            fnCompiler.FunctionBody();
            fnCompiler.EndCompiler();
            EmitOpcode(OP_LOAD_FUNCTION);
            EmitData((byte)fnCompiler.Arity);
            // EmitConstantIndex(MakeBitStrConstant(fnName), _FixupConstants); // has fixup
            AddFixup(fnCompiler);
            // EmitOpcode(OP_CLOSURE);
            EmitData((byte)fnCompiler._UpvalueCount);
            for (int i = 0; i < fnCompiler._UpvalueCount; i++) {
                EmitData((byte)(fnCompiler._UpvalueData[i].IsLocal ? 1 : 0));
                EmitData((byte)(fnCompiler._UpvalueData[i].Index));
            }
            DefineVariable(global);
        }

        /// <summary>
        /// Todo: merge this with FunctionDeclaration
        /// </summary>
        private void MethodDeclaration(string fnType, EFunctionType fnType2) {
            _Tokens.Consume(IDENTIFIER, $"Expect {fnType} name.");
            if (_Tokens.Previous().Lexeme == "init") {
                fnType2 = EFunctionType.TYPE_INITIALIZER;
            }
            string fnName = _Tokens.Previous().Lexeme;
            Compiler fnCompiler = new Compiler(_Tokens, fnType2, fnName, this, _CurrentClass);
            fnCompiler.FunctionBody();
            fnCompiler.EndCompiler();
            EmitOpcode(OP_LOAD_FUNCTION);
            EmitData((byte)fnCompiler.Arity);
            // EmitConstantIndex(MakeBitStrConstant(fnName), _FixupConstants); // has fixup
            AddFixup(fnCompiler);
            EmitData((byte)fnCompiler._UpvalueCount);
            for (int i = 0; i < fnCompiler._UpvalueCount; i++) {
                EmitData((byte)(fnCompiler._UpvalueData[i].IsLocal ? 1 : 0));
                EmitData((byte)(fnCompiler._UpvalueData[i].Index));
            }
            EmitOpcode(OP_METHOD);
            EmitConstantIndex(MakeBitStrConstant(fnName), _FixupConstants); // has fixup
        }

        private void FunctionBody() {
            BeginScope();
            _Tokens.Consume(LEFT_PAREN, $"Expect '(' after function name.");
            // parameter list:
            if (!_Tokens.Check(RIGHT_PAREN)) {
                do {
                    int paramConstant = ParseVariable("Expect parameter name.");
                    DefineVariable(paramConstant);
                    if (++Arity >= 255) {
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

        private void GlobalVarDeclaration() {
            int global = ParseVariable("Expect variable name.");
            if (_Tokens.Match(EQUAL)) {
                Expression(); // var x = value;
            }
            else {
                EmitOpcode(OP_NIL); // equivalent to var x = nil;
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
            return MakeBitStrConstant(name.Lexeme);
        }

        private void DeclareVariable(Token name) {
            if (_ScopeDepth == SCOPE_GLOBAL) {
                return;
            }
            for (int i = _LocalCount - 1; i >= 0; i--) {
                if (_LocalVarData[i].Depth != SCOPE_NONE && _LocalVarData[i].Depth < _ScopeDepth) {
                    break;
                }
                if (name.Lexeme == _LocalVarData[i].Name) {
                    throw new CompilerException(name, "Cannot redefine a local variable in the same scope.");
                }
            }
            AddLocal(name);
        }

        /// <summary>
        /// For global variables, emits the bitstring that identifies the variable. For local variables, marks them as initialized. No fixup needed.
        /// </summary>
        private void DefineVariable(int global) {
            if (_ScopeDepth > SCOPE_GLOBAL) {
                MarkInitialized();
                return;
            }
            EmitOpcode(OP_DEFINE_GLOBAL);
            EmitData((byte)((global >> 8) & 0xff), (byte)(global & 0xff));
        }

        /// <summary>
        /// Indicates that the last parsed local variable is initialized with a value.
        /// </summary>
        private void MarkInitialized() {
            if (_ScopeDepth == SCOPE_GLOBAL) {
                return;
            }
            _LocalVarData[_LocalCount - 1].Depth = _ScopeDepth;
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
                GlobalVarDeclaration();
            }
            else {
                ExpressionStatement();
            }
            // loop just before the condition:
            int loopStart = _Chunk.CodeSize;
            // loop condition:
            int exitJump = -1;
            if (!_Tokens.Match(SEMICOLON)) {
                Expression();
                _Tokens.Consume(SEMICOLON, "Expect ';' after loop condition.");
                // jump out of the for loop if the condition if false.
                exitJump = EmitJump(OP_JUMP_IF_FALSE);
                EmitOpcode(OP_POP);
            }
            // increment:
            if (!_Tokens.Match(RIGHT_PAREN)) {
                int bodyJump = EmitJump(OP_JUMP);
                int incrementStart = _Chunk.CodeSize;
                Expression();
                EmitOpcode(OP_POP);
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
                EmitOpcode(OP_POP);
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
            EmitOpcode(OP_POP);
            Statement(); // then statement
            int elseJump = EmitJump(OP_JUMP);
            PatchJump(thenJump);
            EmitOpcode(OP_POP);
            if (_Tokens.Match(ELSE)) {
                Statement();
            }
            PatchJump(elseJump);
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
                if (_FunctionType == EFunctionType.TYPE_INITIALIZER) {
                    throw new CompilerException(_Tokens.Previous(), "Can't return a value from a class initializer.");
                }
                Expression();
                _Tokens.Consume(SEMICOLON, "Expect ';' after return value.");
                EmitOpcode(OP_RETURN);
            }
        }

        private void WhileStatement() {
            int loopStart = _Chunk.CodeSize; // code point just before the condition
            _Tokens.Consume(LEFT_PAREN, "Expect '(' after 'while'.");
            Expression();
            _Tokens.Consume(RIGHT_PAREN, "Expect ')' after condition.");
            int exitJump = EmitJump(OP_JUMP_IF_FALSE);
            EmitOpcode(OP_POP);
            Statement();
            EmitLoop(loopStart);
            PatchJump(exitJump);
            EmitOpcode(OP_POP);
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
            EmitOpcode(OP_POP);
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
            Or();
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
                EmitOpcode(OP_POP);
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
                EmitOpcode(OP_POP);
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
                    case BANG_EQUAL:
                        EmitOpcode(OP_EQUAL);
                        EmitOpcode(OP_NOT);
                        break;
                    case EQUAL_EQUAL:
                        EmitOpcode(OP_EQUAL);
                        break;
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
                    case GREATER:
                        EmitOpcode(OP_GREATER);
                        break;
                    case GREATER_EQUAL:
                        EmitOpcode(OP_LESS);
                        EmitOpcode(OP_NOT);
                        break;
                    case LESS:
                        EmitOpcode(OP_LESS);
                        break; 
                    case LESS_EQUAL:
                        EmitOpcode(OP_GREATER);
                        EmitOpcode(OP_NOT);
                        break; 
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
                    case PLUS: EmitOpcode(OP_ADD); break;
                    case MINUS: EmitOpcode(OP_SUBTRACT); break;
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
                    case SLASH: EmitOpcode(OP_DIVIDE); break;
                    case STAR: EmitOpcode(OP_MULTIPLY); break;
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
                    case MINUS: EmitOpcode(OP_NEGATE); break;
                    case BANG: EmitOpcode(OP_NOT); break;
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
                    FinishCall(OP_CALL);
                }
                else if (_Tokens.Match(DOT)) {
                    Token name = _Tokens.Consume(IDENTIFIER, "Expect a property name after '.'.");
                    int nameConstant = MakeBitStrConstant(name.Lexeme); // needs fixup
                    if (_CanAssign && _Tokens.Match(EQUAL)) {
                        Expression();
                        EmitOpcode(OP_SET_PROPERTY);
                        EmitConstantIndex(nameConstant, _FixupConstants);
                    }
                    else if (_Tokens.Match(LEFT_PAREN)) {
                        FinishCall(OP_INVOKE);
                        EmitConstantIndex(nameConstant, _FixupConstants);
                    }
                    else {
                        EmitOpcode(OP_GET_PROPERTY);
                        EmitConstantIndex(nameConstant, _FixupConstants);
                    }
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
        private void FinishCall(EGearsOpCode opCode) {
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
            EmitOpcode(opCode);
            EmitData((byte)argumentCount);
        }

        /// <summary>
        /// primary → "false" | "true" | "nil" | "this"
        ///         | NUMBER | STRING | IDENTIFIER | "(" expression ")" 
        ///         | "super" "." IDENTIFIER ;
        /// </summary>
        private void Primary() {
            if (_Tokens.Match(FALSE)) {
                EmitOpcode(OP_FALSE);
                return;
            }
            if (_Tokens.Match(TRUE)) {
                EmitOpcode(OP_TRUE);
                return;
            }
            if (_Tokens.Match(NIL)) {
                EmitOpcode(OP_NIL);
                return;
            }
            if (_Tokens.Match(NUMBER)) {
                EmitOpcode(OP_LOAD_CONSTANT);
                EmitConstantIndex(MakeValueConstant(_Tokens.Previous().LiteralAsNumber), _FixupConstants);
                return;
            }
            if (_Tokens.Match(STRING)) {
                EmitOpcode(OP_LOAD_STRING);
                EmitConstantIndex(MakeStringConstant(_Tokens.Previous().LiteralAsString), _FixupStrings);
                return;
            }
            if (_Tokens.Match(SUPER)) {
                if (_CurrentClass == null) {
                    throw new CompilerException(_Tokens.Previous(), "Can't use 'super' outside of a class.");
                }
                if (!_CurrentClass.HasSuperClass) {
                    throw new CompilerException(_Tokens.Previous(), "Can't use 'super' in a class with no superclass.");
                }
                Token keyword = _Tokens.Previous();
                _Tokens.Consume(DOT, "Expect '.' after 'super'.");
                Token methodName = _Tokens.Consume(IDENTIFIER, "Expect superclass method name.");
                int nameIndex = MakeBitStrConstant(methodName.Lexeme);
                NamedVariable(MakeSyntheticToken(THIS, "this", 0), false); // look up this - load instance onto stack
                if (_Tokens.Match(LEFT_PAREN)) {
                    FinishCall(OP_SUPER_INVOKE);
                    NamedVariable(MakeSyntheticToken(SUPER, "super", 0), false); // look up this.super - load superclass of instance
                    EmitConstantIndex(nameIndex, _FixupConstants);
                }
                else {
                    NamedVariable(MakeSyntheticToken(SUPER, "super", 0), false); // look up this.super - load superclass of instance
                    EmitOpcode(OP_GET_SUPER); // look up super.name - encode name of method to access as operand
                    EmitConstantIndex(nameIndex, _FixupConstants);
                }
                return;
            }
            if (_Tokens.Match(THIS)) {
                if (_CurrentClass == null) {
                    throw new CompilerException(_Tokens.Previous(), $"Cannot use 'this' outside of a class.");
                }
                // need to emit 'this' reference, but don't allow assignment:
                NamedVariable(_Tokens.Previous(), false);
                return;
            }
            if (_Tokens.Match(IDENTIFIER)) {
                NamedVariable(_Tokens.Previous());
                return;
            }
            if (_Tokens.Match(LEFT_PAREN)) {
                Expression();
                _Tokens.Consume(RIGHT_PAREN, "Expect ')' after expression.");
                return;
            }
            throw new CompilerException(_Tokens.Peek(), "Expect expression.");
        }

        /// <summary>
        /// Walks the block scopes for the current function from innermost to outermost. If we don't find the
        /// variable in the function, we try to resolve the reference to variables in enclosing functions. If
        /// we can't find a variable in an enclosing function, we assume it is a global variable.
        /// </summary>
        private void NamedVariable(Token name, bool overrideCanAssign = true) {
            EGearsOpCode getOp, setOp;
            int index = ResolveLocal(name);
            bool needsFixup = false;
            if (index != -1) {
                getOp = OP_GET_LOCAL;
                setOp = OP_SET_LOCAL;
            }
            else if ((index = ResolveUpvalue(name)) != -1) {
                getOp = OP_GET_UPVALUE;
                setOp = OP_SET_UPVALUE;
            }
            else {
                index = MakeBitStrConstant(name.Lexeme); // has fixup
                getOp = OP_GET_GLOBAL;
                setOp = OP_SET_GLOBAL;
                needsFixup = true;
            }
            if (_Tokens.Match(EQUAL)) {
                if (!(_CanAssign & overrideCanAssign)) {
                    throw new CompilerException(name, $"Invalid assignment target '{name}'.");
                }
                Expression();
                EmitData((byte)setOp);
                EmitConstantIndex(index, needsFixup ? _FixupConstants : null);
            }
            else {
                EmitData((byte)getOp);
                EmitConstantIndex(index, needsFixup ? _FixupConstants : null);
            }
        }

        /// <summary>
        /// Called after failing to find a local in the current scope. Checks for a local in the enclosing scope.
        /// </summary>
        private int ResolveUpvalue(Token name) {
            if (_EnclosingCompiler == null) {
                return -1;
            }
            int local = _EnclosingCompiler.ResolveLocal(name);
            if (local != -1) {
                _EnclosingCompiler._LocalVarData[local].IsCaptured = true;
                return AddUpvalue(name, local, true);
            }
            int upvalue = _EnclosingCompiler.ResolveUpvalue(name);
            if (upvalue != -1) {
                return AddUpvalue(name, upvalue, false);
            }
            return -1;
        }

        /// <summary>
        /// Creates an upvalue which tracks the closed-over identifier, allowing the inner function to access the variable.
        /// </summary>
        private int AddUpvalue(Token name, int index, bool isLocal) {
            for (int i = 0; i < _UpvalueCount; i++) {
                if (_UpvalueData[i].Index == index && _UpvalueData[i].IsLocal == isLocal) {
                    return i;
                }
            }
            if (_UpvalueCount >= MAX_UPVALUES) {
                throw new CompilerException(name, $"Too many closure variables in scope.");
            }
            int upvalueIndex = _UpvalueCount++;
            _UpvalueData[upvalueIndex] = new CompilerUpvalue(index, isLocal);
            return upvalueIndex;
        }

        // === Emit Infrastructure ===================================================================================
        // ===========================================================================================================

        private void EmitOpcode(EGearsOpCode opcode) {
            _Chunk.WriteCode(opcode);
        }

        private void EmitData(params byte[] data) {
            foreach (byte i in data) {
                _Chunk.WriteCode(i);
            }
        }

        private void EmitLoop(int loopStart) {
            EmitOpcode(OP_LOOP);
            int offset = _Chunk.CodeSize - loopStart + 2;
            if (offset > ushort.MaxValue) {
                throw new CompilerException(_Tokens.Peek(), "Loop body too large.");
            }
            EmitData((byte)((offset >> 8) & 0xff), (byte)(offset & 0xff));
        }

        private int EmitJump(EGearsOpCode instruction) {
            EmitData((byte)instruction, 0xff, 0xff);
            return _Chunk.CodeSize - 2;
        }

        private void PatchJump(int offset) {
            // We adjust by two for the jump offset
            int jump = _Chunk.CodeSize - offset - 2;
            if (jump > ushort.MaxValue) {
                throw new CompilerException(_Tokens.Peek(), "Too much code to jump over.");
            }
            _Chunk.WriteCodeAt(offset, (byte)((jump >> 8) & 0xff));
            _Chunk.WriteCodeAt(offset + 1, (byte)(jump & 0xff));
        }

        /// <summary>
        /// Lox implicitly returns nil when no return value is specified.
        /// </summary>
        private void EmitReturn() {
            if (_FunctionType == EFunctionType.TYPE_INITIALIZER) {
                EmitOpcode(OP_GET_LOCAL);
                EmitConstantIndex(0); // no fixup required
            }
            else {
                EmitOpcode(OP_NIL);
            }
            EmitOpcode(OP_RETURN);
        }

        // === Constants =============================================================================================
        // ===========================================================================================================

        private void EmitConstantIndex(int index, List<CompilerFixup> fixups = null) {
            if (fixups != null) {
                fixups.Add(new CompilerFixup(_Chunk.CodeSize, index));
            }
            EmitData((byte)((index >> 8) & 0xff), (byte)(index & 0xff));
        }

        /// <summary>
        /// Adds the given value to the chunk's constant table.
        /// Returns the index of that constant in the constant table.
        /// </summary>
        private int MakeValueConstant(GearsValue value) {
            for (int i = 0; i < _Chunk.ConstantSize; i++) {
                if ((ulong)_Chunk.ReadConstantValue(i) == (ulong)value) {
                    return i;
                }
            }
            int index = _Chunk.WriteConstantValue(value);
            if (index > short.MaxValue) {
                throw new CompilerException(_Tokens.Peek(), "Too many constants in one chunk.");
            }
            return index;
        }

        /// <summary>
        /// Adds the given string to the chunk's string table.
        /// Returns the index of that string in the string table.
        /// </summary>
        private int MakeStringConstant(string value) {
            for (int i = 0; i < _Chunk.StringTableSize; i++) {
                if (_Chunk.ReadStringConstant(i) == value) {
                    return i;
                }
            }
            int index = _Chunk.WriteStringConstant(value);
            if (index > short.MaxValue) {
                throw new CompilerException(_Tokens.Previous(), "Too many constants in one chunk.");
            }
            return index;
        }

        private int MakeBitStrConstant(string value) {
            ulong bitstr = BitString.GetBitStr(value);
            if (_OptimizedStrings.TryGetValue(bitstr, out string optimized)) {
                if (optimized != value) {
                    throw new CompilerException(_Tokens.Previous(), $"String collision: '{value}' != '{optimized}'. First 10 characters of all identifiers must be distinct.");
                }
            }
            _OptimizedStrings[bitstr] = value;
            return MakeValueConstant(bitstr);
        }

        private Token MakeSyntheticToken(TokenType type, string name, int line) {
            return new Token(type, line, name, 0, name.Length);
        }

        // === Scope and Locals ======================================================================================
        // ===========================================================================================================

        private void BeginScope() {
            _ScopeDepth += 1;
        }

        private void EndScope() {
            _ScopeDepth -= 1;
            while (_LocalCount > 0 && _LocalVarData[_LocalCount - 1].Depth > _ScopeDepth) {
                if (_LocalVarData[_LocalCount - 1].IsCaptured) {
                    EmitOpcode(OP_CLOSE_UPVALUE);
                }
                else {
                    EmitOpcode(OP_POP);
                }
                _LocalCount -= 1;
            }
        }

        private void AddLocal(Token name) {
            if (_LocalCount >= MAX_LOCALS) {
                throw new CompilerException(name, "Too many local variables in scope.");
            }
            _LocalVarData[_LocalCount++] = new CompilerLocal(name.Lexeme, SCOPE_NONE);
        }

        private int ResolveLocal(Token name) {
            for (int i = _LocalCount - 1; i >= 0; i--) {
                if (_LocalVarData[i].Name == name.Lexeme) {
                    if (_LocalVarData[i].Depth == SCOPE_NONE) {
                        throw new CompilerException(name, "Cannot read local variable in its own initializer.");
                    }
                    return i;
                }
            }
            return -1;
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
                    case RETURN:
                        return;
                }
                _Tokens.Advance();
            }
        }
    }
}
