using System;

namespace XPT.Core.Scripting.LoxScript.VirtualMachine {
    /// <summary>
    /// Thrown when Virtual Machine instance encounters an error.
    /// </summary>
    internal class GearsRuntimeException : Exception {
        private readonly int _Line;

        internal GearsRuntimeException(int line, string message) : base(message) {
            _Line = line;
        }

        internal GearsRuntimeException(string message) : base(message) {
            _Line = -1;
        }

        public override string ToString() {
            // todo: print stack trace 24.5.2
            if (_Line >= 0) {
                return $"[Line {_Line}] {base.Message}";
            }
            return base.Message;
        }
    }
}
