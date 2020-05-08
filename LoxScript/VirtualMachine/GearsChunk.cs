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

        /// <summary>
        /// How much of the constant string table is in use.
        /// </summary>
        internal int StringTableSize { get; private set; } = 0;

        internal byte[] _Code = null;

        internal byte[] _Constants = null;

        internal byte[] _StringTable = null;

        private int[] _Lines = null; // todo: optimize with RLE.

        internal GearsChunk(string name, byte[] code = null, byte[] constants = null, byte[] stringTable = null) {
            Name = name;
            if (code != null) {
                _Code = code;
                CodeSize = code.Length;
            }
            if (constants != null) {
                _Constants = constants;
                ConstantSize = constants.Length;
            }
            if (stringTable != null) {
                _StringTable = stringTable;
                StringTableSize = stringTable.Length;
            }
        }

        internal void Compress() {
            if (CodeSize > 0) {
                byte[] newCode = new byte[CodeSize];
                Array.Copy(_Code, newCode, CodeSize);
                _Code = newCode;
            }
            if (ConstantSize > 0) {
                byte[] newConstants = new byte[ConstantSize];
                Array.Copy(_Constants, newConstants, ConstantSize);
                _Constants = newConstants;
            }
            if (StringTableSize > 0) {
                byte[] newStringTable = new byte[StringTableSize];
                Array.Copy(_StringTable, newStringTable, StringTableSize);
                _StringTable = newStringTable;
            }
        }

        // === Code Bytes and Lines ==================================================================================
        // ===========================================================================================================

        /// <summary>
        /// Returns the next value, and advances index by the number of bytes read.
        /// </summary>
        internal int ReadCode(ref int index) {
            if (index < 0 || index >= CodeSize) {
                throw new GearsRuntimeException(0, "Attempted to read outside of a chunk.");
            }
            return _Code[index++];
        }

        internal void WriteCode(EGearsOpCode value) {
            WriteCode((byte)value);
        }

        internal void WriteCode(byte value) {
            int capacity = _Code?.Length ?? 0;
            if (capacity < CodeSize + 1) {
                GrowCodeCapacity();
            }
            _Code[CodeSize++] = value;
        }

        internal void WriteCodeAt(int offset, byte value) {
            _Code[offset] = value;
        }

        internal int LineAt(int index) {
            if (index < 0 || index >= CodeSize) {
                return -1; // todo: runtime error
            }
            return _Lines[index];
        }

        private void GrowCodeCapacity() {
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

        internal GearsValue ReadConstantValue(ref int offset) {
            if (offset < 0 || offset + 8 > ConstantSize) {
                return -1; // todo: runtime error
            }
            GearsValue value = new GearsValue(BitConverter.ToUInt64(_Constants, offset));
            offset += 8;
            return value;
        }

        internal byte ReadConstantByte(ref int offset) {
            if (offset < 0 || offset + 1 > ConstantSize) {
                return byte.MaxValue; // todo: runtime error
            }
            byte value = _Constants[offset];
            offset += 1;
            return value;
        }

        internal byte[] ReadConstantBytes(ref int offset) {
            if (offset < 0 || offset + 2 > ConstantSize) {
                return null; // todo: runtime error
            }
            int size = ReadConstantShort(ref offset);
            if (size == 0 || offset + size > ConstantSize) {
                return null;
            }
            byte[] value = new byte[size];
            Array.Copy(_Constants, offset, value, 0, size);
            offset += size;
            return value;
        }

        internal int ReadConstantShort(ref int offset) {
            return (ReadConstantByte(ref offset) << 8) | ReadConstantByte(ref offset);
        }

        internal int WriteConstantValue(GearsValue value) {
            CheckGrowConstantCapcity(8);
            int index = ConstantSize;
            Array.Copy(value.AsBytes, 0, _Constants, index, 8);
            ConstantSize += 8;
            return index;
        }

        internal int WriteConstantByte(byte value) {
            CheckGrowConstantCapcity(1);
            int index = ConstantSize;
            _Constants[ConstantSize] = value;
            ConstantSize += 1;
            return index;
        }

        internal int WriteConstantShort(int value) {
            int index = ConstantSize;
            WriteConstantByte((byte)((value >> 8) & 0xff));
            WriteConstantByte((byte)(value & 0xff));
            return index;
        }

        internal int WriteConstantBytes(byte[] value) {
            int size = value.Length;
            int capacity = _Constants?.Length ?? 0;
            if (capacity < ConstantSize + size) {
                CheckGrowConstantCapcity(size);
            }
            int index = ConstantSize;
            Array.Copy(value, 0, _Constants, index, size);
            ConstantSize += size;
            return index;
        }

        private void CheckGrowConstantCapcity(int size) {
            int capacity = _Constants?.Length ?? 0;
            if (capacity < ConstantSize + size) {
                int newCapacity = _Constants == null ? InitialConstantCapcity : _Constants.Length * GrowCapacityFactor;
                while (newCapacity < ConstantSize + size) {
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

        // === Strings ===============================================================================================
        // ===========================================================================================================

        internal string ReadStringConstant(int offset) {
            if (offset < 0) {
                return null; // todo: runtime error
            }
            for (int i = offset; i < StringTableSize; i++) {
                if (_StringTable[i] == 0) {
                    string value = Encoding.ASCII.GetString(_StringTable, offset, i - offset);
                    offset = i + 1;
                    return value;
                }
            }
            return null; // todo: runtime error
        }

        internal int WriteStringConstant(string value) {
            byte[] ascii = Encoding.ASCII.GetBytes(value);
            int size = ascii.Length + 1;
            CheckGrowStringTable(size);
            int index = StringTableSize;
            Array.Copy(ascii, 0, _StringTable, index, ascii.Length);
            StringTableSize += size;
            return index;
        }

        private void CheckGrowStringTable(int size) {
            int capacity = _StringTable?.Length ?? 0;
            if (capacity < StringTableSize + size) {
                int newCapacity = _StringTable == null ? InitialConstantCapcity : _StringTable.Length * GrowCapacityFactor;
                while (newCapacity < StringTableSize + size) {
                    newCapacity *= GrowCapacityFactor;
                }
                if (_StringTable == null) {
                    _StringTable = new byte[newCapacity];
                }
                else {
                    byte[] newData = new byte[newCapacity];
                    Array.Copy(_StringTable, newData, _StringTable.Length);
                    _StringTable = newData;
                }
            }
        }
    }
}
