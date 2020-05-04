using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoxScript.VirtualMachine {
    /// <summary>
    /// Throw this when vm encounters an error
    /// </summary>
    class GearsRuntimeException : Exception {
        private readonly int _Line;
        private readonly string _Message;

        internal GearsRuntimeException(int line, string message) {
            _Line = line;
            _Message = message;
        }

        internal void Print() {
            // todo: print stack trace 24.5.2
            Program.Error(_Line, _Message);
        }
    }
}
