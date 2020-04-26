using System;
using static LoxScript.VirtualMachine.EGearsOpCode;

namespace LoxScript.VirtualMachine {
    /// <summary>
    /// Gears is a bytecode virtual machine for the Lox language.
    /// </summary>
    class Gears {

        internal Gears() {
            /*GearsChunk chunk = new GearsChunk("test chunk");
            chunk.Write(OP_CONSTANT);
            chunk.Write((byte)chunk.AddConstant(1.2));
            chunk.Write(OP_CONSTANT);
            chunk.Write((byte)chunk.AddConstant(2.4));
            chunk.Write(OP_DIVIDE);
            chunk.Write(OP_NEGATE);
            chunk.Write(OP_RETURN);
            Run(chunk);
            Disassemble(chunk);*/
        }

        internal bool Run(GearsChunk chunk) {
            GearsContext context = new GearsContext();
            while(true) {
                int instruction = chunk.Read(ref context.IP);
                switch ((EGearsOpCode)instruction) {
                    case OP_CONSTANT:
                        context.Push(chunk.GetConstantValue(chunk.Read(ref context.IP)));
                        // Console.WriteLine($"const => {context.Peek()}");
                        break;
                    case OP_STRING:
                        context.Push(GearsValue.CreateObjPtr(context.AddObject(new GearsObjString(chunk.GetConstantString(chunk.Read(ref context.IP))))));
                        break;
                    case OP_NIL:
                        context.Push(GearsValue.NilValue);
                        // Console.WriteLine($"nil");
                        break;
                    case OP_TRUE:
                        context.Push(GearsValue.TrueValue);
                        // Console.WriteLine($"true");
                        break;
                    case OP_FALSE:
                        context.Push(GearsValue.FalseValue);
                        // Console.WriteLine($"false");
                        break;
                    case OP_POP:
                        context.Pop();
                        break;
                    case OP_GET_LOCAL: {
                            int slot = chunk.Read(ref context.IP);
                            context.Push(context.StackGet(slot));
                            break;
                        }
                    case OP_SET_LOCAL: {
                            int slot = chunk.Read(ref context.IP);
                            context.StackSet(slot, context.Peek());
                            break;
                        }
                    case OP_GET_GLOBAL:
                        GLOBAL_GET(chunk, context);
                        break;
                    case OP_DEFINE_GLOBAL:
                        GLOBAL_DEFINE(chunk, context);
                        break;
                    case OP_SET_GLOBAL:
                        GLOBAL_SET(chunk, context);
                        break;
                    case OP_EQUAL:
                        context.Push(AreValuesEqual(context.Pop(), context.Pop()));
                        break;
                    case OP_GREATER:
                        BINARY_GREATER(chunk, context);
                        break;
                    case OP_LESS:
                        BINARY_LESS(chunk, context);
                        break;
                    case OP_ADD:
                        BINARY_ADD(chunk, context);
                        break;
                    case OP_SUBTRACT:
                        BINARY_SUBTRACT(chunk, context);
                        break;
                    case OP_MULTIPLY:
                        BINARY_MULTIPLY(chunk, context);
                        break;
                    case OP_DIVIDE:
                        BINARY_DIVIDE(chunk, context);
                        break;
                    case OP_NOT:
                        context.Push(IsFalsey(context.Pop()));
                        // Console.WriteLine($"not => {context.Peek()}");
                        break;
                    case OP_NEGATE:
                        BINARY_NEGATE(chunk, context);
                        break;
                    case OP_PRINT:
                        Console.WriteLine($"print => {context.Pop().ToString(context)}");
                        break;
                    case OP_JUMP: {
                            int offset = (chunk.Read(ref context.IP) << 8) | chunk.Read(ref context.IP);
                            context.IP += offset;
                            break;
                        }
                    case OP_JUMP_IF_FALSE: {
                            int offset = (chunk.Read(ref context.IP) << 8) | chunk.Read(ref context.IP);
                            if (IsFalsey(context.Peek())) {
                                context.IP += offset;
                            }
                            break;
                        }
                    case OP_LOOP: {
                            int offset = (chunk.Read(ref context.IP) << 8) | chunk.Read(ref context.IP);
                            context.IP -= offset;
                            break;
                        }
                    case OP_RETURN:
                        Console.WriteLine($"return");
                        return true;
                    default:
                        // todo: throw runtime error
                        return false;
                }
            }
        }

        private GearsValue AreValuesEqual(GearsValue a, GearsValue b) {
            if (a.IsBool && b.IsBool) {
                return a.AsBool == b.AsBool;
            }
            else if (a.IsNil && b.IsNil) {
                return true;
            }
            else if (a.IsNumber && b.IsNumber) {
                return a.Equals(b);
            }
            return false;
        }

