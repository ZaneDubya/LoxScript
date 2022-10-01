#if NET_4_5
using System.Runtime.CompilerServices;
#endif
using System;
using System.Collections.Generic;
using XPT.Core.IO;
using XPT.Core.Scripting.Base;
using XPT.Core.Scripting.LoxScript.VirtualMachine;
using XPT.Core.Scripting.Rules;

namespace XPT.Core.Scripting.LoxScript {
    /// <summary>
    /// A wrapper around an array of bytes.
    /// </summary>
    internal class GearsChunk {
        internal static bool TryDeserialize(string name, IReader reader, out GearsChunk chunk) {
            chunk = new GearsChunk(name);
            if (!chunk.Deserialize(reader)) {
                chunk = null;
                return false;
            }
            return true;
        }

        private const int InitialConstantCapcity = 8;
        private const int InitialChunkCapcity = 8;
        private const int GrowCapacityFactor = 2;

        internal int SizeTotal => SizeCode + SizeConstant + Strings.SizeStringTable;

        /// <summary>
        /// How much of the chunk code array is in use.
        /// </summary>
        internal int SizeCode { get; private set; } = 0;

        /// <summary>
        /// How much of the constant array is in use.
        /// </summary>
        internal int SizeConstant { get; private set; } = 0;

        internal StringTable Strings = new StringTable();

        internal byte[] _Code = null;

        internal ulong[] _Constants = null;

        internal ushort[] _Lines = null; // todo: optimize with RLE.

        internal Rule[] Rules = null;

        internal readonly string Name;

        internal GearsChunk(string name) {
            Name = name;
        }

        internal void Compress() {
            if (SizeCode < _Code?.Length) {
                Array.Resize(ref _Code, SizeCode);
                Array.Resize(ref _Lines, SizeCode);
            }
            if (SizeConstant < _Constants?.Length) {
                Array.Resize(ref _Constants, SizeConstant);
            }
            Strings.Compress();
        }

        internal void Serialize(IWriter writer) {
            Compress();
            writer.WriteFourBytes("loxx");
            writer.Write7BitInt(3); // version
            writer.Write7BitInt(_Code?.Length ?? 0);
            if (_Code != null) {
                writer.Write(_Code);
            }
            writer.Write7BitInt(_Constants?.Length ?? 0);
            if (_Constants != null) {
                for (int i = 0; i < _Constants.Length; i++) {
                    writer.Write(_Constants[i]);
                }
            }
            Strings.Serialize(writer);
            writer.Write7BitInt(_Lines?.Length ?? 0);
            if (_Lines != null) {
                for (int i = 0; i < _Lines.Length; i++) {
                    writer.Write7BitInt(_Lines[i]);
                }
            }
            if (Rules?.Length > 0) {
                writer.WriteFourBytes("rule");
                writer.Write7BitInt(Rules.Length);
                for (int i = 0; i < Rules.Length; i++) {
                    Rules[i].Serialize(writer);
                }
            }
        }

        internal bool Deserialize(IReader reader) {
            if (!reader.ReadFourBytes("loxx")) {
                return false;
            }
            int version = reader.Read7BitInt();
            SizeCode = reader.Read7BitInt();
            if (SizeCode > 0) {
                _Code = reader.ReadBytes(SizeCode);
            }
            SizeConstant = reader.Read7BitInt();
            if (SizeConstant > 0) {
                _Constants = new ulong[SizeConstant];
                for (int i = 0; i < SizeConstant; i++) {
                    _Constants[i] = (ulong)reader.ReadLong();
                }
            }
            Strings.Deserialize(reader);
            int linesLength = reader.Read7BitInt();
            if (linesLength > 0) {
                _Lines = new ushort[linesLength];
                for (int i = 0; i < linesLength; i++) {
                    _Lines[i] = (ushort)reader.Read7BitInt();
                }
            }
            if (reader.ReadFourBytes("rule")) {
                Rules = new Rule[reader.Read7BitInt()];
                for (int i = 0; i < Rules.Length; i++) {
                    Rules[i] = Rule.Deserialize(reader);
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
            if (index < 0 || index >= SizeCode) {
                throw new GearsRuntimeException(0, "Attempted to read outside of a chunk.");
            }
            return _Code[index++];
        }

        internal void WriteCode(EGearsOpCode value, int line) {
            WriteCode((byte)value, line);
        }

        internal void WriteCode(byte value, int line) {
            int capacity = _Code?.Length ?? 0;
            if (capacity < SizeCode + 1) {
                GrowCodeCapacity();
            }
            _Lines[SizeCode] = (ushort)line;
            _Code[SizeCode++] = value;
        }

        internal void WriteCode(byte[] value, ushort[] lines, int count) {
            for (int i = 0; i < count; i++) {
                WriteCode(value[i], lines[i]);
            }
        }

        internal void WriteCodeAt(int offset, byte value) {
            _Code[offset] = value;
            _Lines[offset] = value;
        }

        internal int LineAt(int index) {
            if (index < 0 || index >= SizeCode) {
                return -1; // todo: runtime error
            }
            return _Lines[index];
        }

        private void GrowCodeCapacity() {
            if (_Code == null) {
                _Code = new byte[InitialChunkCapcity];
                _Lines = new ushort[InitialChunkCapcity];
            }
            else {
                int newCapacity = _Code.Length * GrowCapacityFactor;
                Array.Resize(ref _Code, newCapacity);
                Array.Resize(ref _Lines, newCapacity);
            }
        }

        // === Constants =============================================================================================
        // ===========================================================================================================

#if NET_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        internal GearsValue ReadConstantValue(int offset) {
            if (offset < 0 || offset > SizeConstant) {
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
            int index = SizeConstant;
            _Constants[index] = (ulong)value;
            SizeConstant += 1;
            return index;
        }

        private void CheckGrowConstantCapacity(int size) {
            int capacity = _Constants?.Length ?? 0;
            if (capacity < SizeConstant + size) {
                int newCapacity = _Constants == null ? InitialConstantCapcity : _Constants.Length * GrowCapacityFactor;
                while (newCapacity < SizeConstant + size) {
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

        // === Rules =================================================================================================
        // ===========================================================================================================

        internal IEnumerable<string> GetRuleMatches(string triggerName, RuleInvocationContext context) {
            ulong triggerBitString = BitString.GetBitStr(triggerName);
            for (int i = 0; i < Rules.Length; i++) {
                if (Rules[i].IsTrue(triggerBitString, context)) {
                    yield return BitString.GetBitStr(Rules[i].Result);
                }
            }
        }
    }
}
