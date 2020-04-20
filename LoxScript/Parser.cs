using LoxScript.Grammar;
using System;
using System.Collections.Generic;
using static LoxScript.Grammar.TokenType;

namespace LoxScript {
    /// <summary>
    /// TODO: Add comma operator, ternary operator, error production on each binary operator without a left-hand operator.
    /// </summary>
    class Parser {
        private readonly List<Token> _Tokens;

        /// <summary>
        /// Points to the next token eagerly waiting to be used.
        /// </summary>
        private int _Current = 0;

        internal Parser(List<Token> tokens) {
            _Tokens = tokens;
        }

        internal List<Stmt> Parse() {
            return Program();
        }

        /// <summary>
        /// program     → declaration* EOF ;
        /// </summary>
        private List<Stmt> Program() {
            List<Stmt> statements = new List<Stmt>();
            while (!IsAtEnd()) {
                try {
                    statements.Add(Declaration());
                }
                catch (ParseException e) {
                    e.Print();
                }
            }
            return statements;
        }

        // === Declarations and Statements ===========================================================================
        // ===========================================================================================================

        /// <summary>
        /// declaration → "class" class
        ///             | "func" function
        ///             | varDecl
        ///             | statement ;
        /// </summary>
        private Stmt Declaration() {
            try {
                if (Match(CLASS)) {
                    return ClassDeclaration();
                }
                if (Match(FUNCTION)) {
                    return Function("function");
                }
                if (Match(VAR)) {
                    return VarDeclaration();
                }
                return Statement();
            }
            catch (ParseException e) {
                Synchronize();
                throw e;
            }
        }

        /// <summary>
        /// classDecl   → "class" IDENTIFIER ( "&lt;" IDENTIFIER )? 
        ///               "{" function* "}" ;
        /// </summary>
        /// <returns></returns>
        private Stmt ClassDeclaration() {
            Token name = Consume(IDENTIFIER, "Expect class name.");
            Expr.Variable superClass = null;
            if (Match(LESS)) {
                Consume(IDENTIFIER, "Expect superclass name.");
                superClass = new Expr.Variable(Previous());
            }
            Consume(LEFT_BRACE, "Expect '{' before class body.");
            List<Stmt.Function> methods = new List<Stmt.Function>();
            while (!Check(RIGHT_BRACE) && !IsAtEnd()) {
                methods.Add(Function("method"));
            }
            Consume(RIGHT_BRACE, "Expect '}' after class body.");
            return new Stmt.Class(name, superClass, methods);
        }

        /// <summary>
        /// function    → IDENTIFIER "(" parameters? ")" block ;
        /// parameters  → IDENTIFIER( "," IDENTIFIER )* ;
        /// </summary>
        private Stmt.Function Function(string kind) {
            Token name = Consume(IDENTIFIER, $"Expect {kind} name.");
            Consume(LEFT_PAREN, $"Expect '(' after {kind} name.");
            // parameter list:
            List<Token> parameters = new List<Token>();
            if (!Check(RIGHT_PAREN)) {
                do {
                    if (parameters.Count >= 255) {
                        LoxScript.Program.Error(Peek(), "Cannot have more than 255 parameters.");
                    }

                    parameters.Add(Consume(IDENTIFIER, "Expect parameter name."));
                } while (Match(COMMA));
            }
            Consume(RIGHT_PAREN, "Expect ')' after parameters.");
            // body:
            Consume(LEFT_BRACE, "Expect '{' before " + kind + " body.");
            List<Stmt> body = Block();
            return new Stmt.Function(name, parameters, body);
        }

        private Stmt VarDeclaration() {
            Token name = Consume(IDENTIFIER, "Expect variable name.");
            Expr initializer = null;
            if (Match(EQUAL)) {
                initializer = Expression();
            }
            Consume(SEMICOLON, "Expect ';' after variable declaration.");
            return new Stmt.Var(name, initializer);
        }

        private Stmt WhileStatement() {
            Consume(LEFT_PAREN, "Expect '(' after 'while'.");
            Expr condition = Expression();
            Consume(RIGHT_PAREN, "Expect ')' after condition.");
            Stmt body = Statement();
            return new Stmt.While(condition, body);
        }

        /// <summary>
        /// statement   → exprStmt
        ///             | forStmt
        ///             | ifStmt
        ///             | printStmt
        ///             | returnStmt
        ///             | whileStmt
        ///             | block ;
        /// </summary>
        private Stmt Statement() {
            if (Match(FOR)) {
                return ForStatement();
            }
            if (Match(IF)) {
                return IfStatement();
            }
            if (Match(PRINT)) {
                return PrintStatement();
            }
            if (Match(RETURN)) {
                return ReturnStatement();
            }
            if (Match(WHILE)) {
                return WhileStatement();
            }
            if (Match(LEFT_BRACE)) {
                return new Stmt.Block(Block());
            }
            // If we didn’t match any other type of statement, we must have an expression statement.
            return ExpressionStatement();
        }