        /// <summary>
        /// Lox has a simple rule for boolean values: 'false' and 'nil' are falsey, and everything else is truthy.
        /// </summary>
        private bool IsFalsey(GearsValue value) {
            return value.IsNil || (value.IsBool && !value.AsBool);
        }

        // === Variables ============================================================================================
        // ===========================================================================================================

        private void GLOBAL_DEFINE(GearsChunk chunk, GearsContext context) {
            string name = chunk.GetConstantString(chunk.Read(ref context.IP));
            context.Globals.Set(name, context.Peek(0));
            context.Pop();
        }

        private void GLOBAL_GET(GearsChunk chunk, GearsContext context) {
            string name = chunk.GetConstantString(chunk.Read(ref context.IP));
            if (!context.Globals.TryGet(name, out GearsValue value)) {
                throw new RuntimeException(chunk.LineAt(context.IP - 1), $"Undefined variable '{name}'.");
            }
            context.Push(value);
        }

        private void GLOBAL_SET(GearsChunk chunk, GearsContext context) {
            string name = chunk.GetConstantString(chunk.Read(ref context.IP));
            if (!context.Globals.ContainsKey(name)) {
                throw new RuntimeException(chunk.LineAt(context.IP - 1), $"Undefined variable '{name}'.");
            }
            context.Globals.Set(name, context.Peek());
        }

        // === Operations ============================================================================================
        // ===========================================================================================================

        private void BINARY_GREATER(GearsChunk chunk, GearsContext context) {
            if (!context.Peek(0).IsNumber || !context.Peek(1).IsNumber) {
                throw new RuntimeException(chunk.LineAt(context.IP - 1), "Operands must be numbers.");
            }
            GearsValue b = context.Pop();
            GearsValue a = context.Pop();
            context.Push(a > b);
            // Console.WriteLine($"greater? {context.Peek()}");
        }

        private void BINARY_LESS(GearsChunk chunk, GearsContext context) {
            if (!context.Peek(0).IsNumber || !context.Peek(1).IsNumber) {
                throw new RuntimeException(chunk.LineAt(context.IP - 1), "Operands must be numbers.");
            }
            GearsValue b = context.Pop();
            GearsValue a = context.Pop();
            context.Push(a < b);
            // Console.WriteLine($"less? {context.Peek()}");
        }

        private void BINARY_ADD(GearsChunk chunk, GearsContext context) {
            if (!context.Peek(0).IsNumber || !context.Peek(1).IsNumber) {
                throw new RuntimeException(chunk.LineAt(context.IP - 1), "Operands must be numbers.");
            }
            GearsValue b = context.Pop();
            GearsValue a = context.Pop();
            context.Push(a + b);
            // Console.WriteLine($"add => {context.Peek()}");
        }

        private void BINARY_SUBTRACT(GearsChunk chunk, GearsContext context) {
            if (!context.Peek(0).IsNumber || !context.Peek(1).IsNumber) {
                throw new RuntimeException(chunk.LineAt(context.IP - 1), "Operands must be numbers.");
            }
            GearsValue b = context.Pop();
            GearsValue a = context.Pop();
            context.Push(a - b);
            // Console.WriteLine($"subtract => {context.Peek()}");
        }

        private void BINARY_MULTIPLY(GearsChunk chunk, GearsContext context) {
            if (!context.Peek(0).IsNumber || !context.Peek(1).IsNumber) {
                throw new RuntimeException(chunk.LineAt(context.IP - 1), "Operands must be numbers.");
            }
            GearsValue b = context.Pop();
            GearsValue a = context.Pop();
            context.Push(a * b);
            // Console.WriteLine($"multiply => {context.Peek()}");
        }

        private void BINARY_DIVIDE(GearsChunk chunk, GearsContext context) {
            if (!context.Peek(0).IsNumber || !context.Peek(1).IsNumber) {
                throw new RuntimeException(chunk.LineAt(context.IP - 1), "Operands must be numbers.");
            }
            GearsValue b = context.Pop();
            GearsValue a = context.Pop();
            context.Push(a / b);
            // Console.WriteLine($"divide => {context.Peek()}");
        }

