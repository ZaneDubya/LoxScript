using System;
using static LoxScript.VirtualMachine.EGearsOpCode;

namespace LoxScript.VirtualMachine {
    /// <summary>
    /// Gears is a bytecode virtual machine for the Lox language.
    /// </summary>
    class Gears : GearsContext {

        internal Gears() : base() {
        }

        private GearsValue NativeFnClock(GearsValue[] args) {
            return new GearsValue((double)DateTimeOffset.Now.ToUnixTimeMilliseconds());
        }

        private void DefineNative(string name, int arity, GearsNativeFunction onInvoke) {
            Globals.Set(name, GearsValue.CreateObjPtr(AddObject(new GearsObjNativeFunction(name, arity, onInvoke))));
        }

        internal bool Run(GearsObjFunction script) {
            Reset(script);
            DefineNative("clock", 0, NativeFnClock);
            while (true) {
                EGearsOpCode instruction = (EGearsOpCode)ReadByte();
                switch (instruction) {
                    case OP_CONSTANT:
                        Push(ReadConstant());
                        break;
                    case OP_STRING:
                        Push(GearsValue.CreateObjPtr(AddObject(new GearsObjString(ReadConstantString()))));
                        break;
                    case OP_FUNCTION:
                        Push(GearsValue.CreateObjPtr(AddObject(new GearsObjFunction(this))));
                        break;
                    case OP_NIL:
                        Push(GearsValue.NilValue);
                        break;
                    case OP_TRUE:
                        Push(GearsValue.TrueValue);
                        break;
                    case OP_FALSE:
                        Push(GearsValue.FalseValue);
                        break;
                    case OP_POP:
                        Pop();
                        break;
                    case OP_GET_LOCAL: {
                            int slot = ReadShort();
                            Push(StackGet(slot + BP));
                        }
                        break;
                    case OP_SET_LOCAL: {
                            int slot = ReadShort();
                            StackSet(slot + BP, Peek());
                        }
                        break;
                    case OP_GET_GLOBAL: {
                            string name = ReadConstantString();
                            if (!Globals.TryGet(name, out GearsValue value)) {
                                throw new RuntimeException(0, $"Undefined variable '{name}'.");
                            }
                            Push(value);
                        }
                        break;
                    case OP_DEFINE_GLOBAL: {
                            string name = ReadConstantString();
                            Globals.Set(name, Peek(0));
                            Pop();
                        }
                        break;
                    case OP_SET_GLOBAL: {
                            string name = ReadConstantString();
                            if (!Globals.ContainsKey(name)) {
                                throw new RuntimeException(0, $"Undefined variable '{name}'.");
                            }
                            Globals.Set(name, Peek());
                            break;
                        }
                    case OP_GET_UPVALUE: {
                            int slot = ReadShort();
                            GearsObjUpvalue upvalue = (_OpenFrame as GearsCallFrameClosure).Closure.Upvalues[slot];
                            if (upvalue.IsClosed) {
                                Push(upvalue.Value);
                            }
                            else {
                                Push(StackGet(upvalue.OriginalSP));
                            }
                        }
                        break;
                    case OP_SET_UPVALUE: {
                            int slot = ReadShort();
                            GearsObjUpvalue upvalue = (_OpenFrame as GearsCallFrameClosure).Closure.Upvalues[slot];
                            if (upvalue.IsClosed) {
                                upvalue.Value = Peek();
                            }
                            else {
                                StackSet(upvalue.OriginalSP, Peek());
                            }
                        }
                        break;
                    case OP_EQUAL:
                        Push(AreValuesEqual(Pop(), Pop()));
                        break;
                    case OP_GREATER: {
                            if (!Peek(0).IsNumber || !Peek(1).IsNumber) {
                                throw new RuntimeException(0, "Operands must be numbers.");
                            }
                            GearsValue b = Pop();
                            GearsValue a = Pop();
                            Push(a > b);
                        }
                        break;
                    case OP_LESS: {
                            if (!Peek(0).IsNumber || !Peek(1).IsNumber) {
                                throw new RuntimeException(0, "Operands must be numbers.");
                            }
                            GearsValue b = Pop();
                            GearsValue a = Pop();
                            Push(a < b);
                        }
                        break;
                    case OP_ADD: {
                            if (Peek(0).IsNumber && Peek(1).IsNumber) {
                                GearsValue b = Pop();
                                GearsValue a = Pop();
                                Push(a + b);
                            }
                            else {
                                throw new RuntimeException(0, "Operands must be numbers or strings.");
                            }
                        }
                        break;
                    case OP_SUBTRACT: {
                            if (!Peek(0).IsNumber || !Peek(1).IsNumber) {
                                throw new RuntimeException(0, "Operands must be numbers.");
                            }
                            GearsValue b = Pop();
                            GearsValue a = Pop();
                            Push(a - b);
                        }
                        break;
                    case OP_MULTIPLY: {
                            if (!Peek(0).IsNumber || !Peek(1).IsNumber) {
                                throw new RuntimeException(0, "Operands must be numbers.");
                            }
                            GearsValue b = Pop();
                            GearsValue a = Pop();
                            Push(a * b);
                        }
                        break;
                    case OP_DIVIDE: {
                            if (!Peek(0).IsNumber || !Peek(1).IsNumber) {
                                throw new RuntimeException(0, "Operands must be numbers.");
                            }
                            GearsValue b = Pop();
                            GearsValue a = Pop();
                            Push(a / b);
                        }
                        break;
                    case OP_NOT:
                        Push(IsFalsey(Pop()));
                        break;
                    case OP_NEGATE: {
                            if (!Peek(0).IsNumber) {
                                throw new RuntimeException(0, "Operand must be a number.");
                            }
                            Push(-Pop());
                        }
                        break;
                    case OP_PRINT:
                        Console.WriteLine(Pop().ToString(this));
                        break;
                    case OP_JUMP: {
                            int offset = ReadShort();
                            ModIP(offset);
                        }
                        break;
                    case OP_JUMP_IF_FALSE: {
                            int offset = ReadShort();
                            if (IsFalsey(Peek())) {
                                ModIP(offset);
                            }
                        }
                        break;
                    case OP_LOOP: {
                            int offset = ReadShort();
                            ModIP(-offset);
                        }
                        break;
                    case OP_CALL: {
                            int argCount = ReadByte();
                            GearsValue ptr = Peek(argCount);
                            if (!ptr.IsObjPtr) {
                                throw new RuntimeException(0, "Attempted call to non-pointer.");
                            }
                            GearsObj obj = GetObject(ptr.AsObjPtr);
                            if (obj is GearsObjFunction fn) { // this is not currently used - all fns currently wrapped in closures
                                CallFunction(fn, argCount);
                                break;
                            }
                            else if (obj is GearsObjClosure closure) {
                                CallClosure(closure, argCount);
                                break;
                            }
                            else if (obj is GearsObjNativeFunction native) {
                                CallNative(native, argCount);
                                break;
                            }
                        }
                        throw new RuntimeException(0, "Can only call functions and classes.");
                    case OP_CLOSURE: {
                            GearsValue ptr = Pop();
                            if (!ptr.IsObjPtr) {
                                throw new RuntimeException(0, "Attempted closure of non-pointer.");
                            }
                            GearsObj obj = GetObject(ptr.AsObjPtr);
                            if (obj.Type == GearsObj.ObjType.ObjFunction) {
                                int upvalueCount = ReadByte();
                                GearsObjClosure closure = new GearsObjClosure(obj as GearsObjFunction, upvalueCount);
                                for (int i = 0; i < upvalueCount; i++) {
                                    bool isLocal = ReadByte() == 1;
                                    int index = ReadByte();
                                    if (isLocal) {
                                        int location = _OpenFrame.BP + index;
                                        closure.Upvalues[i] = CaptureUpvalue(location);
                                    }
                                    else {
                                        closure.Upvalues[i] = (_OpenFrame as GearsCallFrameClosure).Closure.Upvalues[index];
                                    }
                                }
                                Push(GearsValue.CreateObjPtr(AddObject(closure)));
                                break;
                            }
                        }
                        throw new RuntimeException(0, "Can only make closures from functions.");
                    case OP_CLOSE_UPVALUE:
                        CloseUpvalues(SP - 1);
                        Pop();
                        break;
                    case OP_RETURN: {
                            GearsValue result = Pop();
                            CloseUpvalues(_OpenFrame.BP);
                            if (PopFrame()) {
                                if (SP != 0) {
                                    Console.WriteLine($"Report error: SP is '{SP}', not '0'.");
                                }
                                return true;
                            }
                            Push(result);
                        }
                        break;
                    default:
                        throw new RuntimeException(0, $"Unknown opcode {instruction}");
                }
            }
        }

