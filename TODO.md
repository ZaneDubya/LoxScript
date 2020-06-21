# Lox language modifications
My goal with this project is to create a scripting language for the MedievaLands server. This means I would have to add some functionality to the language that would allow me to reference and modify 'native' server objects, which would be implemented in c#, possibly with lox 'wrapper' ('resolver'?) objects.

I also recognize that Lox is intentionally fairly bare bones. Although it is syntactically a C-derivative in syntax, it lacks many of C's operators. So I'd like to add syntax and grammar to my lox implementation to allow it to make use of the language features and syntactic sugar that I enjoy in my main programming language, C#.

In his book [Crafting Interpreters](http://craftinginterpreters.com/optimization.html), Bob Nystrom wrote:
> If you make significant changes to the language, it would be good to also change the name, mostly to avoid confusing people about what “Lox” refers to.

It is an open question as to whether and when I should change the name. If I do, I would keep the name very similar ("dox" - derivative of lox? "nox" - not original lox? "zox" - zane's lox? "sox" - scripting lox?)

## Language changes

Add native interfaces:
- [x] The most important addition! Pass a native objects to a lox script that can be accessed and modified by the script.
- [ ] Document limitations of native object interface.

Compile, Load, and Execute
- [x] Add 'compile' functionality that saves complete byte code to a file/array that can then be run from scratch.
- [x] Also create a reusable 'gears' object that can host a script to completion, unload the script, and then be reused for another script without disposing of the gears.

Print and Assert:
-[x] Make 'print' a native function and get rid of the associate op_print operation.
-[ ] Add 'assert' native function that compares output to an expected value (probably a string, maybe values too?).

Operators and syntactic sugar:
- [ ] Add ++, --, +=, -=, /=, *=.
- [ ] Add modulus % and %=.
- [ ] Add binary |, |=, &, &=.

Parsing Expressions:
- [ ] Add ternary ?: operator.
- [ ] Add error production to handle each binary operator that appears without a left-hand operation.

Control Flow:
- [ ] Add break within loops.
- [ ] Add continue within loops.

## Missing functionality:
- [ ] Add line information to compiled lox code.

## Interesting ideas, but probably won't do:

Resolving and Binding:
- [ ] Don't allow redefinition of a local within an enclosed scope (possibly, I'm of two minds here).

Classes:
- [ ] Add Static Methods.
- [ ] Allow properties and don't allow addition of new properties via code (vars in class)
- [ ] private and public modifiers (maybe).
- [ ] Getters and Setters (maybe maybe).
- [ ] Replace "init" with "new" and C# ctor syntax (classname()).

Inheritance:
- [ ] Replace inherits syntax '<' with ':'
- [ ] Replace "super" with "base"