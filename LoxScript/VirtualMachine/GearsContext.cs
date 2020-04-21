namespace LoxScript.VirtualMachine {
    /// <summary>
    /// The state of a running gears instance.
    /// </summary>
    class GearsContext {
        private const int STACK_MAX = 256;
        private GearsValue[] _Stack;
        private int _StackTop;

        internal int IP;

        internal GearsContext() {
            _Stack = new GearsValue[STACK_MAX];
            Reset();
        }

        internal void Reset() {
            _StackTop = 0;
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
