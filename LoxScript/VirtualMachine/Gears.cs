using System;
using static LoxScript.VirtualMachine.EGearsOpCode;

namespace LoxScript.VirtualMachine {
    /// <summary>
    /// Gears is a bytecode virtual machine for the Lox language.
    /// </summary>
    class Gears {

        internal Gears() {
            DefineNative("clock", NativeFnClock);
        }

        private void DefineNative(string name, object nativeFnClock) {
            throw new NotImplementedException();
        }

        internal bool Run(GearsObjFunction fn) {
            GearsContext context = new GearsContext(fn);
            while(true) {
                EGearsOpCode instruction = (EGearsOpCode)context.ReadByte();
                switch (instruction) {
                    case OP_CONSTANT:
                        context.Push(context.ReadConstant());
                        break;
                    case OP_STRING:
                        context.Push(GearsValue.CreateObjPtr(context.AddObject(new GearsObjString(context.ReadConstantString()))));
                        break;
                    case OP_FUNCTION:
                        context.Push(GearsValue.CreateObjPtr(context.AddObject(new GearsObjFunction(context))));
                        break;
                    case OP_NIL:
                        context.Push(GearsValue.NilValue);
                        break;
                    case OP_TRUE:
                        context.Push(GearsValue.TrueValue);
                        break;
                    case OP_FALSE:
                        context.Push(GearsValue.FalseValue);
                        break;
                    case OP_POP:
                        context.Pop();
                        break;
                    case OP_GET_LOCAL: {
                            int slot = context.ReadShort();
                            context.Push(context.StackGet(slot + context.Frame.BP));
                        }
                        break;
                    case OP_SET_LOCAL: {
                            int slot = context.ReadShort();
                            context.StackSet(slot + context.Frame.BP, context.Peek());
                        }
                        break;
                    case OP_GET_GLOBAL: {
                            string name = context.ReadConstantString();
                            if (!context.Globals.TryGet(name, out GearsValue value)) {
                                throw new RuntimeException(context.LineAtLast(), $"Undefined variable '{name}'.");
                            }
                            context.Push(value);
                        }
                        break;
                    case OP_DEFINE_GLOBAL: {
                            string name = context.ReadConstantString();
                            context.Globals.Set(name, context.Peek(0));
                            context.Pop();
                        }
                        break;
                    case OP_SET_GLOBAL: {
                            string name = context.ReadConstantString();
                            if (!context.Globals.ContainsKey(name)) {
                                throw new RuntimeException(context.LineAtLast(), $"Undefined variable '{name}'.");
                            }
                            context.Globals.Set(name, context.Peek());
                            break;
                        }
                    case OP_EQUAL:
                        context.Push(AreValuesEqual(context.Pop(), context.Pop()));
                        break;
                    case OP_GREATER: {
                            if (!context.Peek(0).IsNumber || !context.Peek(1).IsNumber) {
                                throw new RuntimeException(context.LineAtLast(), "Operands must be numbers.");
                            }
                            GearsValue b = context.Pop();
                            GearsValue a = context.Pop();
                            context.Push(a > b);
                        }
                        break;
                    case OP_LESS: {
                            if (!context.Peek(0).IsNumber || !context.Peek(1).IsNumber) {
                                throw new RuntimeException(context.LineAtLast(), "Operands must be numbers.");
                            }
                            GearsValue b = context.Pop();
                            GearsValue a = context.Pop();
                            context.Push(a < b);
                        }
                        break;
                    case OP_ADD: {
                            if (!context.Peek(0).IsNumber || !context.Peek(1).IsNumber) {
                                throw new RuntimeException(context.LineAtLast(), "Operands must be numbers.");
                            }
                            GearsValue b = context.Pop();
                            GearsValue a = context.Pop();
                            context.Push(a + b);
                        }
                        break;
                    case OP_SUBTRACT: {
                            if (!context.Peek(0).IsNumber || !context.Peek(1).IsNumber) {
                                throw new RuntimeException(context.LineAtLast(), "Operands must be numbers.");
                            }
                            GearsValue b = context.Pop();
                            GearsValue a = context.Pop();
                            context.Push(a - b);
                        }
                        break;
                    case OP_MULTIPLY: {
                            if (!context.Peek(0).IsNumber || !context.Peek(1).IsNumber) {
                                throw new RuntimeException(context.LineAtLast(), "Operands must be numbers.");
                            }
                            GearsValue b = context.Pop();
                            GearsValue a = context.Pop();
                            context.Push(a * b);
                        }
                        break;
                    case OP_DIVIDE: {
                            if (!context.Peek(0).IsNumber || !context.Peek(1).IsNumber) {
                                throw new RuntimeException(context.LineAtLast(), "Operands must be numbers.");
                            }
                            GearsValue b = context.Pop();
                            GearsValue a = context.Pop();
                            context.Push(a / b);
                        }
                        break;
                    case OP_NOT:
                        context.Push(IsFalsey(context.Pop()));
                        break;
                    case OP_NEGATE: {
                            if (!context.Peek(0).IsNumber) {
                                throw new RuntimeException(context.LineAtLast(), "Operand must be a number.");
                            }
                            context.Push(-context.Pop());
                        }
                        break;
                    case OP_PRINT:
                        Console.WriteLine(context.Pop().ToString(context));
                        break;
                    case OP_JUMP: {
                            int offset = context.ReadShort();
                            context.ModIP(offset);
                        }
                        break;
                    case OP_JUMP_IF_FALSE: {
                            int offset = context.ReadShort();
                            if (IsFalsey(context.Peek())) {
                                context.ModIP(offset);
                            }
                        }
                        break;
                    case OP_LOOP: {
                            int offset = context.ReadShort();
                            context.ModIP(-offset);
                        }
                        break;
                    case OP_CALL: {
                            int argCount = context.ReadByte();
                            GearsValue callee = context.Peek(argCount);
                            if (callee.IsObjPtr) {
                                if (callee.IsObjType(context, GearsObj.ObjType.ObjFunction)) {
                                    Call(context, context.GetObject(callee.AsObjPtr) as GearsObjFunction, argCount);
                                    break;
                                }
                                else if (callee.IsObjType(context, GearsObj.ObjType.ObjNative)) {
                                    CallNative(context, context.GetObject(callee.AsObjPtr) as GearsObjNativeFunction, argCount);
                                }
                            }
                        }
                        throw new RuntimeException(context.LineAtLast(), "Can only call functions and classes.");
                    case OP_RETURN: {
                            GearsValue result = context.Pop();
                            if (context.PopFrame()) {
                                if (context.SP != 0) {
                                    Console.WriteLine($"Report error: SP is '{context.SP}', not '0'.");
                                }
                                return true;
                            }
                            context.Push(result);
                        }
                        break;
                    default:
                        throw new RuntimeException(0, $"Unknown opcode 0x{instruction:X2}");
                }
            }
        }

