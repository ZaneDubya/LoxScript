using System;
using static LoxScript.VirtualMachine.EGearsOpCode;

namespace LoxScript.VirtualMachine {
    partial class Gears {

        // === Disassembly ===========================================================================================
        // ===========================================================================================================

        public void Disassemble(GearsChunk chunk) {
            Console.WriteLine($"=== chunk ===");
            int offset = 0;
            while (offset < chunk.CodeSize) {
                offset = Disassemble(chunk, offset);
            }
        }

        private int Disassemble(GearsChunk chunk, int offset) {
            Console.Write($"{offset:X4}  ");
            EGearsOpCode instruction = (EGearsOpCode)chunk.ReadCode(ref offset);
            switch (instruction) {
                case OP_CONSTANT:
                    return DisassembleConstant("OP_CONSTANT", chunk, offset, OP_CONSTANT);
                case OP_LOAD_STRING:
                    return DisassembleConstant("OP_STRING", chunk, offset, OP_LOAD_STRING);
                case OP_LOAD_FUNCTION:
                    return DisassembleFunction("OP_FUNCTION", chunk, offset);
                case OP_NIL:
                    return DisassembleSimple("OP_NIL", chunk, offset);
                case OP_TRUE:
                    return DisassembleSimple("OP_TRUE", chunk, offset);
                case OP_FALSE:
                    return DisassembleSimple("OP_FALSE", chunk, offset);
                case OP_POP:
                    return DisassembleSimple("OP_POP", chunk, offset);
                case OP_GET_LOCAL:
                    return DisassembleTwoParams("OP_GET_LOCAL", chunk, offset);
                case OP_SET_LOCAL:
                    return DisassembleTwoParams("OP_SET_LOCAL", chunk, offset);
                case OP_DEFINE_GLOBAL:
                    return DisassembleConstant("OP_DEF_GLOBAL", chunk, offset, OP_LOAD_STRING);
                case OP_GET_GLOBAL:
                    return DisassembleConstant("OP_GET_GLOBAL", chunk, offset, OP_LOAD_STRING);
                case OP_SET_GLOBAL:
                    return DisassembleConstant("OP_SET_GLOBAL", chunk, offset, OP_LOAD_STRING);
                case OP_GET_UPVALUE:
                    return DisassembleTwoParams("OP_GET_UPVALUE", chunk, offset);
                case OP_SET_UPVALUE:
                    return DisassembleTwoParams("OP_SET_UPVALUE", chunk, offset);
                case OP_GET_PROPERTY:
                    return DisassembleConstant("OP_GET_PROPERTY", chunk, offset, OP_LOAD_STRING);
                case OP_SET_PROPERTY:
                    return DisassembleConstant("OP_SET_PROPERTY", chunk, offset, OP_LOAD_STRING);
                case OP_GET_SUPER:
                    return DisassembleConstant("OP_GET_SUPER", chunk, offset, OP_LOAD_STRING);
                case OP_EQUAL:
                    return DisassembleSimple("OP_EQUAL", chunk, offset);
                case OP_GREATER:
                    return DisassembleSimple("OP_GREATER", chunk, offset);
                case OP_LESS:
                    return DisassembleSimple("OP_LESS", chunk, offset);
                case OP_ADD:
                    return DisassembleSimple("OP_ADD", chunk, offset);
                case OP_SUBTRACT:
                    return DisassembleSimple("OP_SUBTRACT", chunk, offset);
                case OP_MULTIPLY:
                    return DisassembleSimple("OP_MULTIPLY", chunk, offset);
                case OP_DIVIDE:
                    return DisassembleSimple("OP_DIVIDE", chunk, offset);
                case OP_NOT:
                    return DisassembleSimple("OP_NOT", chunk, offset);
                case OP_NEGATE:
                    return DisassembleSimple("OP_NEGATE", chunk, offset);
                case OP_PRINT:
                    return DisassembleSimple("OP_PRINT", chunk, offset);
                case OP_JUMP:
                    return DisassembleTwoParams("OP_JUMP", chunk, offset);
                case OP_JUMP_IF_FALSE:
                    return DisassembleTwoParams("OP_JUMP_IF_FALSE", chunk, offset);
                case OP_LOOP:
                    return DisassembleTwoParams("OP_LOOP", chunk, offset);
                case OP_CALL:
                    return DisassembleOneParam("OP_CALL", chunk, offset);
                case OP_INVOKE:
                    return DisassembleInvoke("OP_INVOKE", chunk, offset);
                case OP_SUPER_INVOKE:
                    return DisassembleInvoke("OP_SUPER_INVOKE", chunk, offset);
                case OP_CLOSURE:
                    return DisassembleClosure("OP_CLOSURE", chunk, offset);
                case OP_CLOSE_UPVALUE:
                    return DisassembleSimple("OP_CLOSE_UPVALUE", chunk, offset);
                case OP_RETURN:
                    return DisassembleSimple("OP_RETURN", chunk, offset);
                case OP_CLASS:
                    return DisassembleConstant("OP_CLASS", chunk, offset, OP_LOAD_STRING);
                case OP_INHERIT:
                    return DisassembleSimple("OP_INHERIT", chunk, offset);
                case OP_METHOD:
                    return DisassembleSimple("OP_METHOD", chunk, offset);
                default:
                    Console.WriteLine($"Unknown opcode {instruction}");
                    return offset;
            }
        }