        /// <summary>
        /// forStmt   → "for" "(" ( varDecl | exprStmt | ";" )
        ///                         expression? ";"
        ///                         expression? ")" statement ;
        /// </summary>
        private Stmt ForStatement() {
            Consume(LEFT_PAREN, "Expect '(' after 'for'.");
            // initializer:
            Stmt initializer;
            if (Match(SEMICOLON)) {
                initializer = null;
            }
            else if (Match(VAR)) {
                initializer = VarDeclaration();
            }
            else {
                initializer = ExpressionStatement();
            }
            // condition:
            Expr condition = null;
            if (!Check(SEMICOLON)) {
                condition = Expression();
            }
            Consume(SEMICOLON, "Expect ';' after loop condition.");
            // increment:
            Expr increment = null;
            if (!Check(RIGHT_PAREN)) {
                increment = Expression();
            }
            Consume(RIGHT_PAREN, "Expect ')' after for clauses.");
            // body:
            Stmt body = Statement();
            // add increment, which must execute following the body in every iterlation of the loop:
            if (increment != null) { 
                body = new Stmt.Block(new List<Stmt>() { body, new Stmt.Expres(increment) });
            }
            // add condition that will be checked for every iteration of the loop. If no condition, force to true:
            if (condition == null) {
                condition = new Expr.Literal(true);
            }
            body = new Stmt.While(condition, body);
            // add initializer, which runs once before the entire loop:
            if (initializer != null) {
                body = new Stmt.Block(new List<Stmt>() { initializer, body });
            }
            return body;
        }

        /// <summary>
        /// Follows 'if' token.
        /// </summary>
        private Stmt IfStatement() {
            Consume(LEFT_PAREN, "Expect '(' after 'if'.");
            Expr condition = Expression();
            Consume(RIGHT_PAREN, "Expect ')' after if condition.");
            Stmt thenBranch = Statement();
            Stmt elseBranch = null;
            if (Match(ELSE)) {
                elseBranch = Statement();
            }
            return new Stmt.If(condition, thenBranch, elseBranch);
        }

        /// <summary>
        /// printStmt → "print" expression ";" ;
        /// </summary>
        private Stmt PrintStatement() {
            Expr value = Expression();
            Consume(SEMICOLON, "Expect ';' after value.");
            return new Stmt.Print(value);
        }

        /// <summary>
        /// returnStmt → "return" expression? ";" ;
        /// </summary>
        private Stmt ReturnStatement() {
            Token keyword = Previous();
            Expr value = null;
            if (!Check(SEMICOLON)) {
                value = Expression();
            }
            Consume(SEMICOLON, "Expect ';' after return value.");
            return new Stmt.Return(keyword, value);
        }

        private List<Stmt> Block() {
            List<Stmt> statements = new List<Stmt>();
            while (!Check(RIGHT_BRACE) && !IsAtEnd()) {
                statements.Add(Declaration());
            }
            Consume(RIGHT_BRACE, "Expect '}' after block.");
            return statements;
        }

        /// <summary>
        /// exprStmt  → expression ";" ;
        /// </summary>
        private Stmt ExpressionStatement() {
            Expr expr = Expression();
            Consume(SEMICOLON, "Expect ';' after expression.");
            return new Stmt.Expres(expr);
        }

        // === Expressions ===========================================================================================
        // ===========================================================================================================

        /// <summary>
        /// expression  → assignment ;
        /// </summary>
        private Expr Expression() {
            return Assignment();
        }

        /// <summary>
        /// assignment  → ( call "." )? IDENTIFIER "=" assignment
        ///             | logic_or ;
        /// </summary>
        private Expr Assignment() {
            Expr expr = Or();
            if (Match(EQUAL)) {
                Token equals = Previous();
                Expr value = Assignment();
                // Make sure the left-hand expression is a valid assignment target. If not, fail with a syntax error.
                if (expr is Expr.Variable varExpr) {
                    Token name = varExpr.Name;
                    return new Expr.Assign(name, value);
                }
                else if (expr is Expr.Get getExpr) {
                    return new Expr.Set(getExpr.Obj, getExpr.Name, value);
                }
                LoxScript.Program.Error(equals, "Invalid assignment target.");
            }
            return expr;
        }

        /// <summary>
        /// logic_or    → logic_and ( "or" logic_and)* ;
        /// </summary>
        /// <returns></returns>
        private Expr Or() {
            Expr expr = And();
            while (Match(OR)) {
                Token op = Previous();
                Expr right = And();
                expr = new Expr.Logical(expr, op, right);
            }
            return expr;
        }

        /// <summary>
        /// logic_and   → equality ( "and" equality )* ;
        /// </summary>
        /// <returns></returns>
        private Expr And() {
            Expr expr = Equality();
            while (Match(AND)) {
                Token op = Previous();
                Expr right = Equality();
                expr = new Expr.Logical(expr, op, right);
            }
            return expr;
        }

