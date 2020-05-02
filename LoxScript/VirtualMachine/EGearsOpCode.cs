namespace LoxScript.VirtualMachine {
    enum EGearsOpCode {
        /// <summary>
        /// Loads the indexed constant onto the stack.
        /// </summary>
        OP_CONSTANT,

        /// <summary>
        /// Loads the indexed string into the heap, and loads an object pointing to that string onto the stack.
        /// </summary>
        OP_STRING,

        /// <summary>
        /// Loads the indexed function into the heap, and loads an object pointing to that function onto the stack.
        /// </summary>
        OP_FUNCTION,

        // Types of Values literal-ops
        OP_NIL,
        OP_TRUE,
        OP_FALSE,

        /// <summary>
        /// Global Variables pop-op
        /// </summary>
        OP_POP,

        /// <summary>
        /// Local Variables get-local-op
        /// </summary>
        OP_GET_LOCAL,

        /// <summary>
        /// Local Variables set-local-op
        /// </summary>
        OP_SET_LOCAL,

        /// <summary>
        /// Global Variables get-global-op
        /// </summary>
        OP_GET_GLOBAL,

        /// <summary>
        /// Global Variables define-global-op
        /// </summary>
        OP_DEFINE_GLOBAL,

        /// <summary>
        /// Global Variables set-global-op
        /// </summary>
        OP_SET_GLOBAL,

        /// <summary>
        /// Closures upvalue-ops
        /// </summary>
        OP_GET_UPVALUE,

        OP_SET_UPVALUE,

        /// <summary>
        /// Classes and Instances property-ops
        /// </summary>
        OP_GET_PROPERTY,

        OP_SET_PROPERTY,

        /// <summary>
        /// Superclasses get-super-op
        /// </summary>
        OP_GET_SUPER,

        /// <summary>
        /// Types of Values comparison-ops
        /// </summary>
        OP_EQUAL,
        OP_GREATER,
        OP_LESS,

        /// <summary>
        /// Pops two values from the stack, adds them, and pushes the result to the stack.
        /// </summary>
        OP_ADD,

        /// <summary>
        /// Pops the subtrahend and minuend from the stack, subtracts them, and pushes the result to the stack.
        /// </summary>
        OP_SUBTRACT,

        /// <summary>
        /// Pops two values from the stack, multiplies them, and pushes the result to the stack.
        /// </summary>
        OP_MULTIPLY,

        /// <summary>
        /// Pops the dividend and divisor from the stack, divides them, and pushes the result to the stack.
        /// </summary>
        OP_DIVIDE,

        OP_NOT,

        /// <summary>
        /// Pops a value on the stack, negates it, and pushes it back onto the stack.
        /// </summary>
        OP_NEGATE,

        OP_PRINT,

        OP_JUMP,

        OP_JUMP_IF_FALSE,

        OP_LOOP,

        OP_CALL,

        OP_INVOKE,

        OP_SUPER_INVOKE,

        /// <summary>
        /// Creates a closure for the preceding function on the stack.
        /// </summary>
        OP_CLOSURE,

        /// <summary>
        /// Creates an upvalue for a local variable captured by a closure. The local variable is on top of the stack.
        /// </summary>
        OP_CLOSE_UPVALUE,

        /// <summary>
        /// Pops a value from the stack and returns it as the result of the current function.
        /// </summary>
        OP_RETURN,
            
        OP_CLASS,

        OP_INHERIT,

        OP_METHOD
    }
}