        private int DisassembleInvoke(string name, GearsChunk chunk, int offset) {
            int args = chunk.ReadCode(ref offset);
            int nameIndex = (chunk.ReadCode(ref offset) << 8) + chunk.ReadCode(ref offset);
            string value = chunk.ReadStringConstant(nameIndex);
            Console.WriteLine($"{name} const[{nameIndex}] ({value})");
            return offset;
        }

        private int DisassembleClosure(string name, GearsChunk chunk, int offset) {
            int upvalueCount = chunk.ReadCode(ref offset);
            Console.WriteLine($"{name} ({upvalueCount} upvalues)");
            for (int i = 0; i < upvalueCount; i++) {
                chunk.ReadCode(ref offset); // is local?
                chunk.ReadCode(ref offset); // index
            }
            return offset;
        }

        private int DisassembleFunction(string name, GearsChunk chunk, int offset) {
            int argCount = chunk.ReadCode(ref offset);
            int nameIndex = (chunk.ReadCode(ref offset) << 8) + chunk.ReadCode(ref offset);
            string value = chunk.ReadStringConstant(nameIndex);
            int fnAddress = (chunk.ReadCode(ref offset) << 8) + chunk.ReadCode(ref offset);
            Console.WriteLine($"{name} {value}({argCount} arguments) @{fnAddress:X4}");
            return offset;
        }

        private int DisassembleSimple(string name, GearsChunk chunk, int offset) {
            Console.WriteLine(name);
            return offset;
        }

        private int DisassembleOneParam(string name, GearsChunk chunk, int offset) {
            int index = chunk.ReadCode(ref offset);
            Console.WriteLine($"{name} ({index})");
            return offset;
        }

        private int DisassembleTwoParams(string name, GearsChunk chunk, int offset) {
            int index = (chunk.ReadCode(ref offset) << 8) + chunk.ReadCode(ref offset);
            Console.WriteLine($"{name} ({index})");
            return offset;
        }

        private int DisassembleConstant(string name, GearsChunk chunk, int offset, EGearsOpCode constantType) {
            int constantIndex = (chunk.ReadCode(ref offset) << 8) + chunk.ReadCode(ref offset);
            switch (constantType) {
                case OP_CONSTANT: {
                        GearsValue value = chunk.ReadConstantValue(constantIndex);
                        Console.WriteLine($"{name} const[{constantIndex}] ({value})");
                    }
                    break;
                case OP_LOAD_STRING: {
                        string value = chunk.ReadStringConstant(constantIndex);
                        Console.WriteLine($"{name} const[{constantIndex}] ({value})");
                    }
                    break;
                case OP_LOAD_FUNCTION: {
                        string value = chunk.ReadStringConstant(constantIndex);
                        Console.WriteLine($"{name} const[{constantIndex}] ({value})");
                    }
                    break;
            }
            return offset;
        }
    }
}
