using System;
using System.Collections.Generic;
using System.IO;
using XPT.Core.Scripting.Base;
using XPT.Core.Scripting.LoxScript.VirtualMachine;
using XPT.Core.Scripting.Rules;
using XPT.Core.Scripting.Rules.Compiling;
using static XPT.Core.Scripting.Base.TokenTypes;
using static XPT.Core.Scripting.LoxScript.Compiling.LoxTokenTypes;
using static XPT.Core.Scripting.LoxScript.VirtualMachine.EGearsOpCode;

namespace XPT.Core.Scripting.LoxScript.Compiling {
    /// <summary>
    /// LoxCompiler parses a TokenList and compiles it into a GearsChunk, which is bytecode executed by the Gears VM.
    /// </summary>
    internal sealed partial class LoxCompiler : ACompiler {
        /// <summary>
        /// Attempts to compile the passed source, tokenizing first.
        /// If compilation is successful, compiler.Chunk will be set.
        /// If compilation fails, status will be the error message.
        /// </summary>
        public static bool TryCompileFromPath(string path, out GearsChunk chunk, out string status, out int? line) {
            chunk = null;
            line = null;
            string source;
            if (!File.Exists(path)) {
                status = $"LoxCompiler: File '{path}' does not exist.";
                return false;
            }
            try {
                source = File.ReadAllText(path);
            }
            catch (Exception e) {
                status = $"LoxCompiler: Could not read '{path}': {e.Message}";
                return false;
            }
            return TryCompileFromSource(path, source, out chunk, out status, out line);
        }

        /// <summary>
        /// Attempts to compile the passed source, tokenizing first.
        /// If compilation is successful, compiler.Chunk will be set.
        /// If compilation fails, status will be the error message.
        /// </summary>
        public static bool TryCompileFromSource(string path, string source, out GearsChunk chunk, out string status, out int? line) {
            try {
                TokenList tokens = new LoxTokenizer(path, source).ScanTokens();
                LoxCompiler compiler = new LoxCompiler(tokens, ELoxFunctionType.TYPE_SCRIPT, path, null, null, null);
                if (compiler.Compile()) {
                    chunk = compiler._Chunk;
                    status = null;
                    line = null;
                    return true;
                }
                status = $"LoxCompiler: uncaught error while compiling {path}.";
                chunk = null;
                line = null;
                return false;
            }
            catch (CompilerException e) {
                chunk = null;
                status = $"LoxCompiler: error at {e.TargetSite.DeclaringType.Name}.{e.TargetSite.Name} while compiling {path}, error={e}";
                line = e.Token.Line;
                return false;
            }
        }

        // === Instance ==============================================================================================
        // ===========================================================================================================

        // Compiling code to:
        private int Arity;
        private readonly GearsChunk _Chunk;
        private readonly RuleCollection _Rules;
        private readonly ELoxFunctionType _FunctionType;
        private LoxCompilerClass _CurrentClass;
        private bool _CanAssign = false;
        private bool _InSwitchStatement = false; // no nested switches
        private int _OriginAddress = 0;

        // scope and locals (locals are references to variables in scope; these are stored on the stack at runtime):
        private readonly LoxCompiler _EnclosingCompiler;
        private const int SCOPE_GLOBAL = 0;
        private const int SCOPE_NONE = -1;
        private const int MAX_LOCALS = 256;
        private const int MAX_UPVALUES = 256;
        private const int MAX_PARAMS = 255;
        private int _ScopeDepth = SCOPE_GLOBAL;
        private int _LocalCount = 0;
        private int _UpvalueCount = 0;
        private readonly LoxCompilerLocal[] _LocalVarData = new LoxCompilerLocal[MAX_LOCALS];
        private readonly LoxCompilerUpvalue[] _UpvalueData = new LoxCompilerUpvalue[MAX_UPVALUES];

        private LoxCompiler(TokenList tokens, ELoxFunctionType type, string name, LoxCompiler enclosing, LoxCompilerClass enclosingClass, GearsChunk containerChunk)
            : base(tokens, name) {
            _FunctionType = type;
            Arity = 0;
            _Chunk = new GearsChunk(name, containerChunk);
            _Rules = new RuleCollection();
            _EnclosingCompiler = enclosing;
            _CurrentClass = enclosingClass;
            // stack slot zero is used for 'this' reference in methods, and is empty for script/functions:
            if (type != ELoxFunctionType.TYPE_FUNCTION) {
                _LocalVarData[_LocalCount++] = new LoxCompilerLocal("this", 0);
            }
            else {
                _LocalVarData[_LocalCount++] = new LoxCompilerLocal(string.Empty, 0);
            }
        }