        private void BINARY_NEGATE(GearsChunk chunk, GearsContext context) {
            if (!context.Peek(0).IsNumber) {
                throw new RuntimeException(chunk.LineAt(context.IP - 1), "Operand must be a number.");
            }
            context.Push(-context.Pop());
            // Console.WriteLine($"negate => {context.Peek()}");
        }

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
            int instruction = chunk.Read(ref offset);
            switch ((EGearsOpCode)instruction) {
                case OP_CONSTANT:
                    return DisassembleConstantInstruction("OP_CONSTANT", chunk, offset, false);
                case OP_STRING:
                    return DisassembleConstantInstruction("OP_STRING", chunk, offset, true);
                case OP_NIL:
                    return DisassembleSimpleInstruction("OP_NIL", chunk, offset);
                case OP_TRUE:
                    return DisassembleSimpleInstruction("OP_TRUE", chunk, offset);
                case OP_FALSE:
                    return DisassembleSimpleInstruction("OP_FALSE", chunk, offset);
                case OP_POP:
                    return DisassembleSimpleInstruction("OP_POP", chunk, offset);
                case OP_GET_LOCAL:
                    return DisassembleByteInstruction("OP_GET_LOCAL", chunk, offset);
                case OP_SET_LOCAL:
                    return DisassembleByteInstruction("OP_SET_LOCAL", chunk, offset);
                case OP_DEFINE_GLOBAL:
                    return DisassembleConstantInstruction("OP_DEF_GLOBAL", chunk, offset, true);
                case OP_GET_GLOBAL:
                    return DisassembleConstantInstruction("OP_GET_GLOBAL", chunk, offset, true);
                case OP_SET_GLOBAL:
                    return DisassembleConstantInstruction("OP_SET_GLOBAL", chunk, offset, true);
                case OP_EQUAL:
                    return DisassembleSimpleInstruction("OP_EQUAL", chunk, offset);
                case OP_GREATER:
                    return DisassembleSimpleInstruction("OP_GREATER", chunk, offset);
                case OP_LESS:
                    return DisassembleSimpleInstruction("OP_LESS", chunk, offset);
                case OP_ADD:
                    return DisassembleSimpleInstruction("OP_ADD", chunk, offset);
                case OP_SUBTRACT:
                    return DisassembleSimpleInstruction("OP_SUBTRACT", chunk, offset);
                case OP_MULTIPLY:
                    return DisassembleSimpleInstruction("OP_MULTIPLY", chunk, offset);
                case OP_DIVIDE:
                    return DisassembleSimpleInstruction("OP_DIVIDE", chunk, offset);
                case OP_NOT:
                    return DisassembleSimpleInstruction("OP_NOT", chunk, offset);
                case OP_NEGATE:
                    return DisassembleSimpleInstruction("OP_NEGATE", chunk, offset);
                case OP_PRINT:
                    return DisassembleSimpleInstruction("OP_PRINT", chunk, offset);
                case OP_JUMP:
                    return DisassembleTwoByteInstruction("OP_JUMP", chunk, offset);
                case OP_JUMP_IF_FALSE:
                    return DisassembleTwoByteInstruction("OP_JUMP_IF_FALSE", chunk, offset);
                case OP_LOOP:
                    return DisassembleTwoByteInstruction("OP_LOOP", chunk, offset);
                case OP_RETURN:
                    return DisassembleSimpleInstruction("OP_RETURN", chunk, offset);
                default:
                    Console.WriteLine($"Unknown opcode {instruction:X2}");
                    return offset;
            }
        }

        private int DisassembleSimpleInstruction(string name, GearsChunk chunk, int offset) {
            Console.WriteLine(name);
            return offset;
        }

        private int DisassembleByteInstruction(string name, GearsChunk chunk, int offset) {
            int index = chunk.Read(ref offset);
            Console.WriteLine($"{name} ({index})");
            return offset;
        }

        private int DisassembleTwoByteInstruction(string name, GearsChunk chunk, int offset) {
            int index = (chunk.Read(ref offset) << 8) + chunk.Read(ref offset);
            Console.WriteLine($"{name} ({index})");
            return offset;
        }

        private int DisassembleConstantInstruction(string name, GearsChunk chunk, int offset, bool asString) {
            int constantIndex = chunk.Read(ref offset);
            if (asString) {
                string value = chunk.GetConstantString(constantIndex);
                Console.WriteLine($"{name} #{constantIndex} ({value})");
            }
            else {
                GearsValue value = chunk.GetConstantValue(constantIndex);
                Console.WriteLine($"{name} #{constantIndex} ({value})");
            }
            return offset;
        }

        // === Error reporting =======================================================================================
        // ===========================================================================================================

        /// <summary>
        /// Throw this when vm encounters an error
        /// </summary>
        private class RuntimeException : Exception {
            private readonly int _Line;
            private readonly string _Message;

            internal RuntimeException(int line, string message) {
                _Line = line;
                _Message = message;
            }

            internal void Print() {
                Program.Error(_Line, _Message);
            }
        }

    }
}
