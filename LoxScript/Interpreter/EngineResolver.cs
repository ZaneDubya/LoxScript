using System;
using System.Collections.Generic;
using System.Linq;
using LoxScript.Grammar;

namespace LoxScript.Interpreter {
    class EngineResolver : Expr.IVisitor<object>, Stmt.IVisitor {
        private readonly Engine _Interpreter;
        private readonly Stack<Dictionary<string, bool>> _Scopes;
        private FunctionType _CurrentFunctionType = FunctionType.None;
        private ClassType _CurrentClassType = ClassType.None;

        internal EngineResolver(Engine interpreter) {
            _Interpreter = interpreter;
            _Scopes = new Stack<Dictionary<string, bool>>();
        }

        internal void Resolve(List<Stmt> statements) {
            foreach (Stmt statement in statements) {
                Resolve(statement);
            }
        }

        // === Scoping and Resolution ================================================================================
        // ===========================================================================================================

        private void BeginScope() {
            _Scopes.Push(new Dictionary<string, bool>());
        }

        private void EndScope() {
            _Scopes.Pop();
        }

        private void Resolve(Stmt statement) {
            statement.Accept(this);
        }

        private void Resolve(Expr expression) {
            expression.Accept(this);
        }

        private void Declare(Token name) {
            if (_Scopes.Count == 0) {
                return;
            }
            Dictionary<string, bool> scope = _Scopes.Peek();
            // detect re-assigning an existing local variable in the same scope:
            if (scope.ContainsKey(name.Lexeme)) {
                Program.Error(name, "Variable with this name already declared in this scope.");
            }
            // declare the variable!
            _Scopes.Peek()[name.Lexeme] = false; // declared but not initialized, unavailable
        }

        private void Define(Token name) {
            if (_Scopes.Count == 0) {
                return;
            }
            _Scopes.Peek()[name.Lexeme] = true; // declared and initialized, available for use
        }

        /// <summary>
        /// Look for a matching name in the containing scopes, starting at the innermost scope and working outwards.
        /// If the variable is found, tell the interpreter it has been resolved, passing in the number of scopes
        /// between the current innermost scope and the scope where the variable was found. 
        /// Example: if the variable was found in the current scope, pass 0. If in the next enclosing scope, 1. 
        /// </summary>
        private void ResolveLocal(Expr expr, Token name) {
            for (int i = 0; i < _Scopes.Count; i++) {
                if (_Scopes.ElementAt(i).ContainsKey(name.Lexeme)) {
                    _Interpreter.Resolve(expr, i);
                    return;
                }
            }
            // Not found. Assume it is global.  
        }

        private void ResolveFunction(Stmt.Function function, FunctionType fnType) {
            FunctionType enclosingFunctionType = _CurrentFunctionType;
            _CurrentFunctionType = fnType;
            BeginScope();
            foreach (Token param in function.Parameters) {
                Declare(param);
                Define(param);
            }
            // in static analysis demanded by resolution, we immediately analyze the body.
            // at runtime, we only traverse the body when the function is called.
            Resolve(function.Body); 
            EndScope();
            _CurrentFunctionType = enclosingFunctionType;
        }

        // === Statements ===========================================================================================
        // ===========================================================================================================

        public void VisitBlockStmt(Stmt.Block stmt) {
            BeginScope();
            Resolve(stmt.Statements);
            EndScope();
        }

        public void VisitClassStmt(Stmt.Class stmt) {
            ClassType enclosingClassType = _CurrentClassType;
            _CurrentClassType = ClassType.Class;
            Declare(stmt.Name);
            Define(stmt.Name);
            if (stmt.SuperClass != null) {
                _CurrentClassType = ClassType.SubClass; // this class is a subclass
                if (stmt.Name.Lexeme.Equals(stmt.SuperClass.Name.Lexeme)) {
                    Program.Error(stmt.SuperClass.Name, "A class cannot inherit from itself.");
                }
                Resolve(stmt.SuperClass);
            }
            if (stmt.SuperClass != null) {
                BeginScope();
                _Scopes.Peek()["super"] = true;
            }
            BeginScope();
            _Scopes.Peek()[Keywords.This] = true; // declare 'this' in the local scope.
            foreach (Stmt.Function method in stmt.Methods) {
                FunctionType declaration = FunctionType.Method;
                if (method.Name.Lexeme.Equals(Keywords.Ctor)) {
                    declaration = FunctionType.Ctor;
                }
                ResolveFunction(method, declaration);
            }
            EndScope();
            if (stmt.SuperClass != null) {
                EndScope();
            }
            _CurrentClassType = enclosingClassType;
        }

        public void VisitExpresStmt(Stmt.Expres stmt) {
            Resolve(stmt.Expression);
        }

