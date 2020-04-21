namespace LoxScript.VirtualMachine {
    enum EGearsOpCode {
        /// <summary>
        /// 'Loads' the indexed constant onto the stack.
        /// </summary>
        OP_CONSTANT,

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

        /// <summary>
        /// Pops a value on the stack, negates it, and pushes it back onto the stack.
        /// </summary>
        OP_NEGATE,

        /// <summary>
        /// Pops a value from the stack and returns it as the result of the current function.
        /// </summary>
        OP_RETURN 
    }
}
