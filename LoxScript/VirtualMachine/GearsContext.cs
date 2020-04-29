namespace LoxScript.VirtualMachine {
    /// <summary>
    /// The state of a running gears instance.
    /// It represents the Call frame stack, the value Stack, and the Heap.
    /// </summary>
    class GearsContext {

        internal readonly GearsHashTable Globals;

        internal GearsContext(GearsObjFunction fn) {
            _Stack = new GearsValue[STACK_MAX];
            _Heap = new GearsObj[HEAP_MAX];
            Globals = new GearsHashTable();
            Reset();
            AddFrame(new GearsCallFrame(fn));
            Push(GearsValue.CreateObjPtr(AddObject(fn)));
        }

        internal void Reset() {
            _StackTop = 0;
            for (int i = 0; i < _Heap.Length; i++) {
                _Heap[i] = null;
            }
        }

        // === Call frames ==========================================================================================
        // === This should be part of the stack! See todo.md ========================================================
        // === Dear lord this is so inefficient! ====================================================================
        // ==========================================================================================================

        private const int FRAMES_MAX = 64;
        private GearsCallFrame[] _Frames = new GearsCallFrame[FRAMES_MAX];
        private int _FrameCount = 0;

        internal GearsCallFrame Frame => _Frames[_FrameCount - 1];

        internal GearsCallFrame AddFrame(GearsCallFrame frame) {
            _Frames[_FrameCount++] = frame;
            return frame;
        }

        internal void ModIP(int value) {
            _Frames[_FrameCount - 1].IP += value;
        }

        internal int ReadByte() {
            GearsCallFrame frame = Frame;
            return frame.Function.Chunk.Read(ref frame.IP);
        }

        internal int ReadShort() {
            GearsCallFrame frame = Frame;
            return (frame.Function.Chunk.Read(ref frame.IP) << 8) | frame.Function.Chunk.Read(ref frame.IP);
        }

        internal GearsValue ReadConstant() {
            int index = ReadShort();
            GearsCallFrame frame = Frame;
            return frame.Function.Chunk.ReadConstantValue(ref index);
        }

        internal string ReadConstantString() {
            int index = ReadShort();
            GearsCallFrame frame = Frame;
            return frame.Function.Chunk.ReadConstantString(ref index);
        }

        internal int LineAtLast() {
            GearsCallFrame frame = Frame;
            return frame.Function.Chunk.LineAt(frame.IP - 1);
        }

        // === Stack ================================================================================================
        // ==========================================================================================================

        internal int SP => _StackTop;

        private const int STACK_MAX = 256;
        private GearsValue[] _Stack;
        private int _StackTop;

        internal GearsValue StackGet(int index) {
            if (index < 0 || index >= _StackTop) {
                throw new Gears.RuntimeException(LineAtLast(), "Stack exception");
            }
            return _Stack[index];
        }

        internal void StackSet(int index, GearsValue value) {
            if (index < 0 || index >= _StackTop) {
                throw new Gears.RuntimeException(LineAtLast(), "Stack exception");
            }
            _Stack[index] = value;
        }

        internal void Push(GearsValue value) {
            if (_StackTop >= STACK_MAX) {
                throw new Gears.RuntimeException(LineAtLast(), "Stack exception");
            }
            _Stack[_StackTop++] = value;
        }

        internal GearsValue Pop() {
            if (_StackTop == 0) {
                throw new Gears.RuntimeException(LineAtLast(), "Stack exception");
            }
            return _Stack[--_StackTop];
        }

        internal GearsValue Peek(int offset = 0) {
            if (_StackTop - 1 - offset < 0) {
                throw new Gears.RuntimeException(LineAtLast(), "Stack exception");
            }
            return _Stack[_StackTop - 1 - offset];
        }

        // === Heap ==================================================================================================
        // ===========================================================================================================

        private const int HEAP_MAX = 256;
        private GearsObj[] _Heap;

        internal int AddObject(GearsObj obj) {
            for (int i = 0; i < _Heap.Length; i++) {
                if (_Heap[i] == null) {
                    _Heap[i] = obj;
                    return i;
                }
            }
            // todo: throw runtime exception, out of heap space
            return -1;
        }

        internal GearsObj GetObject(int index) {
            if (index < 0 || index >= _Heap.Length || _Heap[index] == null) {
                return null; // todo, throw runtime exception, null object
            }
            return _Heap[index];
        }

        internal void FreeObject(int index) {
            _Heap[index] = null;
        }
    }
}
