using LoxScript.Grammar;
using System.Collections.Generic;

namespace LoxScript {
    abstract class Expr {

        internal interface IVisitor<T> {
            T VisitAssignExpr(Assign expr);
            T VisitBinaryExpr(Binary expr);
            T VisitCallExpr(Call expr);
            T VisitGetExpr(Get expr);
            T VisitGroupingExpr(Grouping expr);
            T VisitLiteralExpr(Literal expr);
            T VisitLogicalExpr(Logical expr);
            T VisitSuperExpr(Super expr);
            T VisitSetExpr(Set expr);
            T VisitThisExpr(This expr);
            T VisitUnaryExpr(Unary expr);
            T VisitVariableExpr(Variable expr);
        }

        internal abstract T Accept<T>(IVisitor<T> visitor);

        internal class Assign : Expr {
            internal Token Name;
            internal Expr Value;

            internal Assign(Token name, Expr value) {
                Name = name;
                Value = value;
            }

            internal override T Accept<T>(IVisitor<T> visitor) {
                return visitor.VisitAssignExpr(this);
            }
        }

        internal class Binary : Expr {
            internal Expr Left;
            internal Token Op;
            internal Expr Right;

            internal Binary(Expr left, Token op, Expr right) {
                Left = left;
                Op = op;
                Right = right;
            }

            internal override T Accept<T>(IVisitor<T> visitor) {
                return visitor.VisitBinaryExpr(this);
            }
        }

        internal class Call : Expr {
            internal Expr Callee;
            internal Token Paren;
            internal List<Expr> Arguments;

            internal Call(Expr callee, Token paren, List<Expr> arguments) {
                Callee = callee;
                Paren = paren;
                Arguments = arguments;
            }

            internal override T Accept<T>(IVisitor<T> visitor) {
                return visitor.VisitCallExpr(this);
            }
        }

        internal class Get : Expr {
            internal Expr Obj;
            internal Token Name;

            internal Get(Expr obj, Token name) {
                Obj = obj;
                Name = name;
            }

            internal override T Accept<T>(IVisitor<T> visitor) {
                return visitor.VisitGetExpr(this);
            }
        }

        internal class Grouping : Expr {
            internal Expr Expression;

            internal Grouping(Expr expression) {
                Expression = expression;
            }

            internal override T Accept<T>(IVisitor<T> visitor) {
                return visitor.VisitGroupingExpr(this);
            }
        }

        internal class Literal : Expr {
            internal object Value;

            internal Literal(object value) {
                Value = value;
            }

            internal override T Accept<T>(IVisitor<T> visitor) {
                return visitor.VisitLiteralExpr(this);
            }
        }

        internal class Logical : Expr {
            internal Expr Left;
            internal Token Op;
            internal Expr Right;

            internal Logical(Expr left, Token op, Expr right) {
                Left = left;
                Op = op;
                Right = right;
            }

            internal override T Accept<T>(IVisitor<T> visitor) {
                return visitor.VisitLogicalExpr(this);
            }
        }

        internal class Super : Expr {
            internal Token Keyword;
            internal Token Method;

            internal Super(Token keyword, Token method) {
                Keyword = keyword;
                Method = method;
            }

            internal override T Accept<T>(IVisitor<T> visitor) {
                return visitor.VisitSuperExpr(this);
            }
        }

        internal class Set : Expr {
            internal Expr Obj;
            internal Token Name;
            internal Expr Value;

            internal Set(Expr obj, Token name, Expr value) {
                Obj = obj;
                Name = name;
                Value = value;
            }

            internal override T Accept<T>(IVisitor<T> visitor) {
                return visitor.VisitSetExpr(this);
            }
        }

        internal class This : Expr {
            internal Token Keyword;

            internal This(Token keyword) {
                Keyword = keyword;
            }

            internal override T Accept<T>(IVisitor<T> visitor) {
                return visitor.VisitThisExpr(this);
            }
        }

        internal class Unary : Expr {
            internal Token Op;
            internal Expr Right;

            internal Unary(Token op, Expr right) {
                Op = op;
                Right = right;
            }

            internal override T Accept<T>(IVisitor<T> visitor) {
                return visitor.VisitUnaryExpr(this);
            }
        }

        internal class Variable : Expr {
            internal Token Name;

            internal Variable(Token name) {
                Name = name;
            }

            internal override T Accept<T>(IVisitor<T> visitor) {
                return visitor.VisitVariableExpr(this);
            }
        }

    }
}
