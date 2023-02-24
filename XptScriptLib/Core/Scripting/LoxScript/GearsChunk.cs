#if NET_4_5
using System.Runtime.CompilerServices;
#endif
using System;
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
            chunk = new GearsChunk(name, null);
            if (!chunk.Deserialize(reader)) {
                chunk = null;
                return false;
            }
            return true;
        }

        private const int InitialConstantCapcity = 8;
        private const int InitialChunkCapcity = 8;
        private const int GrowCapacityFactor = 2;

        internal int SizeTotal => SizeCode + SizeConstant + Strings.SizeStringTable + VarNameStrings.SizeStringTable;

        /// <summary>
        /// How much of the chunk code array is in use.
        /// </summary>
        internal int SizeCode { get; private set; } = 0;

        /// <summary>
        /// How much of the constant array is in use.
        /// </summary>
        internal int SizeConstant { get; private set; } = 0;

        internal readonly StringTable Strings;

        internal readonly StringTable VarNameStrings;

        internal byte[] Code = null;

        internal int[] Constants = null;

        internal ushort[] Lines = null; // todo: optimize with RLE.

        internal readonly string Name;

        internal RuleCollection Rules = null;

        internal GearsChunk(string name, GearsChunk containerChunk) {
            Name = name;
            if (containerChunk == null) {
                Strings = new StringTable();
                VarNameStrings = new StringTable();
            }
            else {
                Strings = containerChunk.Strings;
                VarNameStrings = containerChunk.VarNameStrings;
            }
        }

        internal void Compress() {
            if (SizeCode < Code?.Length) {
                Array.Resize(ref Code, SizeCode);
                Array.Resize(ref Lines, SizeCode);
            }
            if (SizeConstant < Constants?.Length) {
                Array.Resize(ref Constants, SizeConstant);
            }
            Strings.Compress();
            VarNameStrings.Compress();
        }

        internal void Serialize(IWriter writer) {
            Compress();
            writer.WriteFourBytes("loxx");
            writer.Write7BitInt(3); // version
            writer.Write7BitInt(Code?.Length ?? 0);
            if (Code != null) {
                writer.Write(Code);
            }
            writer.Write7BitInt(Constants?.Length ?? 0);
            if (Constants != null) {
                for (int i = 0; i < Constants.Length; i++) {
                    writer.Write(Constants[i]);
                }
            }
            Strings.Serialize(writer);
            VarNameStrings.Serialize(writer);
            writer.Write7BitInt(Lines?.Length ?? 0);
            if (Lines != null) {
                for (int i = 0; i < Lines.Length; i++) {
                    writer.Write7BitInt(Lines[i]);
                }
            }
            if (Rules != null) {
                Rules.Serialize(writer);
            }
        }

        internal bool Deserialize(IReader reader) {
            if (!reader.ReadFourBytes("loxx")) {
                return false;
            }
            int version = reader.Read7BitInt();
            SizeCode = reader.Read7BitInt();
            if (SizeCode > 0) {
                Code = reader.ReadBytes(SizeCode);
            }
            SizeConstant = reader.Read7BitInt();
            if (SizeConstant > 0) {
                Constants = new int[SizeConstant];
                for (int i = 0; i < SizeConstant; i++) {
                    Constants[i] = reader.ReadInt();
                }
            }
            Strings.Deserialize(reader);
            VarNameStrings.Deserialize(reader);
            int linesLength = reader.Read7BitInt();
            if (linesLength > 0) {
                Lines = new ushort[linesLength];
                for (int i = 0; i < linesLength; i++) {
                    Lines[i] = (ushort)reader.Read7BitInt();
                }
            }
            if (RuleCollection.TryDeserialize(reader, out RuleCollection collection)) {
                Rules = collection;
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
            return Code[index++];
        }

        internal void WriteCode(EGearsOpCode value, int line) {
            WriteCode((byte)value, line);
        }

        internal void WriteCode(byte value, int line) {
            int capacity = Code?.Length ?? 0;
            if (capacity < SizeCode + 1) {
                GrowCodeCapacity();
            }
            Lines[SizeCode] = (ushort)line;
            Code[SizeCode++] = value;
        }

        internal void WriteCode(byte[] value, ushort[] lines, int count) {
            for (int i = 0; i < count; i++) {
                WriteCode(value[i], lines[i]);
            }
        }

        internal void WriteCodeAt(int offset, byte value) {
            Code[offset] = value;
            Lines[offset] = value;
        }

        internal int LineAt(int index) {
            if (index < 0 || index >= SizeCode) {
                return -1; // todo: runtime error
            }
            return Lines[index];
        }

        private void GrowCodeCapacity() {
            if (Code == null) {
                Code = new byte[InitialChunkCapcity];
                Lines = new ushort[InitialChunkCapcity];
            }
            else {
                int newCapacity = Code.Length * GrowCapacityFactor;
                Array.Resize(ref Code, newCapacity);
                Array.Resize(ref Lines, newCapacity);
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
            GearsValue value = Constants[offset];
            return value;
        }

        internal int WriteConstantValue(GearsValue value) {
            CheckGrowConstantCapacity(1);
            int index = SizeConstant;
            Constants[index] = (int)value;
            SizeConstant += 1;
            return index;
        }

        private void CheckGrowConstantCapacity(int size) {
            int capacity = Constants?.Length ?? 0;
            if (capacity < SizeConstant + size) {
                int newCapacity = Constants == null ? InitialConstantCapcity : Constants.Length * GrowCapacityFactor;
                while (newCapacity < SizeConstant + size) {
                    newCapacity *= GrowCapacityFactor;
                }
                if (Constants == null) {
                    Constants = new int[newCapacity];
                }
                else {
                    int[] newData = new int[newCapacity];
                    Array.Copy(Constants, newData, Constants.Length);
                    Constants = newData;
                }
            }
        }
    }
}
