// #define DEBUG_LOG_GC

using System;
using System.Diagnostics;
#if NET_4_5
using System.Runtime.CompilerServices;
#endif

namespace XPT.Core.Scripting.LoxScript.VirtualMachine {
    /// <summary>
    /// The state of a running gears instance.
    /// It represents the Call frame stack, the value Stack, and the Heap.
    /// </summary>
    internal partial class Gears { // frame, heap, state management

        private static readonly long InitString = BitString.GetBitStr("init");
        private const int FRAMES_MAX = 32;
        private const int HEAP_MAX = 256;
        private const int STACK_MAX = 256;

        /// <summary>
        /// Reset the VM. Heap, globals, and stack are cleared.
        /// Then the VM is run once, unless firstRun == false
        /// </summary>
        internal Gears Reset(GearsChunk chunk, bool firstRun) {
            Chunk = chunk;
            _SP = 0;
            for (int i = 0; i < _Stack.Length; i++) {
                _Stack[i] = 0;
            }
            _FrameCount = 0;
            for (int i = 0; i < _Frames.Length; i++) {
                _Frames[i] = null;
            }
            for (int i = 0; i < _Heap.Length; i++) {
                _Heap[i] = null;
            }
            Globals.Reset();
            _GrayList.Clear();
            GearsObjFunction closure = new GearsObjFunction(Chunk, 0, 0, 0);
            PushFrame(new GearsCallFrame(closure));
            Push(GearsValue.CreateObjPtr(HeapAddObject(closure)));
            AddNativeFunctionToGlobals("clock", 0, NativeFnClock);
            AddNativeFunctionToGlobals("print", 1, NativeFnPrint);
            if (firstRun) {
                Run();
            }
            return this;
        }

        public override string ToString() => $"{_OpenFrame.Function}@{_IP}";

        // === Includes ============================================================================================
        // =========================================================================================================

        private GearsValue NativeFnClock(GearsValue[] args) {
            return (long)(Stopwatch.GetTimestamp() / ((long)Stopwatch.Frequency / 1000));
        }

        private GearsValue NativeFnPrint(GearsValue[] args) {
            Console.WriteLine(args[0].ToString(this));
            return GearsValue.NilValue;
        }

        /// <summary>
        /// Defines a function that can be called by scripts.
        /// Arity is the number of arguments expected.
        /// </summary>
        internal void AddNativeFunctionToGlobals(string name, int arity, GearsFunctionNativeDelegate onInvoke) {
            Globals.Set(BitString.GetBitStr(name), GearsValue.CreateObjPtr(HeapAddObject(new GearsObjFunctionNative(name, arity, onInvoke))));
        }

        internal void AddNativeObjectToGlobals(string name, object obj) {
            Globals.Set(BitString.GetBitStr(name), GearsValue.CreateObjPtr(HeapAddObject(new GearsObjInstanceNative(this, obj))));
        }

        // === Call frames ==========================================================================================
        // === This should be part of the stack! See todo.md ========================================================
        // === Dear lord this is so inefficient! ====================================================================
        // ==========================================================================================================

        internal void PushFrame(GearsCallFrame frame) {
            if (_FrameCount == FRAMES_MAX) {
                throw new GearsRuntimeException(Chunk.LineAt(_IP), "Stack frame overflow.");
            }
            if (_FrameCount > 0) {
                SaveFrameVars(_Frames[_FrameCount - 1]);
            }
            _Frames[_FrameCount++] = frame;
            LoadFrameVars(frame);
        }

        /// <summary>
        /// Returns true if this was the last frame and the script has ended.
        /// </summary>
        internal bool PopFrame() {
            _SP = _BP;
            _FrameCount -= 1;
            if (_FrameCount < 0) {
                throw new GearsRuntimeException(Chunk.LineAt(_IP), "Stack frame underflow.");
            }
            if (_FrameCount > 0) {
                LoadFrameVars(_Frames[_FrameCount - 1]);
                return false;
            }
            return true;
        }

        private void SaveFrameVars(GearsCallFrame frame) {
            frame.BP = _BP;
            frame.IP = _IP;
        }

        private void LoadFrameVars(GearsCallFrame frame) {
            Chunk = frame.Function.Chunk;
            _Code = Chunk._Code;
            _IP = frame.IP;
            _BP = frame.BP;
        }

        internal void ModIP(int value) {
            _IP += value;
        }

#if NET_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        internal int ReadByte() {
            return _Code[_IP++];
        }

#if NET_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        internal int ReadShort() {
            return (_Code[_IP++] << 8) | _Code[_IP++];
        }

#if NET_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        internal GearsValue ReadConstant() {
            int index = ReadShort();
            return Chunk.ReadConstantValue(index);
        }