        private void CallClosure(GearsObjClosure closure, int argCount) {
            if (closure.Function.Arity != argCount) {
                throw new RuntimeException(0, $"{closure.Function} expects {closure.Function.Arity} arguments but was passed {argCount}.");
            }
            int bp = SP - (closure.Function.Arity + 1);
            PushFrame(new GearsCallFrameClosure(closure, bp: bp));
        }

        private void CallFunction(GearsObjFunction fn, int argCount) {
            if (fn.Arity != argCount) {
                throw new RuntimeException(0, $"{fn} expects {fn.Arity} arguments but was passed {argCount}.");
            }
            int bp = SP - (fn.Arity + 1);
            PushFrame(new GearsCallFrame(fn, bp: bp));
        }

        private void CallNative(GearsObjNativeFunction fn, int argCount) {
            if (fn.Arity != argCount) {
                throw new RuntimeException(0, $"{fn} expects {fn.Arity} arguments but was passed {argCount}.");
            }
            GearsValue[] args = new GearsValue[argCount];
            for (int i = argCount - 1; i >= 0; i++) {
                args[i] = Pop();
            }
            Pop(); // pop the function signature
            Push(fn.Invoke(args));
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

        // === Closures ==============================================================================================
        // ===========================================================================================================
        
        private GearsObjUpvalue CaptureUpvalue(int sp) {
            GearsObjUpvalue previousUpvalue = null;
            GearsObjUpvalue currentUpValue = _OpenUpvalues;
            while (currentUpValue != null && currentUpValue.OriginalSP > sp) {
                previousUpvalue = currentUpValue;
                currentUpValue = currentUpValue.Next;
            }
            if (currentUpValue != null && currentUpValue.OriginalSP == sp) {
                return currentUpValue;
            }
            GearsObjUpvalue createdUpvalue = new GearsObjUpvalue(sp);
            createdUpvalue.Next = currentUpValue;
            if (previousUpvalue == null) {
                _OpenUpvalues = createdUpvalue;
            }
            else {
                previousUpvalue.Next = currentUpValue;
            }
            return createdUpvalue;
        }

        /// <summary>
        /// Starting with the passed stack slot, closes every open upvalue it can find that points to that slot or any
        /// slot above it on the stack. Close these upvalues, moving the values from the stack to the heap.
        /// </summary>
        private void CloseUpvalues(int sp) {
            // this function takes as a parameter an index of a stack slot. It closes every open upvalue it can find
            // pointing to that slot or any slot above it on the stack.
            // To do this, it walks the list of open upvalues, from top to bottom. If an upvalue's location points
            // into the range of slots we are closing, we close the upvalue. Otherwise, once we reach an upvalue
            // outside of that range, we know the rest will be too so we stop iterating.
            while (_OpenUpvalues != null && _OpenUpvalues.OriginalSP >= sp) {
                // To close an upvalue, we copy the variable' value into the closed field.
                GearsObjUpvalue upvalue = _OpenUpvalues;
                upvalue.Value = StackGet(upvalue.OriginalSP);
                upvalue.IsClosed = true;
                _OpenUpvalues = upvalue.Next;
            }
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
