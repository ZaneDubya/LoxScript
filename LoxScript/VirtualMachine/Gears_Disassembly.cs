using System;
using static LoxScript.VirtualMachine.EGearsOpCode;

namespace LoxScript.VirtualMachine {
    partial class Gears {

        // === Disassembly ===========================================================================================
        // ===========================================================================================================

        public void Disassemble(GearsChunk chunk) {
            Console.WriteLine($"=== {chunk.Name} ===");
            int offset = 0;
            while (offset < chunk.CodeSize) {
                offset = DisassembleInstruction(chunk, offset);
            }
        }

        private int DisassembleInstruction(GearsChunk chunk, int offset) {
            Console.Write($"{offset:X4}  ");
            EGearsOpCode instruction = (EGearsOpCode)chunk.Read(ref offset);
            switch (instruction) {
                case OP_CONSTANT:
                    return DisassembleInstructionConstant("OP_CONSTANT", chunk, offset, OP_CONSTANT);
                case OP_STRING:
                    return DisassembleInstructionConstant("OP_STRING", chunk, offset, OP_STRING);
                case OP_FUNCTION:
                    return DisassembleInstructionConstant("OP_FUNCTION", chunk, offset, OP_FUNCTION);
                case OP_NIL:
                    return DisassembleInstructionSimple("OP_NIL", chunk, offset);
                case OP_TRUE:
                    return DisassembleInstructionSimple("OP_TRUE", chunk, offset);
                case OP_FALSE:
                    return DisassembleInstructionSimple("OP_FALSE", chunk, offset);
                case OP_POP:
                    return DisassembleInstructionSimple("OP_POP", chunk, offset);
                case OP_GET_LOCAL:
                    return DisassembleInstructionTwoParams("OP_GET_LOCAL", chunk, offset);
                case OP_SET_LOCAL:
                    return DisassembleInstructionTwoParams("OP_SET_LOCAL", chunk, offset);
                case OP_DEFINE_GLOBAL:
                    return DisassembleInstructionConstant("OP_DEF_GLOBAL", chunk, offset, OP_STRING);
                case OP_GET_GLOBAL:
                    return DisassembleInstructionConstant("OP_GET_GLOBAL", chunk, offset, OP_STRING);
                case OP_SET_GLOBAL:
                    return DisassembleInstructionConstant("OP_SET_GLOBAL", chunk, offset, OP_STRING);
                case OP_GET_UPVALUE:
                    return DisassembleInstructionTwoParams("OP_GET_UPVALUE", chunk, offset);
                case OP_SET_UPVALUE:
                    return DisassembleInstructionTwoParams("OP_SET_UPVALUE", chunk, offset);
                case OP_GET_PROPERTY:
                    return DisassembleInstructionConstant("OP_GET_PROPERTY", chunk, offset, OP_STRING);
                case OP_SET_PROPERTY:
                    return DisassembleInstructionConstant("OP_SET_PROPERTY", chunk, offset, OP_STRING);
                case OP_EQUAL:
                    return DisassembleInstructionSimple("OP_EQUAL", chunk, offset);
                case OP_GREATER:
                    return DisassembleInstructionSimple("OP_GREATER", chunk, offset);
                case OP_LESS:
                    return DisassembleInstructionSimple("OP_LESS", chunk, offset);
                case OP_ADD:
                    return DisassembleInstructionSimple("OP_ADD", chunk, offset);
                case OP_SUBTRACT:
                    return DisassembleInstructionSimple("OP_SUBTRACT", chunk, offset);
                case OP_MULTIPLY:
                    return DisassembleInstructionSimple("OP_MULTIPLY", chunk, offset);
                case OP_DIVIDE:
                    return DisassembleInstructionSimple("OP_DIVIDE", chunk, offset);
                case OP_NOT:
                    return DisassembleInstructionSimple("OP_NOT", chunk, offset);
                case OP_NEGATE:
                    return DisassembleInstructionSimple("OP_NEGATE", chunk, offset);
                case OP_PRINT:
                    return DisassembleInstructionSimple("OP_PRINT", chunk, offset);
                case OP_JUMP:
                    return DisassembleInstructionTwoParams("OP_JUMP", chunk, offset);
                case OP_JUMP_IF_FALSE:
                    return DisassembleInstructionTwoParams("OP_JUMP_IF_FALSE", chunk, offset);
                case OP_LOOP:
                    return DisassembleInstructionTwoParams("OP_LOOP", chunk, offset);
                case OP_CALL:
                    return DisassembleInstructionOneParam("OP_CALL", chunk, offset);
                case OP_CLOSURE:
                    return DisassembleClosure("OP_CLOSURE", chunk, offset);
                case OP_CLOSE_UPVALUE:
                    return DisassembleInstructionSimple("OP_CLOSE_UPVALUE", chunk, offset);
                case OP_RETURN:
                    return DisassembleInstructionSimple("OP_RETURN", chunk, offset);
                case OP_CLASS:
                    return DisassembleInstructionConstant("OP_CLASS", chunk, offset, OP_STRING);
                default:
                    Console.WriteLine($"Unknown opcode {instruction}");
                    return offset;
            }
        }

        private int DisassembleClosure(string name, GearsChunk chunk, int offset) {
            int upvalueCount = chunk.Read(ref offset);
            Console.WriteLine($"{name} ({upvalueCount} upvalues)");
            for (int i = 0; i < upvalueCount; i++) {
                chunk.Read(ref offset); // is local?
                chunk.Read(ref offset); // index
            }
            return offset;
        }

        private int DisassembleInstructionSimple(string name, GearsChunk chunk, int offset) {
            Console.WriteLine(name);
            return offset;
        }

        private int DisassembleInstructionOneParam(string name, GearsChunk chunk, int offset) {
            int index = chunk.Read(ref offset);
            Console.WriteLine($"{name} ({index})");
            return offset;
        }

        private int DisassembleInstructionTwoParams(string name, GearsChunk chunk, int offset) {
            int index = (chunk.Read(ref offset) << 8) + chunk.Read(ref offset);
            Console.WriteLine($"{name} ({index})");
            return offset;
        }

        private int DisassembleInstructionConstant(string name, GearsChunk chunk, int offset, EGearsOpCode constantType) {
            int constantIndex = (chunk.Read(ref offset) << 8) + chunk.Read(ref offset);
            switch (constantType) {
                case OP_CONSTANT: {
                        GearsValue value = chunk.ReadConstantValue(ref constantIndex);
                        Console.WriteLine($"{name} const[{constantIndex}] ({value})");
                    }
                    break;
                case OP_STRING: {
                        string value = chunk.ReadConstantString(ref constantIndex);
                        Console.WriteLine($"{name} const[{constantIndex}] ({value})");
                    }
                    break;
                case OP_FUNCTION: {
                        string value = chunk.ReadConstantString(ref constantIndex);
                        Console.WriteLine($"{name} const[{constantIndex}] ({value})");
                    }
                    break;
            }
            return offset;
        }
    }
}
