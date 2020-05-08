// #define DEBUG_LOG_GC

using System;
using System.Collections.Generic;

namespace LoxScript.VirtualMachine {
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

        internal void Reset(GearsObjFunction fn) {
            _FrameCount = 0;
            _SP = 0;
            for (int i = 0; i < _Heap.Length; i++) {
                _Heap[i] = null;
            }
            Globals.Reset();
            _GrayList.Clear();
            PushFrame(new GearsCallFrame(fn));
            Push(GearsValue.CreateObjPtr(HeapAddObject(fn)));
        }

        public override string ToString() => $"{_OpenFrame.Function}@{_IP}";

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

        internal int ReadByte() {
            return _Code[_IP++];
        }

        internal int ReadShort() {
            return (_Code[_IP++] << 8) | _Code[_IP++];
        }

        internal GearsValue ReadConstant() {
            int index = ReadShort();
            return Chunk.ReadConstantValue(ref index);
        }

        internal string ReadConstantString() {
            int index = ReadShort();
            return Chunk.ReadStringConstant(index);
        }

        // === Stack ================================================================================================
        // ==========================================================================================================

        private const int STACK_MAX = 256;
        private readonly GearsValue[] _Stack;
        protected int _SP;

        internal GearsValue StackGet(int index) {
            if (index < 0 || index >= _SP) {
                throw new GearsRuntimeException(0, "Stack exception");
            }
            return _Stack[index];
        }

        internal void StackSet(int index, GearsValue value) {
            if (index < 0 || index >= _SP) {
                throw new GearsRuntimeException(0, "Stack exception");
            }
            _Stack[index] = value;
        }

        internal void Push(GearsValue value) {
            if (_SP >= STACK_MAX) {
                throw new GearsRuntimeException(0, "Stack exception");
            }
            _Stack[_SP++] = value;
        }

        internal GearsValue Pop() {
            if (_SP == 0) {
                throw new GearsRuntimeException(0, "Stack exception");
            }
            return _Stack[--_SP];
        }

        internal GearsValue Peek(int offset = 0) {
            if (_SP - 1 - offset < 0) {
                throw new GearsRuntimeException(0, "Stack exception");
            }
            return _Stack[_SP - 1 - offset];
        }

        // === Heap ==================================================================================================
        // ===========================================================================================================

        private const int HEAP_MAX = 32;
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
                MarkObject((_Frames[i] as GearsCallFrameClosure)?.Closure);
            }
            GearsObjUpvalue upvalue = _OpenUpvalues;
            while (upvalue != null) {
                MarkObject(upvalue);
                upvalue = upvalue.Next;
            }
        }

        private void MarkTable(GearsHashTable table) {
            foreach (GearsValue value in table.AllValues) {
                MarkValue(value);
            }
        }

        private void MarkValue(GearsValue value) {
            // we don't need to collect value types, as they require no heap allocation.
            if (!value.IsObjPtr) {
                return;
            }
            MarkObject(value.AsObject(this));
        }

        private void MarkObject(GearsObj obj) {
            if (obj == null || obj.IsMarked) {
                return;
            }
#if DEBUG_LOG_GC                 
            Console.WriteLine($"Mark {obj}");
#endif
            obj.IsMarked = true;
        }

        private void TraceReferences() {
            while (_GrayList.Count > 0) {
                GearsObj obj = _GrayList.Dequeue();
                BlackenObject(obj);
            }
        }

        private void BlackenObject(GearsObj obj) {
#if DEBUG_LOG_GC                 
            Console.WriteLine($"Blacken {obj}");
#endif
            switch (obj.Type) {
                case GearsObj.ObjType.ObjBoundMethod:
                    MarkValue((obj as GearsObjBoundMethod).Receiver);
                    MarkObject((obj as GearsObjBoundMethod).Method);
                    break;
                case GearsObj.ObjType.ObjClass:
                    MarkTable((obj as GearsObjClass).Methods);
                    break;
                case GearsObj.ObjType.ObjClosure:
                    MarkObject((obj as GearsObjClosure).Function);
                    foreach (GearsObjUpvalue upvalue in (obj as GearsObjClosure).Upvalues) {
                        MarkObject(upvalue);
                    }
                    break;
                case GearsObj.ObjType.ObjUpvalue:
                    MarkValue((obj as GearsObjUpvalue).Value);
                    break;
                case GearsObj.ObjType.ObjInstance:
                    MarkObject((obj as GearsObjInstance).Class);
                    MarkTable((obj as GearsObjInstance).Fields);
                    break;
                case GearsObj.ObjType.ObjFunction:
                case GearsObj.ObjType.ObjNative:
                case GearsObj.ObjType.ObjString:
                    // these have no outgoing references, so there is nothing to traverse.
                    break;
            }
        }

        /// <summary>
        /// Reclaim all unmarked objects.
        /// </summary>
        private void Sweep() {
            for (int i = 0; i < _Heap.Length; i++) {
                if (!_Heap[i].IsMarked) {
                    Console.WriteLine($"Collect {i}");
                    _Heap[i] = null;
                }
            }
        }
    }
}