        /// <summary>
        /// equality    → comparison ( ( "!=" | "==" ) comparison )* ;
        /// </summary>
        private Expr Equality() {
            Expr expr = Comparison();
            while (Match(BANG_EQUAL, EQUAL_EQUAL)) {
                Token op = Previous();
                Expr right = Comparison();
                expr = new Expr.Binary(expr, op, right);
            }

            return expr;
        }

        /// <summary>
        /// comparison → addition ( ( ">" | ">=" | "&lt;" | "&lt;=" ) addition )* ;
        /// </summary>
        private Expr Comparison() {
            Expr expr = Addition();
            while (Match(GREATER, GREATER_EQUAL, LESS, LESS_EQUAL)) {
                Token op = Previous();
                Expr right = Addition();
                expr = new Expr.Binary(expr, op, right);
            }
            return expr;
        }

        /// <summary>
        /// addition → multiplication ( ( "-" | "+" ) multiplication )* ;
        /// </summary>
        private Expr Addition() {
            Expr expr = Multiplication();

            while (Match(MINUS, PLUS)) {
                Token op = Previous();
                Expr right = Multiplication();
                expr = new Expr.Binary(expr, op, right);
            }

            return expr;
        }

        /// <summary>
        /// multiplication → unary ( ( "/" | "*" ) unary )* ;
        /// </summary>
        private Expr Multiplication() {
            Expr expr = Unary();

            while (Match(SLASH, STAR)) {
                Token op = Previous();
                Expr right = Unary();
                expr = new Expr.Binary(expr, op, right);
            }

            return expr;
        }

        /// <summary>
        /// unary → ( "!" | "-" ) unary | call ;
        /// </summary>
        private Expr Unary() {
            if (Match(BANG, MINUS)) {
                Token op = Previous();
                Expr right = Unary();
                return new Expr.Unary(op, right);
            }

            return Call();
        }

        /// <summary>
        /// call → primary ( "(" arguments? ")" | "." IDENTIFIER )* ;
        /// 
        /// Matches a primary expression followed by zero or more function calls. If there are no parentheses, this
        /// parses a bare primary expression. Otherwise, each call is recognized by a pair of parentheses with an
        /// optional list of arguments inside.
        /// </summary>
        private Expr Call() {
            Expr expr = Primary();
            while (true) {
                if (Match(LEFT_PAREN)) {
                    expr = FinishCall(expr);
                }
                else if (Match(DOT)) {
                    Token name = Consume(IDENTIFIER, "Expect a property name after '.'.");
                    expr = new Expr.Get(expr, name);
                }
                else {
                    break;
                }
            }
            return expr;
        }

        /// <summary>
        /// arguments → expression ( "," expression )* ;
        /// 
        /// Requires one or more argument expressions, followed by zero or more expressions each preceded by a comma.
        /// To handle zero-argument calls, the call rule itself considers the entire arguments production optional.
        /// </summary>
        private Expr FinishCall(Expr callee) {
            List<Expr> arguments = new List<Expr>();
            if (!Check(RIGHT_PAREN)) {
                do {
                    if (arguments.Count >= 255) {
                        LoxScript.Program.Error(Peek(), "Cannot have more than 255 arguments.");
                    }
                    arguments.Add(Expression());
                } while (Match(COMMA));
            }
            Token paren = Consume(RIGHT_PAREN, "Expect ')' ending call operator parens (following any arguments).");
            return new Expr.Call(callee, paren, arguments);
        }

        /// <summary>
        /// primary → "false" | "true" | "nil" | "this"
        ///         | NUMBER | STRING | IDENTIFIER | "(" expression ")" 
        ///         | "super" "." IDENTIFIER ;
        /// </summary>
        private Expr Primary() {
            if (Match(FALSE)) {
                return new Expr.Literal(false);
            }
            if (Match(TRUE)) {
                return new Expr.Literal(true);
            }
            if (Match(NIL)) {
                return new Expr.Literal(null);
            }
            if (Match(NUMBER, STRING)) {
                return new Expr.Literal(Previous().Literal);
            }
            if (Match(SUPER)) {
                Token keyword = Previous();
                Consume(DOT, "Expect '.' after 'super'.");
                Token method = Consume(IDENTIFIER, "Expect superclass method name.");
                return new Expr.Super(keyword, method);
            }
            if (Match(THIS)) {
                return new Expr.This(Previous());
            }
            if (Match(IDENTIFIER)) {
                return new Expr.Variable(Previous());
            }
            if (Match(LEFT_PAREN)) {
                Expr expr = Expression();
                Consume(RIGHT_PAREN, "Expect ')' after expression.");
                return new Expr.Grouping(expr);
            }
            throw new ParseException(Peek(), "Expect expression.");
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
            throw new ParseException(Peek(), message);
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
            if (IsAtEnd()) return false;
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
        private class ParseException : Exception {
            private readonly Token _Token;
            private readonly string _Message;

            internal ParseException(Token token, string message) {
                _Token = token;
                _Message = message;
            }

            internal void Print() {
                LoxScript.Program.Error(_Token, _Message);
            }
        }
    }
}
