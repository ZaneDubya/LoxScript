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
- Add anonymous functions.

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

Inheritance(???):
- Replace inherits extender '<' with ':'
- Replace "super" with "base"

# VM Changes
- Functions, methods, class definitions, etc should all exist in one chunk.
- If that means that some data should exist in a metadata byte array, that's fine.
- Proposed format: byte[] code, byte[] constantStrings, value[] constantValues.
