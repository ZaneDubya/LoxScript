using System;

namespace LoxScript.VirtualMachine {
    /// <summary>
    /// Throw this when vm encounters an error
    /// </summary>
    class GearsRuntimeException : Exception {
        private readonly int _Line;

        internal GearsRuntimeException(int line, string message) : base(message) {
            _Line = line;
        }

        internal void Print() {
            // todo: print stack trace 24.5.2
            Program.Error(_Line, Message);
        }
    }
}
