using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using static LoxScript.VirtualMachine.EGearsOpCode;

namespace LoxScript.VirtualMachine {
    /// <summary>
    /// Gears is a bytecode virtual machine for the Lox language.
    /// </summary>
    partial class Gears {

        private readonly static ulong InitString = Compiling.CompilerBitStr.GetBitStr("init");

        private GearsValue NativeFnClock(GearsValue[] args) {
            return (double)Stopwatch.GetTimestamp() / (Stopwatch.Frequency / 1000);
        }

        internal bool Run(GearsChunk chunk) {
            Reset(chunk);
            DefineNative("clock", 0, NativeFnClock);
            try {
                while (true) {
                    EGearsOpCode instruction = (EGearsOpCode)ReadByte();
                    switch (instruction) {
                        case OP_LOAD_CONSTANT:
                            Push(ReadConstant());
                            break;
                        case OP_LOAD_STRING:
                            Push(GearsValue.CreateObjPtr(HeapAddObject(new GearsObjString(ReadConstantString()))));
                            break;
                        case OP_LOAD_FUNCTION: {
                                int arity = ReadByte();
                                int address = ReadShort();
                                int upvalueCount = ReadByte();
                                GearsObjFunction closure = new GearsObjFunction(Chunk, arity, upvalueCount, address);
                                for (int i = 0; i < upvalueCount; i++) {
                                    bool isLocal = ReadByte() == 1;
                                    int index = ReadByte();
                                    if (isLocal) {
                                        int location = _OpenFrame.BP + index;
                                        closure.Upvalues[i] = CaptureUpvalue(location);
                                    }
                                    else {
                                        closure.Upvalues[i] = _OpenFrame.Function.Upvalues[index];
                                    }
                                }
                                Push(GearsValue.CreateObjPtr(HeapAddObject(closure)));
                            }
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
                                Push(StackGet(slot + _BP));
                            }
                            break;
                        case OP_SET_LOCAL: {
                                int slot = ReadShort();
                                StackSet(slot + _BP, Peek());
                            }
                            break;
                        case OP_GET_GLOBAL: {
                                ulong name = (ulong)ReadConstant();
                                if (!Globals.TryGet(name, out GearsValue value)) {
                                    throw new GearsRuntimeException(0, $"Undefined variable '{Compiling.CompilerBitStr.GetBitStr(name)}'.");
                                }
                                Push(value);
                            }
                            break;
                        case OP_DEFINE_GLOBAL: {
                                ulong name = (ulong)ReadConstant();
                                Globals.Set(name, Peek());
                                Pop();
                            }
                            break;
                        case OP_SET_GLOBAL: {
                                ulong name = (ulong)ReadConstant();
                                if (!Globals.ContainsKey(name)) {
                                    throw new GearsRuntimeException(0, $"Undefined variable '{Compiling.CompilerBitStr.GetBitStr(name)}'.");
                                }
                                Globals.Set(name, Peek());
                                break;
                            }
                        case OP_GET_UPVALUE: {
                                int slot = ReadShort();
                                GearsObjUpvalue upvalue = _OpenFrame.Function.Upvalues[slot];
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
                                GearsObjUpvalue upvalue = _OpenFrame.Function.Upvalues[slot];
                                if (upvalue.IsClosed) {
                                    upvalue.Value = Peek();
                                }
                                else {
                                    StackSet(upvalue.OriginalSP, Peek());
                                }
                            }
                            break;
                        case OP_GET_PROPERTY: {
                                GearsObjClassInstance instance = GetObjectFromPtr<GearsObjClassInstance>(Peek());
                                ulong name = (ulong)ReadConstant(); // property name
                                if (instance.Fields.TryGet(name, out GearsValue value)) {
                                    Pop(); // instance
                                    Push(value); // property value
                                    break;
                                }
                                if (!BindMethod(instance.Class, name)) {
                                    throw new GearsRuntimeException(0, $"Undefined property or method '{name}'.");
                                }
                            }
                            break;
                        case OP_SET_PROPERTY: {
                                GearsObjClassInstance instance = GetObjectFromPtr<GearsObjClassInstance>(Peek(1));
                                ulong name = (ulong)ReadConstant(); // property name
                                GearsValue value = Pop(); // value
                                instance.Fields.Set(name, value);
                                Pop(); // ptr
                                Push(value); // value
                            }
                            break;
                        case OP_GET_SUPER: {
                                ulong name = (ulong)ReadConstant(); // method/property name
                                GearsObjClass superclass = GetObjectFromPtr<GearsObjClass>(Pop());
                                if (!BindMethod(superclass, name)) {
                                    throw new GearsRuntimeException(0, $"Could not get {name} in superclass {superclass}");
                                }
                            }
                            break;
                        case OP_EQUAL:
                            Push(AreValuesEqual(Pop(), Pop()));
                            break;
                        case OP_GREATER: {
                                if (!Peek(0).IsNumber || !Peek(1).IsNumber) {
                                    throw new GearsRuntimeException(0, "Operands must be numbers.");
                                }
                                GearsValue b = Pop();
                                GearsValue a = Pop();
                                Push(a > b);
                            }
                            break;
                        case OP_LESS: {
                                if (!Peek(0).IsNumber || !Peek(1).IsNumber) {
                                    throw new GearsRuntimeException(0, "Operands must be numbers.");
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
                                else if (Peek(0).IsObjType(this, GearsObj.ObjType.ObjString) && Peek(1).IsObjType(this, GearsObj.ObjType.ObjString)) {
                                    string b = GetObjectFromPtr<GearsObjString>(Pop()).Value;
                                    string a = GetObjectFromPtr<GearsObjString>(Pop()).Value;
                                    Push(GearsValue.CreateObjPtr(HeapAddObject(new GearsObjString(a + b))));
                                }
                                else {
                                    throw new GearsRuntimeException(0, "Operands must be numbers or strings.");
                                }
                            }
                            break;
                        case OP_SUBTRACT: {
                                if (!Peek(0).IsNumber || !Peek(1).IsNumber) {
                                    throw new GearsRuntimeException(0, "Operands must be numbers.");
                                }
                                GearsValue b = Pop();
                                GearsValue a = Pop();
                                Push(a - b);
                            }
                            break;
                        case OP_MULTIPLY: {
                                if (!Peek(0).IsNumber || !Peek(1).IsNumber) {
                                    throw new GearsRuntimeException(0, "Operands must be numbers.");
                                }
                                GearsValue b = Pop();
                                GearsValue a = Pop();
                                Push(a * b);
                            }
                            break;
                        case OP_DIVIDE: {
                                if (!Peek(0).IsNumber || !Peek(1).IsNumber) {
                                    throw new GearsRuntimeException(0, "Operands must be numbers.");
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
                                    throw new GearsRuntimeException(0, "Operand must be a number.");
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
                                Call();
                            }
                            break;
                        case OP_INVOKE: {
                                CallInvoke();
                            }
                            break;
                        case OP_SUPER_INVOKE: {
                                CallInvokeSuper();
                            }
                            break;
                        case OP_CLOSE_UPVALUE:
                            CloseUpvalues(_SP - 1);
                            Pop();
                            break;
                        case OP_RETURN: {
                                GearsValue result = Pop();
                                CloseUpvalues(_OpenFrame.BP);
                                if (PopFrame()) {
                                    if (_SP != 0) {
                                        Console.WriteLine($"Report error: SP is '{_SP}', not '0'.");
                                    }
                                    return true;
                                }
                                Push(result);
                            }
                            break;
                        case OP_CLASS: {
                                Push(GearsValue.CreateObjPtr(HeapAddObject(new GearsObjClass(ReadConstantString()))));
                            }
                            break;
                        case OP_INHERIT: {
                                if (!Peek(0).IsObjType(this, GearsObj.ObjType.ObjClass)) {
                                    throw new GearsRuntimeException(0, "Superclass is not a class.");
                                }
                                GearsObjClass super = GetObjectFromPtr<GearsObjClass>(Peek(1));
                                GearsObjClass sub = GetObjectFromPtr<GearsObjClass>(Peek(0));
                                foreach (ulong key in super.Methods.AllKeys) {
                                    if (!super.Methods.TryGet(key, out GearsValue methodPtr)) {
                                        throw new GearsRuntimeException(0, "Could not copy superclass method table.");
                                    }
                                    sub.Methods.Set(key, methodPtr);
                                }
                                Pop(); // pop subclass
                            }
                            break;
                        case OP_METHOD: {
                                DefineMethod();
                            }
                            break;
                        default:
                            throw new GearsRuntimeException(0, $"Unknown opcode {instruction}");
                    }
                }
            }
            catch (GearsRuntimeException e) {
                Console.WriteLine(e.Message);
                return false;
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsFalsey(GearsValue value) {
            return value.IsNil || (value.IsBool && !value.AsBool);
        }

        private T GetObjectFromPtr<T>(GearsValue ptr) where T : GearsObj {
            if (!ptr.IsObjPtr) {
                throw new Exception($"GetObjectFromPtr: Value is not a pointer and cannot reference a {typeof(T).Name}.");
            }
            GearsObj obj = HeapGetObject(ptr.AsObjPtr);
            if (obj is T) {
                return obj as T;
            }
            throw new Exception($"GetObjectFromPtr: Object is not {typeof(T).Name}.");
        }

        // === Functions =============================================================================================
        // ===========================================================================================================

        private void DefineNative(string name, int arity, GearsFunctionNativeDelegate onInvoke) {
            Globals.Set(Compiling.CompilerBitStr.GetBitStr(name), GearsValue.CreateObjPtr(HeapAddObject(new GearsObjFunctionNative(name, arity, onInvoke))));
        }

        private void DefineMethod() {
            ulong methodName = (ulong)ReadConstant();
            GearsValue methodPtr = Peek();
            GearsObjFunction method = GetObjectFromPtr<GearsObjFunction>(methodPtr);
            GearsObjClass objClass = GetObjectFromPtr<GearsObjClass>(Peek(1));
            objClass.Methods.Set(methodName, methodPtr);
            Pop();
        }

        private bool BindMethod(GearsObjClass classObj, ulong name) {
            if (!classObj.Methods.TryGet(name, out GearsValue method)) {
                return false;
            }
            int objPtr = HeapAddObject(new GearsObjBoundMethod(Peek(), HeapGetObject(method.AsObjPtr) as GearsObjFunction));
            Pop();
            Push(GearsValue.CreateObjPtr(objPtr));
            return true;
        }

        // --- Can probably merge a ton of code from the three call methods ---

        private void CallInvoke() {
            int argCount = ReadByte();
            ulong methodName = (ulong)ReadConstant();
            GearsValue receiverPtr = Peek(argCount);
            if (!(receiverPtr.IsObjPtr) || !(receiverPtr.AsObject(this) is GearsObjClassInstance instance)) {
                throw new GearsRuntimeException(0, "Attempted invoke to non-pointer or non-method.");
            }
            if (instance.Fields.TryGet(methodName, out GearsValue value)) {
                // check fields first 28.5.1:
                if ((!value.IsObjPtr) || !(HeapGetObject(value.AsObjPtr) is GearsObjFunction function)) {
                    throw new GearsRuntimeException(0, $"Could not resolve method {methodName} in class {instance.Class}.");
                }
                if (function.Arity != argCount) {
                    throw new GearsRuntimeException(0, $"{function} expects {function.Arity} arguments but was passed {argCount}.");
                }
                int ip = function.IP;
                int bp = _SP - (function.Arity + 1);
                PushFrame(new GearsCallFrame(function, ip, bp));
            }
            else {
                InvokeFromClass(argCount, methodName, receiverPtr, instance.Class);
            }
        }

        private void InvokeFromClass(int argCount, ulong methodName, GearsValue receiverPtr, GearsObjClass objClass) {
            if (!objClass.Methods.TryGet(methodName, out GearsValue methodPtr)) {
                throw new GearsRuntimeException(0, $"{objClass} has no method with name '{Compiling.CompilerBitStr.GetBitStr(methodName)}'.");
            }
            if ((!methodPtr.IsObjPtr) || !(HeapGetObject(methodPtr.AsObjPtr) is GearsObjFunction method)) {
                throw new GearsRuntimeException(0, $"Could not resolve method '{Compiling.CompilerBitStr.GetBitStr(methodName)}' in class {objClass}.");
            }
            if (method.Arity != argCount) {
                throw new GearsRuntimeException(0, $"{method} expects {method.Arity} arguments but was passed {argCount}.");
            }
            int ip = method.IP;
            int bp = _SP - (method.Arity + 1);
            if (!receiverPtr.IsNil) {
                StackSet(bp, receiverPtr); // todo: this wipes out the method object. Is this bad?
            }
            PushFrame(new GearsCallFrame(method, ip, bp));
        }

        private void CallInvokeSuper() {
            int argCount = ReadByte();
            // next instruction will always be OP_GET_UPVALUE (for the super class). we include it here:
            if (!(ReadByte() == (int)OP_GET_UPVALUE)) {
                throw new GearsRuntimeException(0, "OP_SUPER_INVOKE must be followed by OP_GET_UPVALUE.");
            }
            int slot = ReadShort();
            GearsObjUpvalue upvalue = _OpenFrame.Function.Upvalues[slot];
            GearsObjClass superclass = GetObjectFromPtr<GearsObjClass>(upvalue.IsClosed ? upvalue.Value : StackGet(upvalue.OriginalSP));
            ulong methodName = (ulong)ReadConstant();
            InvokeFromClass(argCount, methodName, GearsValue.NilValue, superclass);
        }

        private void Call() {
            int argCount = ReadByte();
            GearsValue ptr = Peek(argCount);
            if (!ptr.IsObjPtr) {
                throw new GearsRuntimeException(0, "Attempted call to non-pointer.");
            }
            GearsObj obj = HeapGetObject(ptr.AsObjPtr);
            if (obj is GearsObjFunction function) {
                if (function.Arity != argCount) {
                    throw new GearsRuntimeException(0, $"{function} expects {function.Arity} arguments but was passed {argCount}.");
                }
                int ip = function.IP;
                int bp = _SP - (function.Arity + 1);
                PushFrame(new GearsCallFrame(function, ip, bp));
            }
            else if (obj is GearsObjFunctionNative native) {
                if (native.Arity != argCount) {
                    throw new GearsRuntimeException(0, $"{native} expects {native.Arity} arguments but was passed {argCount}.");
                }
                GearsValue[] args = new GearsValue[argCount];
                for (int i = argCount - 1; i >= 0; i++) {
                    args[i] = Pop();
                }
                Pop(); // pop the function signature
                Push(native.Invoke(args));
            }
            else if (obj is GearsObjBoundMethod method) {
                if (method.Method.Arity != argCount) {
                    throw new GearsRuntimeException(0, $"{method} expects {method.Method.Arity} arguments but was passed {argCount}.");
                }
                int ip = method.Method.IP;
                int bp = _SP - (method.Method.Arity + 1);
                StackSet(bp, method.Receiver); // todo: this wipes out the method object. Is this bad?
                PushFrame(new GearsCallFrame(method.Method, ip, bp));
            }
            else if (obj is GearsObjClass classObj) {
                StackSet(_SP - argCount - 1, GearsValue.CreateObjPtr(HeapAddObject(new GearsObjClassInstance(classObj))));
                if (classObj.Methods.TryGet(InitString, out GearsValue initPtr)) {
                    if (!initPtr.IsObjPtr) {
                        throw new GearsRuntimeException(0, "Attempted call to non-pointer.");
                    }
                    GearsObjFunction initFn = HeapGetObject(initPtr.AsObjPtr) as GearsObjFunction;
                    PushFrame(new GearsCallFrame(initFn, initFn.IP, _SP - argCount - 1));
                }
            }
            else {
                throw new GearsRuntimeException(0, $"Unhandled call to object {obj}");
            }
        }
        // --- end merge candidates ---

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
    }
}
