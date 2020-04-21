using System;

namespace LoxScript.VirtualMachine {
    /// <summary>
    /// A wrapper around an array of bytes.
    /// </summary>
    class GearsChunk {
        private const int InitialConstantCapcity = 4;
        private const int InitialChunkCapcity = 8;
        private const int GrowCapacityFactor = 2;

        internal readonly string Name;

        /// <summary>
        /// How much of the chunk is in use.
        /// </summary>
        internal int Count { get; private set; } = 0;

        /// <summary>
        /// How much of the chunk is in use.
        /// </summary>
        internal int ConstantCount { get; private set; } = 0;

        private byte[] _Code = null;

        private GearsValue[] _Constants = null;

        private int[] _Lines = null; // todo: optimize with RLE.

        internal GearsChunk(string name) {
            Name = name;
        }

        // === Code Bytes and Lines ==================================================================================
        // ===========================================================================================================

        internal int Read(ref int index) {
            if (index < 0 || index >= Count) {
                return -1; // runtime error?
            }
            return _Code[index++];
        }

        internal void Write(byte value) {
            int capacity = _Code?.Length ?? 0;
            if (capacity < Count + 1) {
                GrowChunkCapacity();
            }
            _Code[Count++] = value;
        }

        internal void Write(GearsOpCode value) {
            Write((byte)value);
        }

        internal int LineAt(int index) {
            if (index < 0 || index >= Count) {
                return -1; // todo: runtime error
            }
            return _Lines[index];
        }

        private void GrowChunkCapacity() {
            if (_Code == null) {
                _Code = new byte[InitialChunkCapcity];
                _Lines = new int[InitialChunkCapcity];
            }
            else {
                int newCapacity = _Code.Length * GrowCapacityFactor;
                byte[] newCode = new byte[newCapacity];
                Array.Copy(_Code, newCode, _Code.Length);
                _Code = newCode;
                int[] newLines = new int[newCapacity];
                Array.Copy(_Code, newLines, _Lines.Length);
                _Lines = newLines;
            }
        }

        // === Constants =============================================================================================
        // ===========================================================================================================

        internal GearsValue GetConstant(int offset) {
            if (offset < 0 || offset >= ConstantCount) {
                return -1; // todo: runtime error
            }
            return _Constants[offset];
        }

        /// <summary>
        /// Adds a 'value' constant to the collection. Returns the index of the stored constant.
        /// </summary>
        internal int AddConstant(GearsValue value) {
            int capacity = _Constants?.Length ?? 0;
            if (capacity < ConstantCount + 1) {
                GrowConstantCapacity();
            }
            int index = ConstantCount++;
            _Constants[index] = value;
            return index;
        }

        private void GrowConstantCapacity() {
            if (_Constants == null) {
                _Constants = new GearsValue[InitialConstantCapcity];
            }
            else {
                int newCapacity = _Constants.Length * GrowCapacityFactor;
                GearsValue[] newData = new GearsValue[newCapacity];
                Array.Copy(_Constants, newData, _Constants.Length);
                _Constants = newData;
            }
        }
    }
}
