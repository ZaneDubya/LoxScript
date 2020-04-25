namespace LoxScript.VirtualMachine {
    /// <summary>
    /// The state of a running gears instance.
    /// It represents the Stack and the Heap.
    /// </summary>
    class GearsContext {
        // --- stack ---
        private const int STACK_MAX = 256;
        private GearsValue[] _Stack;
        private int _StackTop;

        // --- 'heap' ---
        private const int HEAP_MAX = 256;
        private GearsObj[] _Heap;

        internal int IP;

        internal GearsContext() {
            _Stack = new GearsValue[STACK_MAX];
            _Heap = new GearsObj[HEAP_MAX];
            Reset();
        }

        internal void Reset() {
            _StackTop = 0;
            for (int i = 0; i < _Heap.Length; i++) {
                _Heap[i] = null;
            }
        }

        internal void Push(GearsValue value) {
            if (_StackTop >= STACK_MAX) {
                // todo: throw runtime exception
            }
            _Stack[_StackTop++] = value;
        }

        internal GearsValue Pop() {
            if (_StackTop == 0) {
                // todo: throw runtime exception
            }
            return _Stack[--_StackTop];
        }

        internal GearsValue Peek(int offset = 0) {
            if (_StackTop - 1 - offset < 0) {
                // todo: throw runtime exception
            }
            return _Stack[_StackTop - 1 - offset];
        }
    }
}
