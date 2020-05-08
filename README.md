# LoxScript

LoxScript is an implementation of Bob Nystrom's [Lox Language](http://craftinginterpreters.com/the-lox-language.html) written in C#. I have implemented both a slow runtime interpreter ("Engine") and a fast(ish) bytecode virtual machine ("Gears") for this language.

## "Gears"

The LoxScript bytecode virtual machine is called [Gears](https://github.com/ZaneDubya/LoxScript/tree/master/LoxScript/Gears).

## "Engine" - a LoxScript Interpreter

The LoxScript interpreter is called [Engine](https://github.com/ZaneDubya/LoxScript/tree/master/LoxScript/Interpreter).  Engine includes a complete abstract syntax tree grammar generator, scanner/parser, and interpreter written in C#, as described in Chapters 4-13 of Bob's book [Crafting Interpreters](http://craftinginterpreters.com). 

The Lox language grammar is declared in a set of class definitions. These are created by the included GenerateGrammar project. The effect of running GenerateGrammar is that it will overwrite two files in the main LoxScript project: [Interpreter/Expr.cs](LoxScript/Interpreter/Expr.cs) and [Interpreter/Stmt.cs](LoxScript/Interpreter/Stmt.cs). These files contain the class definitions that make up the nodes of the abstract syntax tree (AST). You will not need to run GenerateGrammar unless you are adding additional grammar to the Lox language. Note that GenerateGrammar will only change the functionality of Engine, not Gears.

At runtime, LoxScript either reads input from the console or a file. [LoxScript/Scanner.cs](LoxScript/Scanner.cs) transforms the input into a list of Tokens. Tokens defined by reserved keywords are recognized by checking against [LoxScript/Grammar/Keywords.cs](LoxScript/Grammar/Keywords.cs). [LoxScript/Parser.cs](LoxScript/Parser.cs) then transforms the list of tokens into the AST. Engine walks the AST, executing each node using the Visitor pattern.
