# LoxScript

LoxScript is a bytecode compiler and virtual machine ("Gears") for Bob Nystrom's [Lox language](http://craftinginterpreters.com/the-lox-language.html), both written in C#.

The LoxScript bytecode virtual machine is called [Gears](https://github.com/ZaneDubya/LoxScript/tree/master/LoxScript/Core/Scripting/VirtualMachine). Gears executes bytecode created by the LoxScript compiler. The output of Gears matches the reference "clox" virtual machine described in Chapters 14-30 of Bob's book [Crafting Interpreters](http://craftinginterpreters.com). However, the internal implementation of the virtual machine is different, and the bytecode generated by the Gears compiler will not run on clox.

Improvements over Lox/clox:

- Compiler can write bytecode to a binary file and load/run this at runtime.
- Native interfaces allow Lox files to interact with native C# functions and objects.

Performance:

- Gears runs the reference 'fibonnaci' benchmark roughly 69x slower than native c# code.
