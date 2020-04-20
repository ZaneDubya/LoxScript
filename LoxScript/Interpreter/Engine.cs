using LoxScript.Grammar;
using System;
using System.Collections.Generic;

namespace LoxScript.Interpreter {
    class Engine : Expr.IVisitor<object>, Stmt.IVisitor {
        private readonly EngineEnvironment _Globals = new EngineEnvironment();
        private readonly Dictionary<Expr, int> _Locals = new Dictionary<Expr, int>();
        private EngineEnvironment _Environment;

        internal Engine() {
            _Globals.Define("clock", new LoxCallableClock());
            _Environment = _Globals;
        }

        public void Interpret(List<Stmt> statements) {
            try {
                foreach (Stmt statement in statements) {
                    Execute(statement);
                }
            }
            catch (RuntimeException error) {
                Program.RuntimeError(error);
            }
        }

        private void Execute(Stmt statement) {
            statement.Accept(this);
        }

        /// <summary>
        /// Store the resolved variable, associating it with each syntax tree node.
        /// Note that each expression is its own object, with its own unique identity.
        /// </summary>
        internal void Resolve(Expr expr, int depth) {
            _Locals[expr] = depth;
        }

        private object LookUpVariable(Token name, Expr expr) {
            // If we have resolved distance for this object, it must be local.
            // Retrieve it from the appropriate scope.
            if (_Locals.TryGetValue(expr, out int distance)) {
                return _Environment.GetAt(distance, name.Lexeme);
            }
            // If we don’t find the distance in the map, it must be global.
            return _Globals.Get(name);
        }

        // === Statement Visitor Handling ============================================================================
        // ===========================================================================================================

        public void VisitBlockStmt(Stmt.Block stmt) {
            ExecuteBlock(stmt.Statements, new EngineEnvironment(_Environment));
        }

        /// <summary>
        /// Executing a class declaration turns the syntactic representation of a class (its AST node) into its runtime representation, a EngineClass object.
        /// </summary>
        public void VisitClassStmt(Stmt.Class stmt) {
            object superClass = null;
            if (stmt.SuperClass != null) {
                superClass = Evaluate(stmt.SuperClass);
                if (!(superClass is EngineClassDeclaration)) {
                    throw new RuntimeException(stmt.SuperClass.Name, "Superclass must be a class.");
                }
            }
            _Environment.Define(stmt.Name.Lexeme, null);
            // subclasses get a reference to the superclass:
            if (stmt.SuperClass != null) {
                _Environment = new EngineEnvironment(_Environment);
                _Environment.Define("super", superClass);
            }
            Dictionary<string, EngineFunction> methods = new Dictionary<string, EngineFunction>();
            foreach (Stmt.Function method in stmt.Methods) {
                methods[method.Name.Lexeme] = new EngineFunction(method, _Environment, method.Name.Lexeme.Equals(Keywords.Ctor));
            }
            EngineClassDeclaration classDeclaration = new EngineClassDeclaration(stmt.Name.Lexeme, (EngineClassDeclaration)superClass, methods);
            if (superClass != null) {
                _Environment = _Environment.Enclosing;
            }
            _Environment.Assign(stmt.Name, classDeclaration);
        }

        public void VisitExpresStmt(Stmt.Expres stmt) {
            Evaluate(stmt.Expression);
        }

        public void VisitFunctionStmt(Stmt.Function stmt) {
            EngineFunction function = new EngineFunction(stmt, _Environment, false);
            _Environment.Define(stmt.Name.Lexeme, function);
        }

        public void VisitIfStmt(Stmt.If stmt) {
            if (IsTruthy(Evaluate(stmt.Condition))) {
                Execute(stmt.ThenBranch);
            }
            else if (stmt.ElseBranch != null) {
                Execute(stmt.ElseBranch);
            }
        }

        public void VisitPrintStmt(Stmt.Print stmt) {
            object value = Evaluate(stmt.Expression);
            Console.WriteLine(Stringify(value));
        }

        public void VisitReturnStmt(Stmt.Return stmt) {
            object value = null;
            if (stmt.Value != null) {
                value = Evaluate(stmt.Value);
            }
            throw new ReturnException(value);
        }

        public void VisitVarStmt(Stmt.Var stmt) {
            object value = null;
            // If the variable has an initializer, we evaluate it.
            if (stmt.Initializer != null) {
                value = Evaluate(stmt.Initializer);
            }
            _Environment.Define(stmt.Name.Lexeme, value);
        }

