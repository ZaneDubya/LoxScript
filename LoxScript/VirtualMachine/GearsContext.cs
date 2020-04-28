namespace LoxScript.VirtualMachine {
    /// <summary>
    /// The state of a running gears instance.
    /// It represents the Stack and the Heap.
    /// </summary>
    class GearsContext {
        // --- call frames -------------------------------------------------------------------------------------------
        // This should be part of the stack! See todo.md
        private const int FRAMES_MAX = 64;
        private GearsCallFrame[] _Frames = new GearsCallFrame[FRAMES_MAX];

        // --- stack -------------------------------------------------------------------------------------------------
        private const int STACK_MAX = 256;
        private GearsValue[] _Stack;
        private int _StackTop;

        // --- 'heap' ------------------------------------------------------------------------------------------------
        private const int HEAP_MAX = 256;
        private GearsObj[] _Heap;

        internal readonly GearsHashTable Globals;
        internal int IP;

        internal GearsContext() {
            _Stack = new GearsValue[STACK_MAX];
            _Heap = new GearsObj[HEAP_MAX];
            Globals = new GearsHashTable();
            Reset();
        }

        internal void Reset() {
            _StackTop = 0;
            for (int i = 0; i < _Heap.Length; i++) {
                _Heap[i] = null;
            }
        }

        // === Call frames ==========================================================================================
        // === This should be part of the stack! See todo.md ========================================================
        // ==========================================================================================================



        // === Stack ================================================================================================
        // ==========================================================================================================

        internal GearsValue StackGet(int index) {
            if (index < 0 || index >= _StackTop) {
                return -1; // todo, throw runtime exception, stack exception
            }
            return _Stack[index];
        }

        internal void StackSet(int index, GearsValue value) {
            if (index < 0 || index >= _StackTop) {
                return; // todo, throw runtime exception, stack exception
            }
            _Stack[index] = value;
        }

        internal void Push(GearsValue value) {
            if (_StackTop >= STACK_MAX) {
                // todo: throw runtime exception stack space
            }
            _Stack[_StackTop++] = value;
        }

        internal GearsValue Pop() {
            if (_StackTop == 0) {
                // todo: throw runtime exception stack space
            }
            return _Stack[--_StackTop];
        }

        internal GearsValue Peek(int offset = 0) {
            if (_StackTop - 1 - offset < 0) {
                // todo: throw runtime exception stack space
            }
            return _Stack[_StackTop - 1 - offset];
        }

        // === Heap ==================================================================================================
        // ===========================================================================================================

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
