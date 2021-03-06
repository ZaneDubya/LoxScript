﻿// #define DEBUG_LOG_GC

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace XPT.Core.Scripting.VirtualMachine {
    /// <summary>
    /// The state of a running gears instance.
    /// It represents the Call frame stack, the value Stack, and the Heap.
    /// </summary>
    partial class Gears {

        internal readonly GearsHashTable Globals;

        internal Gears() {
            _Frames = new GearsCallFrame[FRAMES_MAX];
            _Stack = new GearsValue[STACK_MAX];
            _Heap = new GearsObj[HEAP_MAX];
            Globals = new GearsHashTable();
        }

        internal void Reset(GearsChunk chunk) {
            _FrameCount = 0;
            _SP = 0;
            for (int i = 0; i < _Heap.Length; i++) {
                _Heap[i] = null;
            }
            Globals.Reset();
            _GrayList.Clear();
            GearsObjFunction closure = new GearsObjFunction(chunk, 0, 0, 0);
            PushFrame(new GearsCallFrame(closure));
            Push(GearsValue.CreateObjPtr(HeapAddObject(closure)));
            DefineNative("clock", 0, NativeFnClock);
            DefineNative("print", 1, NativeFnPrint);
        }

        public override string ToString() => $"{_OpenFrame.Function}@{_IP}";

        // === Includes ============================================================================================
        // =========================================================================================================

        private GearsValue NativeFnClock(GearsValue[] args) {
            return Stopwatch.GetTimestamp() / ((double)Stopwatch.Frequency / 1000);
        }

        private GearsValue NativeFnPrint(GearsValue[] args) {
            Console.WriteLine(args[0].ToString(this));
            return GearsValue.NilValue;
        }

        internal void AddNativeObject(string name, object obj) {
            GearsObjInstanceNative instance = new GearsObjInstanceNative(this, obj);
            ulong bitstrName = BitString.GetBitStr(name);
            Globals.Set(bitstrName, GearsValue.CreateObjPtr(HeapAddObject(instance)));
        }

        // === Call frames ==========================================================================================
        // === This should be part of the stack! See todo.md ========================================================
        // === Dear lord this is so inefficient! ====================================================================
        // ==========================================================================================================

        private const int FRAMES_MAX = 64;
        private readonly GearsCallFrame[] _Frames;
        private int _FrameCount = 0;
        protected GearsObjUpvalue _OpenUpvalues = null; // reference to open upvariables
        protected GearsCallFrame _OpenFrame => _Frames[_FrameCount - 1]; // references the current Frame
        protected int _BP;
        protected int _IP;
        protected byte[] _Code;
        internal GearsChunk Chunk; // reference to current chunk

        internal void PushFrame(GearsCallFrame frame) {
            if (_FrameCount == FRAMES_MAX) {
                throw new GearsRuntimeException(0, "Stack frame overflow.");
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
                throw new GearsRuntimeException(0, "Stack frame underflow.");
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int ReadByte() {
            return _Code[_IP++];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int ReadShort() {
            return (_Code[_IP++] << 8) | _Code[_IP++];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal GearsValue ReadConstant() {
            int index = ReadShort();
            return Chunk.ReadConstantValue(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal string ReadConstantString() {
            int index = ReadShort();
            return Chunk.ReadStringConstant(index);
        }

        // === Stack ================================================================================================
        // ==========================================================================================================

        private const int STACK_MAX = 256;
        private readonly GearsValue[] _Stack;
        protected int _SP;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal GearsValue StackGet(int index) {
            if (index < 0 || index >= _SP) {
                throw new GearsRuntimeException(0, "Stack exception");
            }
            return _Stack[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void StackSet(int index, GearsValue value) {
            if (index < 0 || index >= _SP) {
                throw new GearsRuntimeException(0, "Stack exception");
            }
            _Stack[index] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Push(GearsValue value) {
            if (_SP >= STACK_MAX) {
                throw new GearsRuntimeException(0, "Stack exception");
            }
            _Stack[_SP++] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal GearsValue Pop() {
            if (_SP == 0) {
                throw new GearsRuntimeException(0, "Stack exception");
            }
            return _Stack[--_SP];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal GearsValue Peek(int offset = 0) {
            int index = _SP - 1 - offset;
            if (index < 0 || index >= _SP) {
                throw new GearsRuntimeException(0, "Stack exception");
            }
            return _Stack[index];
        }

        // === Heap ==================================================================================================
        // ===========================================================================================================

        private const int HEAP_MAX = 64;
        private GearsObj[] _Heap;

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
            throw new GearsRuntimeException(0, "Out of heap space.");
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

        private readonly Queue<GearsObj> _GrayList = new Queue<GearsObj>();

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
                if (!_Heap[i].IsMarked) {
#if DEBUG_LOG_GC
                    Console.WriteLine($"Collect {i}");
#endif
                    _Heap[i] = null;
                }
            }
        }
    }
}