        public void VisitWhileStmt(Stmt.While stmt) {
            while (IsTruthy(Evaluate(stmt.Condition))) {
                Execute(stmt.Body);
            }
        }

        // === Expression Visitor Handling ===========================================================================
        // ===========================================================================================================

        public object VisitAssignExpr(Expr.Assign expr) {
            object value = Evaluate(expr.Value);
            // _Environment.Assign(expr.Name, value); - original code, replaced in Ch. 11 'Resolving and Binding'
            if (_Locals.TryGetValue(expr, out int distance)) {
                _Environment.AssignAt(distance, expr.Name, value);
            }
            else {
                _Globals.Assign(expr.Name, value);
            }
            return value;
        }

        public object VisitBinaryExpr(Expr.Binary expr) {
            object left = Evaluate(expr.Left);
            object right = Evaluate(expr.Right);

            switch (expr.Op.Type) {
                // comparison operators:
                case TokenType.GREATER:
                    CheckNumberOperands(expr.Op, left, right);
                    return (double)left > (double)right;
                case TokenType.GREATER_EQUAL:
                    CheckNumberOperands(expr.Op, left, right);
                    return (double)left >= (double)right;
                case TokenType.LESS:
                    CheckNumberOperands(expr.Op, left, right);
                    return (double)left < (double)right;
                case TokenType.LESS_EQUAL:
                    CheckNumberOperands(expr.Op, left, right);
                    return (double)left <= (double)right;
                case TokenType.MINUS:
                    CheckNumberOperands(expr.Op, left, right);
                    return (double)left - (double)right;
                // arithmetic operators:
                case TokenType.PLUS:
                    if ((left is double) && (right is double)) {
                        return (double)left + (double)right;
                    }
                    if ((left is string || left is double) && (right is string || right is double)) {
                        return Stringify(left) + Stringify(right);
                    }
                    throw new RuntimeException(expr.Op, "Operands must be two numbers or two strings.");
                case TokenType.SLASH:
                    CheckNumberOperands(expr.Op, left, right);
                    if ((double)right == 0) {
                        throw new RuntimeException(expr.Op, "Division by zero.");
                    }
                    return (double)left / (double)right;
                case TokenType.STAR:
                    CheckNumberOperands(expr.Op, left, right);
                    return (double)left * (double)right;
                // equality operators:
                case TokenType.BANG_EQUAL:
                    return !IsEqual(left, right);
                case TokenType.EQUAL_EQUAL:
                    return IsEqual(left, right);
            }
            // Unreachable.                                
            return null;
        }

        public object VisitCallExpr(Expr.Call expr) {
            object callee = Evaluate(expr.Callee);
            List<object> arguments = new List<object>();
            foreach (Expr argument in expr.Arguments) {
                arguments.Add(Evaluate(argument));
            }
            if (!(callee is IEngineCallable function)) {
                throw new RuntimeException(expr.Paren, "Can only call functions and classes.");
            }
            if (arguments.Count != function.Arity()) {
                throw new RuntimeException(expr.Paren, $"Expected {function.Arity()} arguments but passed {arguments.Count}.");
            }
            return function.Call(this, arguments);
        }

        public object VisitGetExpr(Expr.Get expr) {
            object obj = Evaluate(expr.Obj);
            if (obj is EngineClassInstance instance) {
                return instance.Get(expr.Name);
            }
            throw new RuntimeException(expr.Name, "Only instances have properties.");
        }

        public object VisitGroupingExpr(Expr.Grouping expr) {
            return Evaluate(expr.Expression);
        }

        /// <summary>
        /// Convert the literal tree node into a runtime value.
        /// </summary>
        public object VisitLiteralExpr(Expr.Literal expr) {
            return expr.Value;
        }

        public object VisitLogicalExpr(Expr.Logical expr) {
            object left = Evaluate(expr.Left);
            if (expr.Op.Type == TokenType.OR) {
                if (IsTruthy(left)) {
                    return left;
                }
            }
            else { // TokenType.AND
                if (!IsTruthy(left)) {
                    return left;
                }
            }
            return Evaluate(expr.Right);
        }

