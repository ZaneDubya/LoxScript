﻿using System;
using static XPT.Core.Scripting.LoxScript.VirtualMachine.EGearsOpCode;

namespace XPT.Core.Scripting.LoxScript.VirtualMachine {
    internal sealed partial class Gears { // disassembly

        // === Disassembly ===========================================================================================
        // ===========================================================================================================

        public static void Disassemble(GearsChunk chunk, Action<string> write, Action<string> writeLine) {
            writeLine($"=== chunk ===");
            int offset = 0;
            while (offset < chunk.SizeCode) {
                offset = Disassemble(chunk, offset, write, writeLine);
            }
        }

        private static int Disassemble(GearsChunk chunk, int offset, Action<string> write, Action<string> writeLine) {
            write($"{chunk.Lines[offset]:D4}  {offset:D4}  ");
            EGearsOpCode instruction = (EGearsOpCode)chunk.ReadCode(ref offset);
            switch (instruction) {
                case OP_LOAD_CONSTANT:
                    return DisassembleConstant("OP_LOAD_CONSTANT", chunk, offset, OP_LOAD_CONSTANT, writeLine);
                case OP_LOAD_STRING:
                    return DisassembleConstant("OP_LOAD_STRING", chunk, offset, OP_LOAD_STRING, writeLine);
                case OP_LOAD_FUNCTION:
                    return DisassembleFunction("OP_LOAD_FUNCTION", chunk, offset, writeLine);
                case OP_NIL:
                    return DisassembleSimple("OP_NIL", chunk, offset, writeLine);
                case OP_TRUE:
                    return DisassembleSimple("OP_TRUE", chunk, offset, writeLine);
                case OP_FALSE:
                    return DisassembleSimple("OP_FALSE", chunk, offset, writeLine);
                case OP_POP:
                    return DisassembleSimple("OP_POP", chunk, offset, writeLine);
                case OP_GET_LOCAL:
                    return DisassembleTwoParams("OP_GET_LOCAL", chunk, offset, writeLine);
                case OP_SET_LOCAL:
                    return DisassembleTwoParams("OP_SET_LOCAL", chunk, offset, writeLine);
                case OP_DEFINE_GLOBAL:
                    return DisassembleConstant("OP_DEF_GLOBAL", chunk, offset, OP_LOAD_FUNCTION, writeLine);
                case OP_GET_GLOBAL:
                    return DisassembleConstant("OP_GET_GLOBAL", chunk, offset, OP_LOAD_FUNCTION, writeLine);
                case OP_SET_GLOBAL:
                    return DisassembleConstant("OP_SET_GLOBAL", chunk, offset, OP_LOAD_FUNCTION, writeLine);
                case OP_GET_UPVALUE:
                    return DisassembleTwoParams("OP_GET_UPVALUE", chunk, offset, writeLine);
                case OP_SET_UPVALUE:
                    return DisassembleTwoParams("OP_SET_UPVALUE", chunk, offset, writeLine);
                case OP_GET_PROPERTY:
                    return DisassembleConstant("OP_GET_PROPERTY", chunk, offset, OP_LOAD_FUNCTION, writeLine);
                case OP_SET_PROPERTY:
                    return DisassembleConstant("OP_SET_PROPERTY", chunk, offset, OP_LOAD_FUNCTION, writeLine);
                case OP_GET_SUPER:
                    return DisassembleConstant("OP_GET_SUPER", chunk, offset, OP_LOAD_FUNCTION, writeLine);
                case OP_EQUAL:
                    return DisassembleSimple("OP_EQUAL", chunk, offset, writeLine);
                case OP_EQUAL_PRESERVE_FIRST_VALUE:
                    return DisassembleSimple("OP_EQUAL_SAVING_FIRST_VALUE", chunk, offset, writeLine);
                case OP_GREATER:
                    return DisassembleSimple("OP_GREATER", chunk, offset, writeLine);
                case OP_LESS:
                    return DisassembleSimple("OP_LESS", chunk, offset, writeLine);
                case OP_ADD:
                    return DisassembleSimple("OP_ADD", chunk, offset, writeLine);
                case OP_SUBTRACT:
                    return DisassembleSimple("OP_SUBTRACT", chunk, offset, writeLine);
                case OP_MULTIPLY:
                    return DisassembleSimple("OP_MULTIPLY", chunk, offset, writeLine);
                case OP_DIVIDE:
                    return DisassembleSimple("OP_DIVIDE", chunk, offset, writeLine);
                case OP_MODULUS:
                    return DisassembleSimple("OP_MODULUS", chunk, offset, writeLine);
                case OP_NOT:
                    return DisassembleSimple("OP_NOT", chunk, offset, writeLine);
                case OP_NEGATE:
                    return DisassembleSimple("OP_NEGATE", chunk, offset, writeLine);
                case OP_BITWISE_COMPLEMENT:
                    return DisassembleSimple("OP_BITWISE_COMPLEMENT", chunk, offset, writeLine);
                case OP_INCREMENT:
                    return DisassembleSimple("OP_INC", chunk, offset, writeLine);
                case OP_DECREMENT:
                    return DisassembleSimple("OP_DEC", chunk, offset, writeLine);
                case OP_JUMP:
                    return DisassembleJump("OP_JUMP", chunk, offset, writeLine);
                case OP_JUMP_IF_FALSE:
                    return DisassembleJump("OP_JUMP_IF_FALSE", chunk, offset, writeLine);
                case OP_LOOP:
                    return DisassembleLoop("OP_LOOP", chunk, offset, writeLine);
                case OP_CALL:
                    return DisassembleOneParam("OP_CALL", chunk, offset, writeLine);
                case OP_INVOKE:
                    return DisassembleInvoke("OP_INVOKE", chunk, offset, writeLine);
                case OP_SUPER_INVOKE:
                    return DisassembleInvokeSuper("OP_SUPER_INVOKE", chunk, offset, writeLine);
                case OP_CLOSE_UPVALUE:
                    return DisassembleSimple("OP_CLOSE_UPVALUE", chunk, offset, writeLine);
                case OP_RETURN:
                    return DisassembleSimple("OP_RETURN", chunk, offset, writeLine);
                case OP_CLASS:
                    return DisassembleConstant("OP_CLASS", chunk, offset, OP_LOAD_FUNCTION, writeLine);
                case OP_INHERIT:
                    return DisassembleSimple("OP_INHERIT", chunk, offset, writeLine);
                case OP_METHOD:
                    return DisassembleConstant("OP_METHOD", chunk, offset, OP_LOAD_FUNCTION, writeLine);
                default:
                    writeLine($"Unknown opcode {instruction}");
                    return offset;
            }
        }

