#if NET_4_5
using System.Runtime.CompilerServices;
#endif
using System;
using static XPT.Core.Scripting.LoxScript.VirtualMachine.EGearsOpCode;

namespace XPT.Core.Scripting.LoxScript.VirtualMachine {
    /// <summary>
    /// Gears is a virtual machine for the Lox language, which has been compiled to byte code.
    /// </summary>
    internal partial class Gears { // run

        internal bool IsRunning => _IP < Chunk.SizeCode;

        internal void Run() {
            if (IsRunning) {
                try {
                    if (RunOne()) {
                        // completed and over.
                    }
                    // gave up control for cooperative multitasking purposes.
                }
                catch {
                    throw;
                }
            }
        }

        /// <summary>
        /// Runs the script from top to bottom.
        /// </summary>
        private bool RunOne() {
            LastReturnValue = GearsValue.NilValue;
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
                            string name = ReadConstantVarName();
                            if (!Globals.TryGet(name, out GearsValue value)) {
                                throw new GearsRuntimeException(Chunk.LineAt(_IP), $"Undefined variable '{name}'.");
                            }
                            Push(value);
                        }
                        break;
                    case OP_DEFINE_GLOBAL: {
                            string name = ReadConstantVarName();
                            Globals.Set(name, Peek());
                            Pop();
                        }
                        break;
                    case OP_SET_GLOBAL: {
                            string name = ReadConstantVarName();
                            if (!Globals.ContainsKey(name)) {
                                throw new GearsRuntimeException(Chunk.LineAt(_IP), $"Undefined variable '{name}'.");
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
                            GearsObjInstance instance = GetObjectFromPtr<GearsObjInstance>(Peek());
                            string name = ReadConstantVarName(); // property name
                            if (instance.TryGetField(name, out GearsValue value)) {
                                Pop(); // instance
                                Push(value); // property value
                                break;
                            }
                            if (instance is GearsObjInstanceLox loxInstance && BindLoxMethod(loxInstance.Class, name)) {
                                break;
                            }
                            throw new GearsRuntimeException(Chunk.LineAt(_IP), $"Undefined property or method '{name}'.");
                        }
                    case OP_SET_PROPERTY: {
                            GearsObjInstance instance = GetObjectFromPtr<GearsObjInstance>(Peek(1));
                            string name = ReadConstantVarName(); // property name
                            GearsValue value = Pop(); // value
                            instance.SetField(name, value);
                            Pop(); // ptr
                            Push(value); // value
                        }
                        break;
                    case OP_GET_SUPER: {
                            string name = ReadConstantVarName(); // method/property name
                            GearsObjClass superclass = GetObjectFromPtr<GearsObjClass>(Pop());
                            if (!BindLoxMethod(superclass, name)) {
                                throw new GearsRuntimeException(Chunk.LineAt(_IP), $"Could not get {name} in superclass {superclass}");
                            }
                        }
                        break;
                    case OP_EQUAL:
                        Push(AreValuesEqual(Pop(), Pop()));
                        break;
                    case OP_GREATER: {
                            if (!Peek(0).IsNumber || !Peek(1).IsNumber) {
                                throw new GearsRuntimeException(Chunk.LineAt(_IP), "Operands must be numbers.");
                            }
                            GearsValue b = Pop();
                            GearsValue a = Pop();
                            Push(a > b);
                        }
                        break;
                    case OP_LESS: {
                            if (!Peek(0).IsNumber || !Peek(1).IsNumber) {
                                throw new GearsRuntimeException(Chunk.LineAt(_IP), "Operands must be numbers.");
                            }
                            GearsValue b = Pop();
                            GearsValue a = Pop();
                            Push(a < b);
                        }
                        break;
                    case OP_BITWISE_AND: {
                            if (!Peek(0).IsNumber || !Peek(1).IsNumber) {
                                throw new GearsRuntimeException(Chunk.LineAt(_IP), "Operands of bitwise operators must be numbers.");
                            }
                            GearsValue b = Pop();
                            GearsValue a = Pop();
                            Push((int)a & (int)b);
                        }
                        break;
                    case OP_BITWISE_OR: {
                            if (!Peek(0).IsNumber || !Peek(1).IsNumber) {
                                throw new GearsRuntimeException(Chunk.LineAt(_IP), "Operands of bitwise operators must be numbers.");
                            }
                            GearsValue b = Pop();
                            GearsValue a = Pop();
                            Push(((int)a) | ((int)b));
                        }
                        break;
                    case OP_ADD: {
                            GearsValue b = Pop();
                            GearsValue a = Pop();
                            if (a.IsNumber && b.IsNumber) {
                                Push(a + b);
                            }
                            else if (a.IsObjType<GearsObjString>(this) && b.IsObjType<GearsObjString>(this)) {
                                string sa = GetObjectFromPtr<GearsObjString>(a).Value;
                                string sb = GetObjectFromPtr<GearsObjString>(b).Value;
                                Push(GearsValue.CreateObjPtr(HeapAddObject(new GearsObjString(sa + sb))));
                            }
                            else if (a.IsNumber && b.IsObjType<GearsObjString>(this)) {
                                string sa = ((int)a).ToString();
                                string sb = GetObjectFromPtr<GearsObjString>(b).Value;
                                Push(GearsValue.CreateObjPtr(HeapAddObject(new GearsObjString(sa + sb))));
                            }
                            else if (a.IsObjType<GearsObjString>(this) && b.IsNumber) {
                                string sa = GetObjectFromPtr<GearsObjString>(a).Value;
                                string sb = ((int)b).ToString();
                                Push(GearsValue.CreateObjPtr(HeapAddObject(new GearsObjString(sa + sb))));
                            }
                            else {
                                throw new GearsRuntimeException(Chunk.LineAt(_IP), "Operands of add must be numbers or strings.");
                            }
                        }
                        break;
                    case OP_SUBTRACT: {
                            if (!Peek(0).IsNumber || !Peek(1).IsNumber) {
                                throw new GearsRuntimeException(Chunk.LineAt(_IP), "Operands of subtract must be numbers.");
                            }
                            GearsValue b = Pop();
                            GearsValue a = Pop();
                            Push(a - b);
                        }
                        break;
                    case OP_MULTIPLY: {
                            if (!Peek(0).IsNumber || !Peek(1).IsNumber) {
                                throw new GearsRuntimeException(Chunk.LineAt(_IP), "Operands of multiply must be numbers.");
                            }
                            GearsValue b = Pop();
                            GearsValue a = Pop();
                            Push(a * b);
                        }
                        break;
                    case OP_DIVIDE: {
                            if (!Peek(0).IsNumber || !Peek(1).IsNumber) {
                                throw new GearsRuntimeException(Chunk.LineAt(_IP), "Operands of divide must be numbers.");
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
                                throw new GearsRuntimeException(Chunk.LineAt(_IP), "Operand of negate must be a number.");
                            }
                            Push(-Pop());
                        }
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
                            Call(argCount: ReadByte());
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
                                    throw new GearsRuntimeException(Chunk.LineAt(_IP), $"Error after final return: SP is '{_SP}', not '0'.");
                                }
                                LastReturnValue = result;
                                _IP = Chunk.SizeCode; // code is complete and no longer running.
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
                            if (!Peek(0).IsObjType<GearsObjClass>(this)) {
                                throw new GearsRuntimeException(Chunk.LineAt(_IP), "Superclass is not a class.");
                            }
                            GearsObjClass super = GetObjectFromPtr<GearsObjClass>(Peek(1));
                            GearsObjClass sub = GetObjectFromPtr<GearsObjClass>(Peek(0));
                            foreach (string key in super.Methods.AllKeys) {
                                if (!super.Methods.TryGet(key, out GearsValue methodPtr)) {
                                    throw new GearsRuntimeException(Chunk.LineAt(_IP), "Could not copy superclass method table.");
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
                        throw new GearsRuntimeException(Chunk.LineAt(_IP), $"Unknown opcode {instruction}");
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
#if NET_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
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

        private void DefineMethod() {
            string methodName = ReadConstantVarName();
            GearsValue methodPtr = Peek();
            // GearsObjFunction method = GetObjectFromPtr<GearsObjFunction>(methodPtr);
            GearsObjClass objClass = GetObjectFromPtr<GearsObjClass>(Peek(1));
            objClass.Methods.Set(methodName, methodPtr);
            Pop();
        }

        private bool BindLoxMethod(GearsObjClass classObj, string name) {
            if (!classObj.Methods.TryGet(name, out GearsValue method)) {
                return false;
            }
            int objPtr = HeapAddObject(new GearsObjBoundMethod(Peek(), HeapGetObject(method.AsObjPtr) as GearsObjFunction));
            Pop();
            Push(GearsValue.CreateObjPtr(objPtr));
            return true;
        }

        private void InvokeFromClass(int argCount, string methodName, GearsValue receiverPtr, GearsObjClass objClass) {
            if (!objClass.Methods.TryGet(methodName, out GearsValue methodPtr)) {
                throw new GearsRuntimeException(Chunk.LineAt(_IP), $"{objClass} has no method with name '{methodName}'.");
            }
            if ((!methodPtr.IsObjPtr) || !(HeapGetObject(methodPtr.AsObjPtr) is GearsObjFunction method)) {
                throw new GearsRuntimeException(Chunk.LineAt(_IP), $"Could not resolve method '{methodName}' in class {objClass}.");
            }
            if (method.Arity != argCount) {
                throw new GearsRuntimeException(Chunk.LineAt(_IP), $"{method} expects {method.Arity} arguments but was passed {argCount}.");
            }
            int ip = method.IP;
            int bp = _SP - (method.Arity + 1);
            if (!receiverPtr.IsNil) {
                StackSet(bp, receiverPtr); // todo: this wipes out the method object. Is this bad?
            }
            PushFrame(new GearsCallFrame(method, ip, bp));
        }

        // --- Can probably merge a ton of code from the three call methods ---

        private void CallInvoke() {
            int argCount = ReadByte();
            string methodName = ReadConstantVarName();
            GearsValue receiverPtr = Peek(argCount);
            if (!receiverPtr.IsObjPtr) {
                throw new GearsRuntimeException(Chunk.LineAt(_IP), "Attempted invoke to non-pointer.");
            }
            GearsObj obj = receiverPtr.AsObject(this);
            if (obj is GearsObjInstance instance) {
                if (instance.TryGetField(methodName, out GearsValue value)) {
                    if (!value.IsObjPtr) {
                        throw new GearsRuntimeException(Chunk.LineAt(_IP), "Attempted call to non-pointer.");
                    }
                    GearsObj objFn = HeapGetObject(value.AsObjPtr);
                    if (objFn is GearsObjFunction function) {
                        if (function.Arity != argCount) {
                            throw new GearsRuntimeException(Chunk.LineAt(_IP), $"{function} expects {function.Arity} arguments but was passed {argCount}.");
                        }
                        int ip = function.IP;
                        int bp = _SP - (function.Arity + 1);
                        PushFrame(new GearsCallFrame(function, ip, bp));
                    }
                    else if (objFn is GearsObjFunctionNative native) {
                        if (native.Arity != argCount) {
                            throw new GearsRuntimeException(Chunk.LineAt(_IP), $"{native} expects {native.Arity} arguments but was passed {argCount}.");
                        }
                        GearsValue[] args = new GearsValue[argCount];
                        for (int i = argCount - 1; i >= 0; i--) {
                            args[i] = Pop();
                        }
                        Pop(); // pop the function signature
                        Push(native.Invoke(args));
                    }
                    else {
                        throw new GearsRuntimeException(Chunk.LineAt(_IP), $"Could not resolve method {methodName} in {instance}.");
                    }
                }
                else if (instance is GearsObjInstanceLox instanceLox) {
                    InvokeFromClass(argCount, methodName, receiverPtr, instanceLox.Class);
                }
                else {
                    throw new GearsRuntimeException(Chunk.LineAt(_IP), $"{instance} does not have a public method named '{methodName}'.");
                }
                return;
            }
            throw new GearsRuntimeException(Chunk.LineAt(_IP), "Attempted invoke to non-instance.");
        }

        private void CallInvokeSuper() {
            int argCount = ReadByte();
            // next instruction will always be OP_GET_UPVALUE (for the super class). we include it here:
            if (!(ReadByte() == (int)OP_GET_UPVALUE)) {
                throw new GearsRuntimeException(Chunk.LineAt(_IP), "OP_SUPER_INVOKE must be followed by OP_GET_UPVALUE.");
            }
            int slot = ReadShort();
            GearsObjUpvalue upvalue = _OpenFrame.Function.Upvalues[slot];
            GearsObjClass superclass = GetObjectFromPtr<GearsObjClass>(upvalue.IsClosed ? upvalue.Value : StackGet(upvalue.OriginalSP));
            string methodName = ReadConstantVarName();
            InvokeFromClass(argCount, methodName, GearsValue.NilValue, superclass);
        }

        private void Call(int argCount) {
            GearsValue ptr = Peek(argCount);
            if (!ptr.IsObjPtr) {
                throw new GearsRuntimeException(Chunk.LineAt(_IP), "Attempted call to non-pointer.");
            }
            GearsObj obj = HeapGetObject(ptr.AsObjPtr);
            if (obj is GearsObjFunction function) {
                if (function.Arity != argCount) {
                    throw new GearsRuntimeException(Chunk.LineAt(_IP), $"{function} expects {function.Arity} arguments but was passed {argCount}.");
                }
                int ip = function.IP;
                int bp = _SP - (function.Arity + 1);
                PushFrame(new GearsCallFrame(function, ip, bp));
            }
            else if (obj is GearsObjFunctionNative native) {
                if (native.Arity != argCount) {
                    throw new GearsRuntimeException(Chunk.LineAt(_IP), $"{native} expects {native.Arity} arguments but was passed {argCount}.");
                }
                GearsValue[] args = new GearsValue[argCount];
                for (int i = argCount - 1; i >= 0; i--) {
                    args[i] = Pop();
                }
                Pop(); // pop the function signature
                Push(native.Invoke(args));
            }
            else if (obj is GearsObjBoundMethod method) {
                if (method.Method.Arity != argCount) {
                    throw new GearsRuntimeException(Chunk.LineAt(_IP), $"{method} expects {method.Method.Arity} arguments but was passed {argCount}.");
                }
                int ip = method.Method.IP;
                int bp = _SP - (method.Method.Arity + 1);
                StackSet(bp, method.Receiver); // todo: this wipes out the method object. Is this bad?
                PushFrame(new GearsCallFrame(method.Method, ip, bp));
            }
            else if (obj is GearsObjClass classObj) {
                StackSet(_SP - argCount - 1, GearsValue.CreateObjPtr(HeapAddObject(new GearsObjInstanceLox(classObj))));
                if (classObj.Methods.TryGet(InitString, out GearsValue initPtr)) {
                    if (!initPtr.IsObjPtr) {
                        throw new GearsRuntimeException(Chunk.LineAt(_IP), "Attempted call to non-pointer.");
                    }
                    GearsObjFunction initFn = HeapGetObject(initPtr.AsObjPtr) as GearsObjFunction;
                    PushFrame(new GearsCallFrame(initFn, initFn.IP, _SP - argCount - 1));
                }
            }
            else {
                throw new GearsRuntimeException(Chunk.LineAt(_IP), $"Unhandled call to object {obj}");
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