        public object VisitSetExpr(Expr.Set expr) {
            object obj = Evaluate(expr.Obj);
            if (!(obj is EngineClassInstance instance)) {
                throw new RuntimeException(expr.Name, "Only instances have fields.");
            }
            object value = Evaluate(expr.Value);
            instance.Set(expr.Name, value);
            return value;
        }

        public object VisitSuperExpr(Expr.Super expr) {
            int distance = _Locals[expr];
            EngineClassDeclaration superClass = _Environment.GetAt(distance, "super") as EngineClassDeclaration;
            // we also need to bind 'this'... which is always one level nearer than 'super''s environment ...
            EngineClassInstance obj = _Environment.GetAt(distance - 1, "this") as EngineClassInstance;
            if (superClass.TryFindMethod(expr.Method.Lexeme, out EngineFunction method)) {
                return method.Bind(obj);
            }
            throw new RuntimeException(expr.Method, $"Undefined property '{expr.Method.Lexeme}'.");
        }

        public object VisitThisExpr(Expr.This expr) {
            return LookUpVariable(expr.Keyword, expr);
        }

        /// <summary>
        /// Unary expressions have a single subexpression that we must evaluate first, then the unary expression itself does a little final work.
        /// </summary>
        public object VisitUnaryExpr(Expr.Unary expr) {
            object right = Evaluate(expr.Right);
            switch (expr.Op.Type) {
                case TokenType.BANG:
                    CheckNumberOperand(expr.Op, right);
                    return !IsTruthy(right);
                case TokenType.MINUS:
                    return -(double)right;
            }
            // Unreachable.                              
            return null;
        }

        public object VisitVariableExpr(Expr.Variable expr) {
            // return _Environment.Get(expr.Name); - This was the original code, which would allow contained scopes to
            // reference different varaibles depending on when the scope was referenced. This is 'mutable scope'.
            // We replaced this in chapter 11, 'Resolving and Binding', with:
            return LookUpVariable(expr.Name, expr);
        }

        // === Evaluation ============================================================================================
        // ===========================================================================================================

        private void CheckNumberOperand(Token op, object value) {
            if (value is double) {
                return;
            }
            throw new RuntimeException(op, "Operand must be a number.");
        }

        private void CheckNumberOperands(Token op, object left, object right) {
            if (left is double && right is double) {
                return;
            }
            throw new RuntimeException(op, "Operands must be numbers.");
        }

        internal void ExecuteBlock(List<Stmt> statements, EngineEnvironment environment) {
            EngineEnvironment enclosing = _Environment;
            try {
                _Environment = environment;
                foreach (Stmt statement in statements) {
                    Execute(statement);
                }
            }
            finally {
                _Environment = enclosing;
            }
        }

        private object Evaluate(Expr expr) {
            return expr.Accept(this);
        }

        /// <summary>
        /// False and nil are falsey and everything else is truthy.
        /// </summary>
        private bool IsTruthy(object value) {
            if (value == null) {
                return false;
            }
            if (value is bool) {
                return (bool)value;
            }
            return true;
        }

        private bool IsEqual(object a, object b) {
            // nil is only equal to nil.               
            if (a == null && b == null) {
                return true;
            }
            if (a == null) {
                return false;
            }
            return a.Equals(b);
        }

        // === Helpers ===============================================================================================
        // ===========================================================================================================

        private string Stringify(object value) {
            if (value == null) {
                return "nil";
            }
            // Hack. Work around Java adding ".0" to integer-valued doubles.
            if (value is double) {
                string text = value.ToString();
                if (text.EndsWith(".0")) {
                    text = text.Substring(0, text.Length - 2);
                }
                return text;
            }
            return value.ToString();
        }

        private class LoxCallableClock : IEngineCallable {
            public int Arity() => 0;

            public object Call(Engine interpreter, List<object> arguments) {
                return (double)DateTimeOffset.Now.ToUnixTimeMilliseconds();
            }

            public override string ToString() => "<fn clock (native)>";
        }

        // === Calling a function and returning from the same... =====================================================
        // ===========================================================================================================

        internal class ReturnException : Exception {
            internal readonly object Value;

            public ReturnException(object value) {
                Value = value;
            }
        }

        // === Error Reporting =======================================================================================
        // ===========================================================================================================

        internal class RuntimeException : Exception {
            internal readonly Token Token;

            public RuntimeException(Token token, string message)
                : base(message) {
                Token = token;
            }
        }
    }
}