        protected override void EndCompiler() {
            EmitReturn();
            if (_EnclosingCompiler == null) {
                DoFixups(_Chunk, 0, MakeValueConstant, MakeStringConstant, _FixupFns);
                _Chunk.Rules = _Rules;
            }
        }

        // === Fixups ================================================================================================
        // ===========================================================================================================

        private readonly List<LoxCompiler> _FixupFns = new List<LoxCompiler>();
        private readonly List<LoxCompilerFixup> _FixupConstants = new List<LoxCompilerFixup>();
        // private readonly List<LoxCompilerFixup> _FixupStrings = new List<LoxCompilerFixup>();  // got rid of fixup 19-02-2025

        private static void DoFixups(GearsChunk chunk, int origin, Func<GearsValue, int> makeConstantValue, Func<string, int> makeConstantString, List<LoxCompiler> fns) {
            foreach (LoxCompiler fn in fns) {
                int codeBase = chunk.SizeCode;
                chunk.WriteCode(fn._Chunk.Code, fn._Chunk.Lines, fn._Chunk.SizeCode);
                chunk.WriteCodeAt(origin + fn._OriginAddress, (byte)(codeBase >> 8));
                chunk.WriteCodeAt(origin + fn._OriginAddress + 1, (byte)(codeBase & 0xff));
                foreach (LoxCompilerFixup fixup in fn._FixupConstants) {
                    GearsValue value = fn._Chunk.ReadConstantValue(fixup.Value);
                    int constantFixup = makeConstantValue(value); // as fixup
                    chunk.WriteCodeAt(codeBase + fixup.Address, (byte)(constantFixup >> 8));
                    chunk.WriteCodeAt(codeBase + fixup.Address + 1, (byte)(constantFixup & 0xff));
                }
                // got rid of fixup 19-02-2025
                /*foreach (LoxCompilerFixup fixup in fn._FixupStrings) {
                    string value = fn._Chunk.VarNameStrings.ReadStringConstant(fixup.Value);
                    int constantFixup = makeConstantString(value); // as fixup
                    chunk.WriteCodeAt(codeBase + fixup.Address, (byte)(constantFixup >> 8));
                    chunk.WriteCodeAt(codeBase + fixup.Address + 1, (byte)(constantFixup & 0xff));
                }*/
                DoFixups(chunk, codeBase, makeConstantValue, makeConstantString, fn._FixupFns);
                chunk.Compress();
            }
        }

        private void AddFixup(int line, LoxCompiler fn) {
            _FixupFns.Add(fn);
            fn._OriginAddress = _Chunk.SizeCode;
            EmitData(line, 0, 0); // fn jump - to fix up
        }

        // === Error Handling ========================================================================================
        // ===========================================================================================================

        /// <summary>
        /// Discards tokens until it thinks it found a statement boundary. After catching a ParseError, we’ll call this
        /// and then we are hopefully back in sync. When it works well, we have discarded tokens that would have likely
        /// caused cascaded errors anyway and now we can parse the rest of the file starting at the next statement.
        /// </summary>
        protected override void Synchronize() {
            Tokens.Advance();
            while (!Tokens.IsAtEnd()) {
                if (Tokens.Previous().Type == SEMICOLON) {
                    return;
                }
                switch (Tokens.Peek().Type) {
                    case CLASS:
                    case FUNCTION:
                    case VAR:
                    case FOR:
                    case IF:
                    case WHILE:
                    case RETURN:
                        return;
                }
                Tokens.Advance();
            }
        }

        // === Declarations ==========================================================================================
        // ===========================================================================================================

        /// <summary>
        /// declaration → [public] "class" class
        ///             | [public] "fun" function
        ///             | [public] varDecl
        ///             | statement ;
        /// </summary>
        protected override void Declaration() {
            try {
                if (Tokens.Match(CLASS)) {
                    ClassDeclaration();
                    return;
                }
                if (Tokens.Match(FUNCTION)) {
                    FunctionDeclaration("function", ELoxFunctionType.TYPE_FUNCTION);
                    return;
                }
                if (Tokens.Match(VAR)) {
                    GlobalVarDeclaration();
                    return;
                }
                if (Tokens.Match(LEFT_BRACKET)) {
                    RuleDeclaration();
                    return;
                }
                Statement();
            }
            catch {
                Synchronize();
                throw;
            }
        }

