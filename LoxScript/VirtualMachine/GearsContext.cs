namespace LoxScript.VirtualMachine {
    /// <summary>
    /// The state of a running gears instance.
    /// It represents the Call frame stack, the value Stack, and the Heap.
    /// </summary>
    abstract class GearsContext {

        internal readonly GearsHashTable Globals;

        internal GearsContext() {
            _Stack = new GearsValue[STACK_MAX];
            _Heap = new GearsObj[HEAP_MAX];
            Globals = new GearsHashTable();
        }

        internal void Reset(GearsObjFunction fn) {
            _FrameCount = 0;
            _StackTop = 0;
            for (int i = 0; i < _Heap.Length; i++) {
                _Heap[i] = null;
            }
            Globals.Reset();
            PushFrame(new GearsCallFrame(fn));
            Push(GearsValue.CreateObjPtr(AddObject(fn)));
        }

        // === Call frames ==========================================================================================
        // === This should be part of the stack! See todo.md ========================================================
        // === Dear lord this is so inefficient! ====================================================================
        // ==========================================================================================================

        private const int FRAMES_MAX = 64;
        private GearsCallFrame[] _Frames = new GearsCallFrame[FRAMES_MAX];
        private int _FrameCount;

        // references to current chunk:
        internal GearsChunk Chunk;
        internal int BP;
        internal int IP;

        // internal GearsCallFrame Frame => _Frames[_FrameCount - 1];

        internal void PushFrame(GearsCallFrame frame) {
            if (_FrameCount == FRAMES_MAX) {
                throw new Gears.RuntimeException(0, "Stack frame overflow.");
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
            _StackTop = BP;
            _FrameCount -= 1;
            if (_FrameCount < 0) {
                throw new Gears.RuntimeException(0, "Stack frame underflow.");
            }
            if (_FrameCount > 0) {
                LoadFrameVars(_Frames[_FrameCount - 1]);
                return false;
            }
            return true;
        }

        private void SaveFrameVars(GearsCallFrame frame) {
            frame.BP = BP;
            frame.IP = IP;
        }

        private void LoadFrameVars(GearsCallFrame frame) {
            Chunk = frame.Function.Chunk;
            IP = frame.IP;
            BP = frame.BP;
        }

        internal void ModIP(int value) {
            IP += value;
        }

        internal int ReadByte() {
            return Chunk.Read(ref IP);
        }

        internal int ReadShort() {
            return (Chunk.Read(ref IP) << 8) | Chunk.Read(ref IP);
        }

        internal GearsValue ReadConstant() {
            int index = ReadShort();
            return Chunk.ReadConstantValue(ref index);
        }

        internal string ReadConstantString() {
            int index = ReadShort();
            return Chunk.ReadConstantString(ref index);
        }

        // === Stack ================================================================================================
        // ==========================================================================================================

        internal int SP => _StackTop;

        private const int STACK_MAX = 256;
        private GearsValue[] _Stack;
        private int _StackTop;

        internal GearsValue StackGet(int index) {
            if (index < 0 || index >= _StackTop) {
                throw new Gears.RuntimeException(0, "Stack exception");
            }
            return _Stack[index];
        }

        internal void StackSet(int index, GearsValue value) {
            if (index < 0 || index >= _StackTop) {
                throw new Gears.RuntimeException(0, "Stack exception");
            }
            _Stack[index] = value;
        }

        internal void Push(GearsValue value) {
            if (_StackTop >= STACK_MAX) {
                throw new Gears.RuntimeException(0, "Stack exception");
            }
            _Stack[_StackTop++] = value;
        }

        internal GearsValue Pop() {
            if (_StackTop == 0) {
                throw new Gears.RuntimeException(0, "Stack exception");
            }
            return _Stack[--_StackTop];
        }

        internal GearsValue Peek(int offset = 0) {
            if (_StackTop - 1 - offset < 0) {
                throw new Gears.RuntimeException(0, "Stack exception");
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
