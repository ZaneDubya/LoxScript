namespace LoxScript.VirtualMachine {
    enum GearsOpCode {
        /// <summary>
        /// 'Loads' the indexed constant for use.
        /// </summary>
        OP_CONSTANT,

        /// <summary>
        /// Return from the current function.
        /// </summary>
        OP_RETURN 
    }
}