        /// <summary>
        /// classDecl   → "class" IDENTIFIER ( "&lt;" IDENTIFIER )? 
        ///               "{" function* "}" ;
        /// </summary>
        private void ClassDeclaration() {
            Token className = Tokens.Consume(IDENTIFIER, "Expect class name.");
            int nameConstant = MakeVarNameConstant(className.Lexeme); // no fixup needed
            DeclareVariable(className); // The class name binds the class object type to a variable of the same name.
            // todo? make the class declaration an expression, require explicit binding of class to variable (like var Pie = new Pie()); 27.2
            EmitOpcode(className.Line, OP_CLASS);
            EmitConstantIndex(className.Line, nameConstant, null); // got rid of fixup 19-02-2025
            DefineVariable(className.Line, MakeVarNameConstant(className.Lexeme)); // no fixup needed
            _CurrentClass = new LoxCompilerClass(className, _CurrentClass);
            // superclass:
            if (Tokens.Match(LESS)) {
                Tokens.Consume(IDENTIFIER, "Expect superclass name.");
                if (Tokens.Previous().Lexeme == className.Lexeme) {
                    throw new CompilerException(Tokens.Previous(), "A class cannot inherit from itself.");
                }
                NamedVariable(Tokens.Previous(), false); // push super class onto stack
                BeginScope();
                AddLocal(MakeSyntheticToken(SUPER, "super", LineOfLastToken));
                DefineVariable(LineOfLastToken, 0);
                NamedVariable(className); // push class onto stack
                EmitOpcode(className.Line, OP_INHERIT);
                _CurrentClass.HasSuperClass = true;
            }
            NamedVariable(className); // push class onto stack
            // body:
            Tokens.Consume(LEFT_BRACE, "Expect '{' before class body.");
            while (!Tokens.Check(RIGHT_BRACE) && !Tokens.IsAtEnd()) {
                // lox doesn't have field declarations, so anything before the brace that ends the class body must be a method.
                // initializer is a special method
                MethodDeclaration("method", ELoxFunctionType.TYPE_METHOD);
            }
            Tokens.Consume(RIGHT_BRACE, "Expect '}' after class body.");
            EmitOpcode(LineOfLastToken, OP_POP); // pop class from stack
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
        private void FunctionDeclaration(string fnTypeName, ELoxFunctionType fnType) {
            int global = ParseVariable($"Expect {fnTypeName} name.");
            MarkInitialized();
            string fnName = Tokens.Previous().Lexeme;
            int fnLine = LineOfLastToken;
            LoxCompiler fnCompiler = new LoxCompiler(Tokens, fnType, fnName, this, _CurrentClass, _Chunk);
            fnCompiler.FunctionBody();
            fnCompiler.EndCompiler();
            EmitOpcode(fnLine, OP_LOAD_FUNCTION);
            EmitData(fnLine, (byte)fnCompiler.Arity);
            // EmitConstantIndex(MakeBitStrConstant(fnName), _FixupConstants); // has fixup
            AddFixup(fnLine, fnCompiler);
            // EmitOpcode(OP_CLOSURE);
            EmitData(fnLine, (byte)fnCompiler._UpvalueCount);
            for (int i = 0; i < fnCompiler._UpvalueCount; i++) {
                EmitData(fnLine, (byte)(fnCompiler._UpvalueData[i].IsLocal ? 1 : 0));
                EmitData(fnLine, (byte)(fnCompiler._UpvalueData[i].Index));
            }
            DefineVariable(fnLine, global);
        }

        /// <summary>
        /// Declares a method (a function in a class).
        /// Possible todo: merge this with FunctionDeclaration, as they share a lot of code.
        /// </summary>
        private void MethodDeclaration(string fnType, ELoxFunctionType fnType2) {
            Tokens.Consume(IDENTIFIER, $"Expect {fnType} name.");
            if (Tokens.Previous().Lexeme == "init") {
                fnType2 = ELoxFunctionType.TYPE_INITIALIZER;
            }
            string fnName = Tokens.Previous().Lexeme;
            int fnLine = LineOfLastToken;
            LoxCompiler fnCompiler = new LoxCompiler(Tokens, fnType2, fnName, this, _CurrentClass, _Chunk);
            fnCompiler.FunctionBody();
            fnCompiler.EndCompiler();
            EmitOpcode(fnLine, OP_LOAD_FUNCTION);
            EmitData(fnLine, (byte)fnCompiler.Arity);
            // EmitConstantIndex(MakeBitStrConstant(fnName), _FixupConstants); // has fixup
            AddFixup(fnLine, fnCompiler);
            EmitData(fnLine, (byte)fnCompiler._UpvalueCount);
            for (int i = 0; i < fnCompiler._UpvalueCount; i++) {
                EmitData(fnLine, (byte)(fnCompiler._UpvalueData[i].IsLocal ? 1 : 0));
                EmitData(fnLine, (byte)(fnCompiler._UpvalueData[i].Index));
            }
            EmitOpcode(fnLine, OP_METHOD);
            EmitConstantIndex(fnLine, MakeVarNameConstant(fnName), null); // no fixup
        }

        private void FunctionBody() {
            BeginScope();
            Tokens.Consume(LEFT_PAREN, $"Expect '(' after function name.");
            // parameter list:
            if (!Tokens.Check(RIGHT_PAREN)) {
                do {
                    int paramConstant = ParseVariable("Expect parameter name.");
                    DefineVariable(LineOfLastToken, paramConstant);
                    if (++Arity >= MAX_PARAMS) {
                        throw new CompilerException(Tokens.Peek(), $"Cannot have more than {MAX_PARAMS} parameters.");
                    }
                } while (Tokens.Match(COMMA));
            }
            Tokens.Consume(RIGHT_PAREN, "Expect ')' after parameters.");
            // body:
            Tokens.Consume(LEFT_BRACE, "Expect '{' before function body.");
            Block();
            // no need for end scope, as functions are each compiled by their own compiler object.
        }

        private void GlobalVarDeclaration() {
            int global = ParseVariable("Expect variable name.");
            if (Tokens.Match(EQUAL)) {
                Expression(); // var x = value;
            }
            else {
                EmitOpcode(LineOfLastToken, OP_NIL); // equivalent to var x = nil;
            }
            Tokens.Consume(SEMICOLON, "Expect ';' after variable declaration.");
            DefineVariable(LineOfLastToken, global);
            return;
        }

        private int ParseVariable(string errorMsg) {
            Token name = Tokens.Consume(IDENTIFIER, errorMsg);
            DeclareVariable(name);
            // only global variables have their names stored in the constant table.
            if (_ScopeDepth > SCOPE_GLOBAL) {
                return 0;
            }
            return MakeVarNameConstant(name.Lexeme);
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
        private void DefineVariable(int line, int global) {
            if (_ScopeDepth > SCOPE_GLOBAL) {
                MarkInitialized();
                return;
            }
            EmitOpcode(line, OP_DEFINE_GLOBAL);
            EmitData(LineOfLastToken, (byte)((global >> 8) & 0xff), (byte)(global & 0xff));
        }

        /// <summary>
        /// Creates a rule - metadata that allows invocation of a function on
        /// </summary>
        private void RuleDeclaration() {
            if (_EnclosingCompiler != null) {
                throw new CompilerException(Tokens.Peek(), "Rules must be globally scoped.");
            }
            // rule conditions
            RuleCompiler.TryCompile(Tokens, out string triggerName, out RuleCondition[] conditions);
            // following function
            if (Tokens.Peek().Type == FUNCTION && Tokens.Peek(1).Type == IDENTIFIER) {
                string functionName = Tokens.Peek(1).Lexeme;
                _Rules.AddRule(new Rule(triggerName, functionName, conditions));
            }
            else {
                throw new CompilerException(Tokens.Peek(), "Rule declaration must be followed by function.");
            }
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
            if (Tokens.Match(FOR)) {
                ForStatement();
                return;
            }
            if (Tokens.Match(IF)) {
                IfStatement();
                return;
            }
            if (Tokens.Match(SWITCH)) {
                SwitchStatement();
                return;
            }
            if (Tokens.Match(RETURN)) {
                ReturnStatement();
                return;
            }
            if (Tokens.Match(WHILE)) {
                WhileStatement();
                return;
            }
            if (Tokens.Match(LEFT_BRACE)) {
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
            Tokens.Consume(LEFT_PAREN, "Expect '(' after 'for'.");
            // initializer:
            if (Tokens.Match(SEMICOLON)) {
                // no initializer.
            }
            else if (Tokens.Match(VAR)) {
                GlobalVarDeclaration();
            }
            else {
                ExpressionStatement();
            }
            // loop just before the condition:
            int loopStart = _Chunk.SizeCode;
            // loop condition:
            int exitJump = -1;
            if (!Tokens.Match(SEMICOLON)) {
                Expression();
                Tokens.Consume(SEMICOLON, "Expect ';' after loop condition.");
                // jump out of the for loop if the condition if false.
                exitJump = EmitJump(OP_JUMP_IF_FALSE);
                EmitOpcode(LineOfLastToken, OP_POP);
            }
            // increment:
            if (!Tokens.Match(RIGHT_PAREN)) {
                int bodyJump = EmitJump(OP_JUMP);
                int incrementStart = _Chunk.SizeCode;
                Expression();
                EmitOpcode(LineOfLastToken, OP_POP);
                Tokens.Consume(RIGHT_PAREN, "Expect ')' after for clauses.");
                EmitLoop(loopStart, LineOfLastToken);
                loopStart = incrementStart;
                PatchJump(bodyJump);
            }
            // body:
            Statement();
            EmitLoop(loopStart, LineOfLastToken);
            if (exitJump != -1) {
                // we only do this if there is a condition clause that might skip the for loop entirely.
                PatchJump(exitJump);
                EmitOpcode(LineOfLastToken, OP_POP);
            }
            EndScope();
        }

        /// <summary>
        /// Follows 'if' token.
        /// </summary>
        private void IfStatement() {
            Tokens.Consume(LEFT_PAREN, "Expect '(' after 'if'.");
            Expression();
            Tokens.Consume(RIGHT_PAREN, "Expect ')' after if condition.");
            int thenJump = EmitJump(OP_JUMP_IF_FALSE);
            EmitOpcode(LineOfLastToken, OP_POP);
            Statement(); // then statement
            int elseJump = EmitJump(OP_JUMP);
            PatchJump(thenJump);
            EmitOpcode(LineOfLastToken, OP_POP);
            if (Tokens.Match(ELSE)) {
                Statement();
            }
            PatchJump(elseJump);
        }

        /// <summary>
        /// returnStmt → "return" expression? ";" ;
        /// </summary>
        private void ReturnStatement() {
            if (_FunctionType == ELoxFunctionType.TYPE_SCRIPT) {
                throw new CompilerException(Tokens.Previous(), "Can't return from top-level code.");
            }
            if (Tokens.Match(SEMICOLON)) {
                EmitReturn();
            }
            else {
                if (_FunctionType == ELoxFunctionType.TYPE_INITIALIZER) {
                    throw new CompilerException(Tokens.Previous(), "Can't return a value from a class initializer.");
                }
                Expression();
                Tokens.Consume(SEMICOLON, "Expect ';' after return value.");
                EmitOpcode(LineOfLastToken, OP_RETURN);
            }
        }

        private void WhileStatement() {
            int loopStart = _Chunk.SizeCode; // code point just before the condition
            Tokens.Consume(LEFT_PAREN, "Expect '(' after 'while'.");
            Expression();
            Tokens.Consume(RIGHT_PAREN, "Expect ')' after condition.");
            int exitJump = EmitJump(OP_JUMP_IF_FALSE);
            EmitOpcode(LineOfLastToken, OP_POP);
            Statement();
            EmitLoop(loopStart, LineOfLastToken);
            PatchJump(exitJump);
            EmitOpcode(LineOfLastToken, OP_POP);
        }

        private void Block() {
            while (!Tokens.Check(RIGHT_BRACE) && !Tokens.IsAtEnd()) {
                Declaration();
            }
            Tokens.Consume(RIGHT_BRACE, "Expect '}' after block.");
        }

        /// <summary>
        /// exprStmt  → expression ";" ;
        /// </summary>
        private void ExpressionStatement() {
            // semantically, an expression statement evaluates the expression and discards the result.
            Expression();
            Tokens.Consume(SEMICOLON, "Expect ';' after expression.");
            EmitOpcode(LineOfLastToken, OP_POP);
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
            while (Tokens.Match(OR)) {
                _CanAssign = false;
                int elseJump = EmitJump(OP_JUMP_IF_FALSE);
                int endJump = EmitJump(OP_JUMP);
                PatchJump(elseJump);
                EmitOpcode(LineOfLastToken, OP_POP);
                And();
                PatchJump(endJump);
            }
        }

        /// <summary>
        /// logic_and   → equality ( "and" equality )* ;
        /// </summary>
        private void And() {
            Bitwise();
            while (Tokens.Match(AND)) {
                _CanAssign = false;
                int endJump = EmitJump(OP_JUMP_IF_FALSE);
                EmitOpcode(LineOfLastToken, OP_POP);
                Bitwise();
                PatchJump(endJump);
            }
        }

        private void Bitwise() {
            Equality();
            while (Tokens.Match(AMPERSAND, PIPE)) {
                _CanAssign = false;
                Token op = Tokens.Previous();
                Equality();
                switch (op.Type) {
                    case AMPERSAND: EmitOpcode(LineOfLastToken, OP_BITWISE_AND); break;
                    case PIPE: EmitOpcode(LineOfLastToken, OP_BITWISE_OR); break;
                }
            }
        }

        /// <summary>
        /// equality    → comparison ( ( "!=" | "==" ) comparison )* ;
        /// </summary>
        private void Equality() {
            Comparison();
            while (Tokens.Match(BANG_EQUAL, EQUAL_EQUAL)) {
                _CanAssign = false;
                Token op = Tokens.Previous();
                Comparison();
                switch (op.Type) {
                    case BANG_EQUAL:
                        EmitOpcode(LineOfLastToken, OP_EQUAL);
                        EmitOpcode(LineOfLastToken, OP_NOT);
                        break;
                    case EQUAL_EQUAL:
                        EmitOpcode(LineOfLastToken, OP_EQUAL);
                        break;
                }
            }
        }

        /// <summary>
        /// comparison → addition ( ( ">" | ">=" | "&lt;" | "&lt;=" ) addition )* ;
        /// </summary>
        private void Comparison() {
            Addition();
            while (Tokens.Match(GREATER, GREATER_EQUAL, LESS, LESS_EQUAL)) {
                _CanAssign = false;
                Token op = Tokens.Previous();
                Addition();
                switch (op.Type) {
                    case GREATER:
                        EmitOpcode(LineOfLastToken, OP_GREATER);
                        break;
                    case GREATER_EQUAL:
                        EmitOpcode(LineOfLastToken, OP_LESS);
                        EmitOpcode(LineOfLastToken, OP_NOT);
                        break;
                    case LESS:
                        EmitOpcode(LineOfLastToken, OP_LESS);
                        break;
                    case LESS_EQUAL:
                        EmitOpcode(LineOfLastToken, OP_GREATER);
                        EmitOpcode(LineOfLastToken, OP_NOT);
                        break;
                }
            }
        }

        /// <summary>
        /// addition → multiplication ( ( "-" | "+" ) multiplication )* ;
        /// </summary>
        private void Addition() {
            Multiplication();
            while (Tokens.Match(MINUS, PLUS)) {
                _CanAssign = false;
                Token op = Tokens.Previous();
                Multiplication();
                switch (op.Type) {
                    case PLUS: EmitOpcode(LineOfLastToken, OP_ADD); break;
                    case MINUS: EmitOpcode(LineOfLastToken, OP_SUBTRACT); break;
                }
            }
        }

        /// <summary>
        /// multiplication → unary ( ( "/" | "*" ) unary )* ;
        /// </summary>
        private void Multiplication() {
            Unary();
            while (Tokens.Match(SLASH, STAR, PERCENT)) {
                _CanAssign = false;
                Token op = Tokens.Previous();
                Unary();
                switch (op.Type) {
                    case SLASH: EmitOpcode(LineOfLastToken, OP_DIVIDE); break;
                    case STAR: EmitOpcode(LineOfLastToken, OP_MULTIPLY); break;
                    case PERCENT: EmitOpcode(LineOfLastToken, OP_MODULUS); break;
                }
            }
        }

        /// <summary>
        /// unary → ( "!" | "-" | "~" ) unary | call ;
        /// </summary>
        private void Unary() {
            if (Tokens.Match(BANG, MINUS, TILDE)) {
                _CanAssign = false;
                Token op = Tokens.Previous();
                Unary();
                switch (op.Type) {
                    case MINUS: EmitOpcode(LineOfLastToken, OP_NEGATE); break;
                    case BANG: EmitOpcode(LineOfLastToken, OP_NOT); break;
                    case TILDE: EmitOpcode(LineOfLastToken, OP_BITWISE_COMPLEMENT); break;
                }
                return;
            }
            Call();
            if (Tokens.Match(INCREMENT, DECREMENT)) {
                switch (Tokens.Previous().Type) {
                    case INCREMENT: EmitOpcode(LineOfLastToken, OP_INCREMENT); break;
                    case DECREMENT: EmitOpcode(LineOfLastToken, OP_DECREMENT); break;
                }
            }
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
                if (Tokens.Match(LEFT_PAREN)) {
                    FinishCall(OP_CALL);
                }
                else if (Tokens.Match(DOT)) {
                    Token name = Tokens.Consume(IDENTIFIER, "Expect a property name after '.'.");
                    int nameConstant = MakeVarNameConstant(name.Lexeme); // no fixup needed
                    if (_CanAssign && Tokens.Match(EQUAL)) {
                        Expression();
                        EmitOpcode(LineOfLastToken, OP_SET_PROPERTY);
                        EmitConstantIndex(LineOfLastToken, nameConstant, null);
                    }
                    else if (Tokens.Match(LEFT_PAREN)) {
                        FinishCall(OP_INVOKE);
                        EmitConstantIndex(LineOfLastToken, nameConstant, null);
                    }
                    else {
                        EmitOpcode(LineOfLastToken, OP_GET_PROPERTY);
                        EmitConstantIndex(LineOfLastToken, nameConstant, null);
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
            if (!Tokens.Check(RIGHT_PAREN)) {
                do {
                    if (argumentCount >= MAX_PARAMS) {
                        throw new CompilerException(Tokens.Peek(), $"Cannot have more than {MAX_PARAMS} arguments.");
                    }
                    Expression();
                    argumentCount += 1;
                } while (Tokens.Match(COMMA));
            }
            Tokens.Consume(RIGHT_PAREN, "Expect ')' ending call operator parens (following any arguments).");
            EmitOpcode(LineOfLastToken, opCode);
            EmitData(LineOfLastToken, (byte)argumentCount);
        }

        /// <summary>
        /// primary → "false" | "true" | "nil" | "this"
        ///         | NUMBER | STRING | IDENTIFIER | "(" expression ")" 
        ///         | "super" "." IDENTIFIER ;
        /// </summary>
        private void Primary() {
            if (Tokens.Match(FALSE)) {
                EmitOpcode(LineOfLastToken, OP_FALSE);
                return;
            }
            if (Tokens.Match(TRUE)) {
                EmitOpcode(LineOfLastToken, OP_TRUE);
                return;
            }
            if (Tokens.Match(NIL)) {
                EmitOpcode(LineOfLastToken, OP_NIL);
                return;
            }
            if (Tokens.Match(NUMBER)) {
                EmitOpcode(LineOfLastToken, OP_LOAD_CONSTANT);
                EmitConstantIndex(LineOfLastToken, MakeValueConstant(Tokens.Previous().LiteralAsNumber), _FixupConstants);
                return;
            }
            if (Tokens.Match(STRING)) {
                EmitOpcode(LineOfLastToken, OP_LOAD_STRING);
                EmitConstantIndex(LineOfLastToken, MakeStringConstant(Tokens.Previous().LiteralAsString), null); // got rid of fixup 19-02-2025
                return;
            }
            if (Tokens.Match(SUPER)) {
                if (_CurrentClass == null) {
                    throw new CompilerException(Tokens.Previous(), "Can't use 'super' outside of a class.");
                }
                if (!_CurrentClass.HasSuperClass) {
                    throw new CompilerException(Tokens.Previous(), "Can't use 'super' in a class with no superclass.");
                }
                Token keyword = Tokens.Previous();
                Tokens.Consume(DOT, "Expect '.' after 'super'.");
                Token methodName = Tokens.Consume(IDENTIFIER, "Expect superclass method name.");
                int nameIndex = MakeVarNameConstant(methodName.Lexeme);
                NamedVariable(MakeSyntheticToken(THIS, "this", 0), false); // look up this - load instance onto stack
                if (Tokens.Match(LEFT_PAREN)) {
                    FinishCall(OP_SUPER_INVOKE);
                    NamedVariable(MakeSyntheticToken(SUPER, "super", 0), false); // look up this.super - load superclass of instance
                    EmitConstantIndex(LineOfLastToken, nameIndex, null); // got rid of fixup 19-02-2025
                }
                else {
                    NamedVariable(MakeSyntheticToken(SUPER, "super", 0), false); // look up this.super - load superclass of instance
                    EmitOpcode(LineOfLastToken, OP_GET_SUPER); // look up super.name - encode name of method to access as operand
                    EmitConstantIndex(LineOfLastToken, nameIndex, null); // got rid of fixup 19-02-2025
                }
                return;
            }
            if (Tokens.Match(THIS)) {
                if (_CurrentClass == null) {
                    throw new CompilerException(Tokens.Previous(), $"Cannot use 'this' outside of a class.");
                }
                // need to emit 'this' reference, but don't allow assignment:
                NamedVariable(Tokens.Previous(), false);
                return;
            }
            if (Tokens.Match(IDENTIFIER)) {
                NamedVariable(Tokens.Previous());
                return;
            }
            if (Tokens.Match(LEFT_PAREN)) {
                Expression();
                Tokens.Consume(RIGHT_PAREN, "Expect ')' after expression.");
                return;
            }
            if (_InSwitchStatement && (Tokens.Peek().Type == BREAK || Tokens.Peek().Type == CASE)) {
                return;
            }
            throw new CompilerException(Tokens.Peek(), "Expect expression.");
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
                index = MakeVarNameConstant(name.Lexeme); // no longer needs a fixup
                getOp = OP_GET_GLOBAL;
                setOp = OP_SET_GLOBAL;
                needsFixup = false;
            }
            if (Tokens.Match(EQUAL)) {
                if (!(_CanAssign & overrideCanAssign)) {
                    throw new CompilerException(name, $"Invalid assignment target '{name}'.");
                }
                Expression();
                EmitData(LineOfLastToken, (byte)setOp);
                EmitConstantIndex(LineOfLastToken, index, needsFixup ? _FixupConstants : null);
            }
            else {
                EmitData(LineOfLastToken, (byte)getOp);
                EmitConstantIndex(LineOfLastToken, index, needsFixup ? _FixupConstants : null);
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
            _UpvalueData[upvalueIndex] = new LoxCompilerUpvalue(index, isLocal);
            return upvalueIndex;
        }

        // === Emit Infrastructure ===================================================================================
        // ===========================================================================================================

        private void EmitOpcode(int line, EGearsOpCode opcode) {
            _Chunk.WriteCode(opcode, line);
        }

        private void EmitData(int line, params byte[] data) {
            foreach (byte i in data) {
                _Chunk.WriteCode(i, line);
            }
        }

        private void EmitLoop(int loopStart, int line) {
            EmitOpcode(line, OP_LOOP);
            int offset = _Chunk.SizeCode - loopStart + 2;
            if (offset > ushort.MaxValue) {
                throw new CompilerException(Tokens.Peek(), "Loop body too large.");
            }
            EmitData(LineOfLastToken, (byte)((offset >> 8) & 0xff), (byte)(offset & 0xff));
        }

        private int EmitJump(EGearsOpCode instruction) {
            EmitData(LineOfLastToken, (byte)instruction, 0xff, 0xff);
            return _Chunk.SizeCode - 2;
        }

        private void PatchJump(int offset) {
            // We adjust by two for the jump offset
            int jump = _Chunk.SizeCode - offset - 2;
            if (jump > ushort.MaxValue) {
                throw new CompilerException(Tokens.Peek(), "Too much code to jump over.");
            }
            _Chunk.WriteCodeAt(offset, (byte)((jump >> 8) & 0xff));
            _Chunk.WriteCodeAt(offset + 1, (byte)(jump & 0xff));
        }

        /// <summary>
        /// Lox implicitly returns nil when no return value is specified.
        /// </summary>
        private void EmitReturn() {
            if (_FunctionType == ELoxFunctionType.TYPE_INITIALIZER) {
                EmitOpcode(LineOfCurrentToken, OP_GET_LOCAL);
                EmitConstantIndex(LineOfCurrentToken, 0); // no fixup required
            }
            else {
                EmitOpcode(LineOfCurrentToken, OP_NIL);
            }
            EmitOpcode(LineOfCurrentToken, OP_RETURN);
        }

        // === Constants =============================================================================================
        // ===========================================================================================================

        private void EmitConstantIndex(int line, int index, List<LoxCompilerFixup> fixups = null) {
            if (fixups != null) {
                fixups.Add(new LoxCompilerFixup(_Chunk.SizeCode, index));
            }
            EmitData(line, (byte)((index >> 8) & 0xff), (byte)(index & 0xff));
        }

        /// <summary>
        /// Adds the given value to the chunk's constant table.
        /// Returns the index of that constant in the constant table.
        /// </summary>
        private int MakeValueConstant(GearsValue value) {
            for (int i = 0; i < _Chunk.SizeConstant; i++) {
                if ((int)_Chunk.ReadConstantValue(i) == (int)value) {
                    return i;
                }
            }
            int index = _Chunk.WriteConstantValue(value);
            if (index > short.MaxValue) {
                throw new CompilerException(Tokens.Peek(), "Too many constants in one chunk.");
            }
            return index;
        }

        /// <summary>
        /// Adds the given string to the chunk's string table.
        /// Returns the index of that string in the string table.
        /// </summary>
        private int MakeStringConstant(string value) {
            int index = _Chunk.Strings.WriteStringConstant(value);
            if (index > short.MaxValue) {
                throw new CompilerException(Tokens.Previous(), "Too many constants in one chunk.");
            }
            return index;
        }

        private int MakeVarNameConstant(string value) {
            int index = _Chunk.VarNameStrings.WriteStringConstant(value);
            if (index > short.MaxValue) {
                throw new CompilerException(Tokens.Previous(), "Too many late-resolved variable names in one chunk.");
            }
            return index;
        }

        private Token MakeSyntheticToken(int type, string name, int line) {
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
                    EmitOpcode(LineOfLastToken, OP_CLOSE_UPVALUE);
                }
                else {
                    EmitOpcode(LineOfLastToken, OP_POP);
                }
                _LocalCount -= 1;
            }
        }

        private void AddLocal(Token name) {
            if (_LocalCount >= MAX_LOCALS) {
                throw new CompilerException(name, "Too many local variables in scope.");
            }
            _LocalVarData[_LocalCount++] = new LoxCompilerLocal(name.Lexeme, SCOPE_NONE);
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
    }
}
