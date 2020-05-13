using System;

namespace XPT.VirtualMachine {
    /// <summary>
    /// Throw this when vm encounters an error
    /// </summary>
    class GearsRuntimeException : Exception {
        private readonly int _Line;

        internal GearsRuntimeException(int line, string message) : base(message) {
            _Line = line;
        }

        internal GearsRuntimeException(string message) : base(message) {
            _Line = -1;
        }

        internal void Print() {
            // todo: print stack trace 24.5.2
            Program.Error(_Line, Message);
        }

        public override string ToString() {
            if (_Line >= 0) {
                return $"[Line {_Line}] {base.Message}";
            }
            return base.Message;
        }
    }
}
