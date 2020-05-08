using LoxScript.VirtualMachine;
using System.Collections.Generic;

namespace LoxScript.Compiling {
    class CompilerChunk {
        public readonly string Name;
        
        // The number of parameters expected by the function.
        public int Arity;
        
        // The executable code and constants associated with this function.
        public readonly GearsChunk Chunk;

        // Fixups:
        private List<int> _FixupConstantLocs = new List<int>();
        private List<int> _FixupStringLocs = new List<int>();

        public CompilerChunk(string name, int arity) {
            Name = name;
            Arity = arity;
            Chunk = new GearsChunk(name);
        }
    }
}