        private void Call(GearsContext context, GearsObjFunction fn, int argCount) {
            if (fn.Arity != argCount) {
                throw new RuntimeException(0, $"{fn} expects {fn.Arity} arguments but was passed {argCount}.");
            }
            context.PushFrame(new GearsCallFrame(fn, bp: context.SP - (fn.Arity + 1)));
        }

        private void CallNative(GearsContext context, GearsObjNativeFunction fn, int argCount) {
            string name = context.ReadConstantString();
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
                    return DisassembleConstantInstruction("OP_CONSTANT", chunk, offset, OP_CONSTANT);
                case OP_STRING:
                    return DisassembleConstantInstruction("OP_STRING", chunk, offset, OP_STRING);
                case OP_FUNCTION:
                    return DisassembleConstantInstruction("OP_FUNCTION", chunk, offset, OP_FUNCTION);
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
                    return DisassembleConstantInstruction("OP_DEF_GLOBAL", chunk, offset, OP_STRING);
                case OP_GET_GLOBAL:
                    return DisassembleConstantInstruction("OP_GET_GLOBAL", chunk, offset, OP_STRING);
                case OP_SET_GLOBAL:
                    return DisassembleConstantInstruction("OP_SET_GLOBAL", chunk, offset, OP_STRING);
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
                case OP_CALL:
                    return DisassembleByteInstruction("OP_CALL", chunk, offset);
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

        private int DisassembleConstantInstruction(string name, GearsChunk chunk, int offset, EGearsOpCode constantType) {
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

        // === Error reporting =======================================================================================
        // ===========================================================================================================

        /// <summary>
        /// Throw this when vm encounters an error
        /// </summary>
        public class RuntimeException : Exception {
            private readonly int _Line;
            private readonly string _Message;

            internal RuntimeException(int line, string message) {
                _Line = line;
                _Message = message;
            }

            internal void Print() {
                // todo: print stack trace 24.5.2
                Program.Error(_Line, _Message);
            }
        }

    }
}
