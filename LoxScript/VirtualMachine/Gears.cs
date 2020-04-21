using System;
using static LoxScript.VirtualMachine.EGearsResult;
using static LoxScript.VirtualMachine.EGearsOpCode;

namespace LoxScript.VirtualMachine {
    /// <summary>
    /// Gears is a bytecode virtual machine for the Lox language.
    /// </summary>
    class Gears {

        internal Gears() {
            GearsChunk chunk = new GearsChunk("test chunk");
            chunk.Write(OP_CONSTANT);
            chunk.Write((byte)chunk.AddConstant(1.2));
            chunk.Write(OP_CONSTANT);
            chunk.Write((byte)chunk.AddConstant(2.4));
            chunk.Write(OP_DIVIDE);
            chunk.Write(OP_NEGATE);
            chunk.Write(OP_RETURN);
            Run(chunk);
            Disassemble(chunk);
        }

        internal EGearsResult Run(GearsChunk chunk) {
            GearsContext context = new GearsContext();
            while(true) {
                int instruction = chunk.Read(ref context.IP);
                switch ((EGearsOpCode)instruction) {
                    case OP_CONSTANT:
                        GearsValue constant = chunk.GetConstant(chunk.Read(ref context.IP));
                        context.Push(constant);
                        Console.WriteLine($"const => {context.Peek()}");
                        break;
                    case OP_ADD:
                        context.Push(context.Pop() + context.Pop());
                        Console.WriteLine($"add => {context.Peek()}");
                        break;
                    case OP_SUBTRACT:
                        context.Push(context.Pop() - context.Pop());
                        Console.WriteLine($"subtrac => {context.Peek()}");
                        break;
                    case OP_MULTIPLY:
                        context.Push(context.Pop() * context.Pop());
                        Console.WriteLine($"multiply => {context.Peek()}");
                        break;
                    case OP_DIVIDE:
                        context.Push(context.Pop() / context.Pop());
                        Console.WriteLine($"divide => {context.Peek()}");
                        break;
                    case OP_NEGATE:
                        context.Push(-context.Pop());
                        Console.WriteLine($"negate => {context.Peek()}");
                        break;
                    case OP_RETURN:
                        Console.WriteLine($"return => {context.Pop()}");
                        return INTERPRET_OK;
                    default:
                        // todo: throw runtime error
                        return INTERPRET_RUNTIME_ERROR;
                }
            }
        }

        // === Disassembly ===========================================================================================
        // ===========================================================================================================

        private void Disassemble(GearsChunk chunk) {
            Console.WriteLine($"=== {chunk.Name} ===");
            int offset = 0;
            while (offset < chunk.Count) {
                offset = DisassembleInstruction(chunk, offset);
            }
        }

        private int DisassembleInstruction(GearsChunk chunk, int offset) {
            Console.Write($"{offset:X4}  ");
            int instruction = chunk.Read(ref offset);
            switch ((EGearsOpCode)instruction) {
                case OP_CONSTANT:
                    return DisassembleConstantInstruction("OP_CONSTANT", chunk, offset);
                case OP_ADD:
                    return DisassembleSimpleInstruction("OP_ADD", chunk, offset);
                case OP_SUBTRACT:
                    return DisassembleSimpleInstruction("OP_SUBTRACT", chunk, offset);
                case OP_MULTIPLY:
                    return DisassembleSimpleInstruction("OP_MULTIPLY", chunk, offset);
                case OP_DIVIDE:
                    return DisassembleSimpleInstruction("OP_DIVIDE", chunk, offset);
                case OP_NEGATE:
                    return DisassembleSimpleInstruction("OP_NEGATE", chunk, offset);
                case OP_RETURN:
                    return DisassembleSimpleInstruction("OP_RETURN", chunk, offset);
                default:
                    Console.WriteLine($"Unknown opcode {instruction:X2}");
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
