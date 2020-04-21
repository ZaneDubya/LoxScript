using System;
using static LoxScript.VirtualMachine.GearsOpCode;

namespace LoxScript.VirtualMachine {
    /// <summary>
    /// Gears is a bytecode virtual machine for the Lox language.
    /// </summary>
    class Gears {

        internal Gears() {
            GearsChunk chunk = new GearsChunk("test chunk");
            chunk.Write(OP_CONSTANT);
            chunk.Write((byte)chunk.AddConstant(1.2));
            chunk.Write(OP_RETURN);
            DisassembleChunk(chunk);
        }

        private void DisassembleChunk(GearsChunk chunk) {
            Console.WriteLine($"=== {chunk.Name} ===");
            int offset = 0;
            while (offset < chunk.Count) {
                offset = DisassembleInstruction(chunk, offset);
            }
        }

        private int DisassembleInstruction(GearsChunk chunk, int offset) {
            Console.Write($"{offset:X4}  ");
            int value = chunk.Read(ref offset);
            switch ((GearsOpCode)value) {
                case OP_CONSTANT:
                    return DisassembleConstantInstruction("OP_CONSTANT", chunk, offset);
                case OP_RETURN:
                    return DisassembleSimpleInstruction("OP_RETURN", chunk, offset);
                default:
                    Console.WriteLine($"Unknown opcode {value:X2}");
                    return offset;
            }
        }

        private int DisassembleConstantInstruction(string name, GearsChunk chunk, int offset) {
            int constantIndex = chunk.Read(ref offset);
            GearsValue value = chunk.GetConstant(constantIndex);
            Console.WriteLine($"{name} #{constantIndex} ({value})");
            return offset;
        }

        private int DisassembleSimpleInstruction(string name, GearsChunk chunk, int offset) {
            Console.WriteLine(name);
            return offset;
        }
    }
}