        private static int DisassembleInvoke(string opcode, GearsChunk chunk, int offset, Action<string> writeLine) {
            int argCount = chunk.ReadCode(ref offset);
            int nameIndex = (chunk.ReadCode(ref offset) << 8) + chunk.ReadCode(ref offset);
            string name = chunk.VarNameStrings.ReadStringConstant(nameIndex);
            writeLine($"{opcode} const[{nameIndex}] ({name})");
            return offset;
        }

        private static int DisassembleInvokeSuper(string opcode, GearsChunk chunk, int offset, Action<string> writeLine) {
            int argCount = chunk.ReadCode(ref offset);
            chunk.ReadCode(ref offset); // OP GET UPVALUE
            int slot = (chunk.ReadCode(ref offset) << 8) + chunk.ReadCode(ref offset);
            int nameIndex = (chunk.ReadCode(ref offset) << 8) + chunk.ReadCode(ref offset);
            string name = chunk.VarNameStrings.ReadStringConstant(nameIndex);
            writeLine($"{opcode} const[{nameIndex}] ({name})");
            return offset;
        }

        private static int DisassembleFunction(string name, GearsChunk chunk, int offset, Action<string> writeLine) {
            int argCount = chunk.ReadCode(ref offset);
            int fnAddress = (chunk.ReadCode(ref offset) << 8) + chunk.ReadCode(ref offset);
            int upvalueCount = chunk.ReadCode(ref offset);
            writeLine($"{name} ({argCount} arguments, {upvalueCount} upvalues) @{fnAddress:D4}");
            for (int i = 0; i < upvalueCount; i++) {
                chunk.ReadCode(ref offset); // local?
                chunk.ReadCode(ref offset); // index
            }
            return offset;
        }

        private static int DisassembleSimple(string name, GearsChunk chunk, int offset, Action<string> writeLine) {
            writeLine(name);
            return offset;
        }

        private static int DisassembleOneParam(string name, GearsChunk chunk, int offset, Action<string> writeLine) {
            int index = chunk.ReadCode(ref offset);
            writeLine($"{name} ({index})");
            return offset;
        }

        private static int DisassembleTwoParams(string name, GearsChunk chunk, int offset, Action<string> writeLine) {
            int index = (chunk.ReadCode(ref offset) << 8) + chunk.ReadCode(ref offset);
            writeLine($"{name} ({index})");
            return offset;
        }

        private static int DisassembleJump(string name, GearsChunk chunk, int offset, Action<string> writeLine) {
            int index = (offset + 2) + (chunk.ReadCode(ref offset) << 8) + chunk.ReadCode(ref offset);
            writeLine($"{name} >>> {index}");
            return offset;
        }

        private static int DisassembleLoop(string name, GearsChunk chunk, int offset, Action<string> writeLine) {
            int index = (offset + 2) - ((chunk.ReadCode(ref offset) << 8) + chunk.ReadCode(ref offset));
            writeLine($"{name} >>> {index}");
            return offset;
        }

        private static int DisassembleConstant(string name, GearsChunk chunk, int offset, EGearsOpCode constantType, Action<string> writeLine) {
            int constantIndex = (chunk.ReadCode(ref offset) << 8) + chunk.ReadCode(ref offset);
            switch (constantType) {
                case OP_LOAD_CONSTANT: {
                        GearsValue value = chunk.ReadConstantValue(constantIndex);
                        writeLine($"{name} const[{constantIndex}] ({value})");
                    }
                    break;
                case OP_LOAD_STRING: {
                        string value = chunk.Strings.ReadStringConstant(constantIndex);
                        writeLine($"{name} string[{constantIndex}] (\"{value}\")");
                    }
                    break;
                case OP_LOAD_FUNCTION: {
                        string value = chunk.VarNameStrings.ReadStringConstant(constantIndex);
                        writeLine($"{name} varname[{constantIndex}] ({value})");
                    }
                    break;
            }
            return offset;
        }
    }
}
