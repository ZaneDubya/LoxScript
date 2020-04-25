using System;
using System.Text;

namespace LoxScript.VirtualMachine {
    /// <summary>
    /// A wrapper around an array of bytes.
    /// </summary>
    class GearsChunk {
        private const int InitialConstantCapcity = 8;
        private const int InitialChunkCapcity = 8;
        private const int GrowCapacityFactor = 2;

        internal readonly string Name;

        /// <summary>
        /// How much of the chunk code array is in use.
        /// </summary>
        internal int CodeSize { get; private set; } = 0;

        /// <summary>
        /// How much of the constant array is in use.
        /// </summary>
        internal int ConstantSize { get; private set; } = 0;

        private byte[] _Code = null;

        private byte[] _Constants = null;

        private int[] _Lines = null; // todo: optimize with RLE.

        internal GearsChunk(string name) {
            Name = name;
        }

        // === Code Bytes and Lines ==================================================================================
        // ===========================================================================================================

        /// <summary>
        /// Returns the next value, and advances index by the number of bytes read.
        /// </summary>
        internal int Read(ref int index) {
            if (index < 0 || index >= CodeSize) {
                return -1; // runtime error?
            }
            return _Code[index++];
        }

        internal void Write(byte value) {
            int capacity = _Code?.Length ?? 0;
            if (capacity < CodeSize + 1) {
                GrowChunkCapacity();
            }
            _Code[CodeSize++] = value;
        }

        internal void Write(EGearsOpCode value) {
            Write((byte)value);
        }

        internal void WriteAt(int offset, byte value) {
            _Code[offset] = value;
        }

        internal int LineAt(int index) {
            if (index < 0 || index >= CodeSize) {
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

        internal GearsValue GetConstantValue(int offset) {
            if (offset < 0 || offset + 8 > ConstantSize) {
                return -1; // todo: runtime error
            }
            return new GearsValue(BitConverter.ToUInt64(_Constants, offset));
        }

        internal string GetConstantString(int offset) {
            if (offset < 0) {
                return null; // todo: runtime error
            }
            for (int i = offset; i < ConstantSize; i++) {
                if (_Constants[i] == 0) {
                    return Encoding.ASCII.GetString(_Constants, offset, i - offset);
                }
            }
            return null; // todo: runtime error
        }

        /// <summary>
        /// Adds a 'value' constant to the collection. Returns the index of the stored constant.
        /// </summary>
        internal int AddConstant(GearsValue value) {
            int size = 8;
            int capacity = _Constants?.Length ?? 0;
            if (capacity < ConstantSize + size) {
                GrowConstantCapacity(size);
            }
            int index = ConstantSize;
            Array.Copy(value.AsBytes, 0, _Constants, index, size);
            ConstantSize += size;
            return index;
        }

        /// <summary>
        /// Adds a 'value' constant to the collection. Returns the index of the stored constant.
        /// </summary>
        internal int AddConstant(string value) {
            byte[] ascii = Encoding.ASCII.GetBytes(value);
            int size = ascii.Length + 1;
            int capacity = _Constants?.Length ?? 0;
            if (capacity < ConstantSize + size) {
                GrowConstantCapacity(size);
            }
            int index = ConstantSize;
            Array.Copy(ascii, 0, _Constants, index, ascii.Length);
            ConstantSize += size;
            return index;
        }

        private void GrowConstantCapacity(int minSizeToAdd) {
            int newCapacity = _Constants == null ? InitialConstantCapcity : _Constants.Length * GrowCapacityFactor;
            while (newCapacity < ConstantSize + minSizeToAdd) {
                newCapacity *= GrowCapacityFactor;
            }
            if (_Constants == null) {
                _Constants = new byte[newCapacity];
            }
            else {
                byte[] newData = new byte[newCapacity];
                Array.Copy(_Constants, newData, _Constants.Length);
                _Constants = newData;
            }
        }
    }
}
