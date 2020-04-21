# LoxScript

LoxScript is a complete abstract syntax tree grammar generator, scanner/parser, and interpreter for Bob Nystrom's [Lox Language](http://craftinginterpreters.com/the-lox-language.html) written in C#, as described in Chapters 4-13 of Bob's book [Crafting Interpreters](http://craftinginterpreters.com).

## Grammar Generator

The Lox language grammar is declared in a set of class definitions. These are created by the included GenerateGrammar project.

The effect of running GenerateGrammar is that it will overwrite two files in the main LoxScript project: [LoxScript/Grammar/Expr.cs](LoxScript/Grammar/Expr.cs) and [LoxScript/Grammar/Stmt.cs](LoxScript/Grammar/Stmt.cs). These files contain the class definitions that will make up the nodes of the abstract syntax tree (AST).

You do not need to run GenerateGrammar unless you are adding additional grammar to the Lox language.

## Scanner / Parser

At runtime, LoxScript either reads input from the console or a file. [LoxScript/Scanner.cs](LoxScript/Scanner.cs) transforms the input into a list of Tokens. Tokens defined by reserved keywords are recognized by checking against [LoxScript/Grammar/Keywords.cs](LoxScript/Grammar/Keywords.cs).

[LoxScript/Parser.cs](LoxScript/Parser.cs) then transforms the list of tokens into the AST which will then execute the program.

## Interpreter

The LoxScript interpreter is called (Engine)[LoxScript/Interpreter]. Engine walks the AST, executing each node using the Visitor pattern.
