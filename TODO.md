# Language changes

Syntactic sugar:
- Add ++, --, +=, -=, /=, *=, and modulus % and %=

Parsing Expressions:
- Add support for comma expressions.
- Add ternary ?: operator.
- Add error production to handle each binary operator that appears without a left-hand operation.

Control Flow:
- Add break.
- Add continue.
- Make sure these are only usable in loops.

Functions:
- Add anonymous functions:

Resolving and Binding:
- Don't allow redefinition of a local within an enclosed scope.
- Extend resolver to report error if local variable is never used.
- See item four for a more efficient reference system.

Classes
- Add Static Methods.
- Properties (vars in class).
- private and public modifiers (maybe).
- Getters and Setters.
- init with New
- How does set and get work??

Inheritance:
- Replace inherits extender '<' with ':'
- Replace "super" with "base"

# VM Changes
- Functions, methods, class definitions, etc should all exist in one chunk.
- If that means that some data should exist in a metadata byte array, that's fine.
- Figuring out what function is currently running shouldn't rely on stack slot zero (as in 24.2.1).
- The call stack should be on the stack, not in a separate frames array (as in 24.3.3). Should instead have a "BP" (base pointer) that points to the function definition of the currently called script. Parameters would follow the base. Frame pointer would point to the bottom of the local stack for the current function. Then stack pointer would point to the top of the stack.