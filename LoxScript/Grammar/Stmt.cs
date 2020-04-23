using LoxScript.Parsing;
using System.Collections.Generic;

namespace LoxScript.Grammar {
    abstract class Stmt {

        internal interface IVisitor {
            void VisitBlockStmt(Block stmt);
            void VisitClassStmt(Class stmt);
            void VisitExpresStmt(Expres stmt);
            void VisitFunctionStmt(Function stmt);
            void VisitIfStmt(If stmt);
            void VisitPrintStmt(Print stmt);
            void VisitReturnStmt(Return stmt);
            void VisitVarStmt(Var stmt);
            void VisitWhileStmt(While stmt);
        }

        internal abstract void Accept(IVisitor visitor);

        internal class Block : Stmt {
            internal List<Stmt> Statements;

            internal Block(List<Stmt> statements) {
                Statements = statements;
            }

            internal override void Accept(IVisitor visitor) {
                visitor.VisitBlockStmt(this);
            }
        }

        internal class Class : Stmt {
            internal Token Name;
            internal Expr.Variable SuperClass;
            internal List<Function> Methods;

            internal Class(Token name, Expr.Variable superClass, List<Function> methods) {
                Name = name;
                SuperClass = superClass;
                Methods = methods;
            }

            internal override void Accept(IVisitor visitor) {
                visitor.VisitClassStmt(this);
            }
        }

        internal class Expres : Stmt {
            internal Expr Expression;

            internal Expres(Expr expression) {
                Expression = expression;
            }

            internal override void Accept(IVisitor visitor) {
                visitor.VisitExpresStmt(this);
            }
        }

        internal class Function : Stmt {
            internal Token Name;
            internal List<Token> Parameters;
            internal List<Stmt> Body;

            internal Function(Token name, List<Token> parameters, List<Stmt> body) {
                Name = name;
                Parameters = parameters;
                Body = body;
            }

            internal override void Accept(IVisitor visitor) {
                visitor.VisitFunctionStmt(this);
            }
        }

        internal class If : Stmt {
            internal Expr Condition;
            internal Stmt ThenBranch;
            internal Stmt ElseBranch;

            internal If(Expr condition, Stmt thenBranch, Stmt elseBranch) {
                Condition = condition;
                ThenBranch = thenBranch;
                ElseBranch = elseBranch;
            }

            internal override void Accept(IVisitor visitor) {
                visitor.VisitIfStmt(this);
            }
        }

        internal class Print : Stmt {
            internal Expr Expression;

            internal Print(Expr expression) {
                Expression = expression;
            }

            internal override void Accept(IVisitor visitor) {
                visitor.VisitPrintStmt(this);
            }
        }

        internal class Return : Stmt {
            internal Token Keyword;
            internal Expr Value;

            internal Return(Token keyword, Expr value) {
                Keyword = keyword;
                Value = value;
            }

            internal override void Accept(IVisitor visitor) {
                visitor.VisitReturnStmt(this);
            }
        }

        internal class Var : Stmt {
            internal Token Name;
            internal Expr Initializer;

            internal Var(Token name, Expr initializer) {
                Name = name;
                Initializer = initializer;
            }

            internal override void Accept(IVisitor visitor) {
                visitor.VisitVarStmt(this);
            }
        }

        internal class While : Stmt {
            internal Expr Condition;
            internal Stmt Body;

            internal While(Expr condition, Stmt body) {
                Condition = condition;
                Body = body;
            }

            internal override void Accept(IVisitor visitor) {
                visitor.VisitWhileStmt(this);
            }
        }

    }
}
