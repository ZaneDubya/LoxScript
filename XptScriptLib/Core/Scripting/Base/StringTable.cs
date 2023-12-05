using System;
using XPT.Core.IO;
using XPT.Core.Utilities;

namespace XPT.Core.Scripting.Base {
    /// <summary>
    /// StringTable stores constant strings that are referenced by the VM while executing a chunk of bytecode.
    /// </summary>
    sealed class StringTable {
        private const int InitialConstantCapcity = 8;
        private const int GrowCapacityFactor = 2;

        private byte[] _StringTable = null;

        /// <summary>
        /// How much of the constant string table is in use.
        /// </summary>
        internal int SizeStringTable { 
            get; 
            private set; 
        } = 0;

        internal void Compress() {
            if (SizeStringTable < _StringTable?.Length) {
                Array.Resize(ref _StringTable, SizeStringTable);
            }
        }

        internal void Serialize(IWriter writer) {
            writer.Write7BitInt((int)(SizeStringTable));
            if (_StringTable != null) {
                writer.Write(_StringTable);
            }
        }

        internal void Deserialize(IReader reader) {
            SizeStringTable = reader.Read7BitInt();
            _StringTable = reader.ReadBytes(SizeStringTable);
        }

        internal string ReadStringConstant(int offset) {
            if (offset < 0) {
                return null; // todo: runtime error
            }
            for (int i = offset; i < SizeStringTable; i++) {
                if (_StringTable[i] == 0) {
                    string value = TextEncoding.GetString(_StringTable, offset, i - offset);
                    return value;
                }
            }
            return null; // todo: runtime error
        }

        /// <summary>
        /// Adds the given string to the chunk's string table.
        /// Returns the index of that string in the string table.
        /// </summary>
        internal int WriteStringConstant(string value) {
            byte[] ascii = TextEncoding.GetBytes(value);
            if (ascii == null) {
                return -1;
            }
            int size = ascii.Length + 1;
            // does the string exist in this chunk already?
            for (int i = 0; i <= SizeStringTable - size; i++) {
                if (_StringTable[i + size - 1] != 0) {
                    continue;
                }
                bool match = true;
                for (int j = 0; j < ascii.Length; j++) {
                    if (_StringTable[i + j] != ascii[j]) {
                        match = false;
                        break;
                    }
                }
                if (match) {
                    // yes! return the index of the existing string.
                    return i;
                }
            }
            // nope! add it to the string table.
            CheckGrowStringTable(size);
            int index = SizeStringTable;
            Array.Copy(ascii, 0, _StringTable, index, ascii.Length);
            SizeStringTable += size;
            return index;
        }

        private void CheckGrowStringTable(int size) {
            int capacity = _StringTable?.Length ?? 0;
            if (capacity < SizeStringTable + size) {
                int newCapacity = _StringTable == null ? InitialConstantCapcity : _StringTable.Length * GrowCapacityFactor;
                while (newCapacity < SizeStringTable + size) {
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