        public void VisitFunctionStmt(Stmt.Function stmt) {
            // functions both introduce a scope, and bind the function's name in the enclosing scope.
            Declare(stmt.Name);
            Define(stmt.Name);
            ResolveFunction(stmt, FunctionType.Function);
        }

        public void VisitIfStmt(Stmt.If stmt) {
            Resolve(stmt.Condition);
            Resolve(stmt.ThenBranch);
            if (stmt.ElseBranch != null) {
                Resolve(stmt.ElseBranch);
            }
        }

        public void VisitPrintStmt(Stmt.Print stmt) {
            Resolve(stmt.Expression);
        }

        public void VisitReturnStmt(Stmt.Return stmt) {
            if (_CurrentFunctionType == FunctionType.None) {
                Program.Error(stmt.Keyword, "Cannot return from top-level code.");
            }
            if (stmt.Value != null) {
                if (_CurrentFunctionType == FunctionType.Ctor) {
                    Program.Error(stmt.Keyword, "Cannot return a value from an initializer.");
                }
                Resolve(stmt.Value);
            }
        }

        public void VisitVarStmt(Stmt.Var stmt) {
            Declare(stmt.Name);
            if (stmt.Initializer != null) {
                Resolve(stmt.Initializer);
            }
            Define(stmt.Name);
        }

        public void VisitWhileStmt(Stmt.While stmt) {
            Resolve(stmt.Condition);
            Resolve(stmt.Body);
        }

        // === Expressions ===========================================================================================
        // ===========================================================================================================

        public object VisitAssignExpr(Expr.Assign expr) {
            // resolve the expression for the assigned value in case it also contains references to other variables.
            Resolve(expr.Value);
            // resolve the variable that’s being assigned to.
            ResolveLocal(expr, expr.Name);
            return null;
        }

        public object VisitBinaryExpr(Expr.Binary expr) {
            // traverse into and resolve both operands.
            Resolve(expr.Left);
            Resolve(expr.Right);
            return null;
        }

        public object VisitCallExpr(Expr.Call expr) {
            // resolve the thing being called (usually a variable expression).
            Resolve(expr.Callee);
            // walk the argument list and resolve them all.
            foreach (Expr argument in expr.Arguments) {
                Resolve(argument);
            }
            return null;
        }

        public object VisitGetExpr(Expr.Get expr) {
            Resolve(expr.Obj);
            return null;
        }

        public object VisitGroupingExpr(Expr.Grouping expr) {
            Resolve(expr.Expression);
            return null;
        }

        public object VisitLiteralExpr(Expr.Literal expr) {
            // Nothing to traverse, since a literal doesn't mention any variables or contain subexpressions.
            return null;
        }

        public object VisitLogicalExpr(Expr.Logical expr) {
            Resolve(expr.Left);
            Resolve(expr.Right);
            return null;
        }

        public object VisitSetExpr(Expr.Set expr) {
            Resolve(expr.Value);
            Resolve(expr.Obj);
            return null;
        }

        /// <summary>
        /// We resolve the super token exactly as if it were a variable. This stores the number of hops along the
        /// environment chain the interpreter needs to walk to find the environment where the superclass is stored.
        /// </summary>
        public object VisitSuperExpr(Expr.Super expr) {
            if (_CurrentClassType == ClassType.Class) {
                Program.Error(expr.Keyword, "Cannot use 'super' outside of a class.");
            }
            else if (_CurrentClassType != ClassType.SubClass) {
                Program.Error(expr.Keyword, "Cannot use 'super' in a class with no superclass.");
            }
            ResolveLocal(expr, expr.Keyword);
            return null;
        }

        public object VisitThisExpr(Expr.This expr) {
            if (_CurrentClassType == ClassType.None) {
                Program.Error(expr.Keyword, "Cannot use 'this' outside of a class.");
            }
            ResolveLocal(expr, expr.Keyword);
            return null;
        }

        public object VisitUnaryExpr(Expr.Unary expr) {
            Resolve(expr.Right);
            return null;
        }

        public object VisitVariableExpr(Expr.Variable expr) {
            if (_Scopes.Count > 0 && _Scopes.Peek().TryGetValue(expr.Name.Lexeme, out bool isInitialized) && isInitialized == false) {
                Program.Error(expr.Name, "Cannot read local variable in its own initializer.");
            }
            ResolveLocal(expr, expr.Name);
            return null;
        }

        // === Helpers ==============================================================================================
        // ===========================================================================================================

        private enum FunctionType {
            None,
            Function,
            Ctor,
            Method
        }

        private enum ClassType {
            None,
            Class,
            SubClass
        }
    }
}
