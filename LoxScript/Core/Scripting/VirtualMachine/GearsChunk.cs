using System;
using System.Runtime.CompilerServices;
using System.Text;
using XPT.Core.IO;

namespace XPT.Core.Scripting.VirtualMachine {
    /// <summary>
    /// A wrapper around an array of bytes.
    /// </summary>
    class GearsChunk {
        private const int InitialConstantCapcity = 8;
        private const int InitialChunkCapcity = 8;
        private const int GrowCapacityFactor = 2;

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

        internal ulong[] _Constants = null;

        internal byte[] _StringTable = null;

        private int[] _Lines = null; // todo: optimize with RLE.

        /// <summary>
        /// Creates an empty chunk.
        /// </summary>
        internal GearsChunk() {

        }

        internal void Compress() {
            if (CodeSize > 0) {
                byte[] newCode = new byte[CodeSize];
                Array.Copy(_Code, newCode, CodeSize);
                _Code = newCode;
            }
            if (ConstantSize > 0) {
                ulong[] newConstants = new ulong[ConstantSize];
                Array.Copy(_Constants, newConstants, ConstantSize);
                _Constants = newConstants;
            }
            if (StringTableSize > 0) {
                byte[] newStringTable = new byte[StringTableSize];
                Array.Copy(_StringTable, newStringTable, StringTableSize);
                _StringTable = newStringTable;
            }
        }

        internal void Serialize(IWriter writer) {
            writer.WriteFourBytes("loxx");
            writer.Write7BitInt((int)1);
            writer.Write7BitInt((int)(_Code?.Length ?? 0));
            writer.Write7BitInt((int)(_Constants?.Length ?? 0));
            writer.Write7BitInt((int)(_StringTable?.Length ?? 0));
            writer.Write7BitInt((int)(_Lines?.Length ?? 0));
            if (_Code != null) {
                writer.Write(_Code);
            }
            if (_Constants != null) {
                for (int i = 0; i < _Constants.Length; i++) {
                    writer.Write(_Constants[i]);
                }
            }
            if (_StringTable != null) {
                writer.Write(_StringTable);
            }
            if (_Lines != null) {
                for (int i = 0; i < _Lines.Length; i++) {
                    writer.Write7BitInt(_Lines[i]);
                }
            }
        }

        internal bool Deserialize(IReader reader) {
            if (!reader.ReadFourBytes("loxx")) {
                return false;
            }
            int version = reader.Read7BitInt();
            CodeSize = reader.Read7BitInt();
            ConstantSize = reader.Read7BitInt();
            StringTableSize = reader.Read7BitInt();
            int linesLength = reader.Read7BitInt();
            if (CodeSize > 0) {
                _Code = reader.ReadBytes(CodeSize);
            }
            if (ConstantSize > 0) {
                _Constants = new ulong[ConstantSize];
                for (int i = 0; i < ConstantSize; i++) {
                    _Constants[i] = (ulong)reader.ReadLong();
                }
            }
            if (StringTableSize > 0) {
                _StringTable = reader.ReadBytes(StringTableSize);
            }
            if (linesLength > 0) {
                _Lines = new int[linesLength];
                for (int i = 0; i < ConstantSize; i++) {
                    _Lines[i] = reader.Read7BitInt();
                }
            }
            return true;
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

        internal void WriteCode(byte[] value) {
            foreach (byte b in value) {
                WriteCode(b);
            }
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal GearsValue ReadConstantValue(int offset) {
            if (offset < 0 || offset > ConstantSize) {
                return -1; // todo: runtime error
            }
            GearsValue value = _Constants[offset];
            return value;
        }

        internal string ReadConstantValueAsBitStr(int offset) {
            GearsValue value = ReadConstantValue(offset);
            return BitString.GetBitStr((ulong)value);
        }

        internal int WriteConstantValue(GearsValue value) {
            CheckGrowConstantCapacity(1);
            int index = ConstantSize;
            _Constants[index] = (ulong)value;
            ConstantSize += 1;
            return index;
        }

        private void CheckGrowConstantCapacity(int size) {
            int capacity = _Constants?.Length ?? 0;
            if (capacity < ConstantSize + size) {
                int newCapacity = _Constants == null ? InitialConstantCapcity : _Constants.Length * GrowCapacityFactor;
                while (newCapacity < ConstantSize + size) {
                    newCapacity *= GrowCapacityFactor;
                }
                if (_Constants == null) {
                    _Constants = new ulong[newCapacity];
                }
                else {
                    ulong[] newData = new ulong[newCapacity];
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