#if NET_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        internal string ReadConstantString() {
            int index = ReadShort();
            return Chunk.Strings.ReadStringConstant(index);
        }

        // === Stack ================================================================================================
        // ==========================================================================================================

#if NET_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        internal GearsValue StackGet(int index) {
            if (index < 0 || index >= _SP) {
                throw new GearsRuntimeException(Chunk.LineAt(_IP), "Stack exception");
            }
            return _Stack[index];
        }

#if NET_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        internal void StackSet(int index, GearsValue value) {
            if (index < 0 || index >= _SP) {
                throw new GearsRuntimeException(Chunk.LineAt(_IP), "Stack exception");
            }
            _Stack[index] = value;
        }

#if NET_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        internal void Push(GearsValue value) {
            if (_SP >= STACK_MAX) {
                throw new GearsRuntimeException(Chunk.LineAt(_IP), "Stack exception");
            }
            _Stack[_SP++] = value;
        }

#if NET_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        internal GearsValue Pop() {
            if (_SP == 0) {
                throw new GearsRuntimeException(Chunk.LineAt(_IP), "Stack exception");
            }
            return _Stack[--_SP];
        }
        
#if NET_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        internal GearsValue Peek(int offset = 0) {
            int index = _SP - 1 - offset;
            if (index < 0 || index >= _SP) {
                throw new GearsRuntimeException(Chunk.LineAt(_IP), "Stack exception");
            }
            return _Stack[index];
        }

        // === Heap ==================================================================================================
        // ===========================================================================================================

        internal int HeapAddObject(GearsObj obj, bool allowGC = true) {
            for (int i = 0; i < _Heap.Length; i++) {
                if (_Heap[i] == null) {
                    _Heap[i] = obj;
#if DEBUG_LOG_GC
                    Console.WriteLine($"Allocate {obj.Type} at {i}");
#endif
                    return i;
                }
            }
            if (allowGC) {
                CollectGarbage();
                int newIndex = HeapAddObject(obj, false);
                if (newIndex != -1) {
                    return newIndex;
                }
            }
            throw new GearsRuntimeException(Chunk.LineAt(_IP), "Out of heap space.");
        }

        internal GearsObj HeapGetObject(int index) {
            if (index < 0 || index >= _Heap.Length || _Heap[index] == null) {
                return null; // todo, throw runtime exception, null object
            }
            return _Heap[index];
        }

        internal void HeapFreeObject(int index) {
            if (_Heap[index] != null) {
#if DEBUG_LOG_GC
                Console.WriteLine($"Free {_Heap[index].Type} at {index}");
#endif
                _Heap[index] = null;
            }
        }

        // === Garbage Collection ====================================================================================
        // ===========================================================================================================

        private void CollectGarbage() {
#if DEBUG_LOG_GC
            Console.WriteLine("-- gc begin");
#endif
            MarkRoots();
            TraceReferences();
            Sweep();
#if DEBUG_LOG_GC
            Console.WriteLine("-- gc end");
#endif
        }

        private void MarkRoots() {
            for (int i = 0; i < _SP; i++) {
                MarkValue(_Stack[i]);
            }
            MarkTable(Globals);
            for (int i = 0; i < _FrameCount; i++) {
                MarkObject((_Frames[i] as GearsCallFrame)?.Function);
            }
            GearsObjUpvalue upvalue = _OpenUpvalues;
            while (upvalue != null) {
                MarkObject(upvalue);
                upvalue = upvalue.Next;
            }
        }

        public void MarkTable(GearsHashTable table) {
            foreach (GearsValue value in table.AllValues) {
                MarkValue(value);
            }
        }

        public void MarkValue(GearsValue value) {
            // we don't need to collect value types, as they require no heap allocation.
            if (!value.IsObjPtr) {
                return;
            }
            MarkObject(value.AsObject(this));
        }

        public void MarkObject(GearsObj obj) {
            if (obj == null || obj.IsMarked) {
                return;
            }
#if DEBUG_LOG_GC
            Console.WriteLine($"Mark {obj}");
#endif
            _GrayList.Enqueue(obj);
            obj.IsMarked = true;
        }

        private void TraceReferences() {
            while (_GrayList.Count > 0) {
                GearsObj obj = _GrayList.Dequeue();
#if DEBUG_LOG_GC
            Console.WriteLine($"Blacken {obj}");
#endif
                obj.Blacken(this);
            }
        }

        /// <summary>
        /// Reclaim all unmarked objects.
        /// </summary>
        private void Sweep() {
            for (int i = 0; i < _Heap.Length; i++) {
                if (_Heap[i] == null) {
                    continue;
                }
                if (_Heap[i].IsMarked) {
                    _Heap[i].IsMarked = false;
                    continue;
                }
#if DEBUG_LOG_GC
                Console.WriteLine($"Collect {i}");
#endif
                _Heap[i] = null;
            }
        }
    }
}
