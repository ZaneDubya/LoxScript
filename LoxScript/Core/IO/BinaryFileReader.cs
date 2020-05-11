using System;
using System.IO;
using System.Net;
using System.Text;

namespace XPT.Core.IO {
    public class BinaryFileReader : IDisposable, IReader {
        readonly BinaryReader _File;

        public BinaryFileReader(string path) {
            _File = new BinaryReader(new FileStream(path, FileMode.Open, FileAccess.Read));
        }

        public BinaryFileReader(MemoryStream stream) {
            _File = new BinaryReader(stream);
        }

        public BinaryFileReader(BinaryReader br) {
            _File = br;
        }

        public long Length => _File.BaseStream.Length;

        public long Position {
            get { return _File.BaseStream.Position; }
            set { _File.BaseStream.Position = value; }
        }

        public Stream Stream {
            get { return _File.BaseStream; }
        }

        public void Close() {
            _File.Close();
        }

        public long Seek(long offset, SeekOrigin origin) {
            return _File.BaseStream.Seek(offset, origin);
        }

        public virtual DateTime ReadDeltaTime() {
            long ticks = _File.ReadInt64();
            long now = DateTime.Now.Ticks; // allowed access to DateTime.Now

            if (ticks > 0 && (ticks + now) < 0) {
                return DateTime.MaxValue;
            }
            if (ticks < 0 && (ticks + now) < 0) {
                return DateTime.MinValue;
            }

            try {
                return new DateTime(now + ticks);
            }
            catch {
                if (ticks > 0) {
                    return DateTime.MaxValue;
                }
                return DateTime.MinValue;
            }
        }

        public virtual IPAddress ReadIPAddress() {
            return new IPAddress(_File.ReadInt64());
        }

        public virtual int Read7BitInt() {
            int value = 0;
            int shift = 0;
            while (true) {
                byte temp = ReadByte();
                value += (temp & 0x7F) << shift;
                if ((temp & 0x80) == 0x80) {
                    shift += 7;
                }
                else {
                    return value;
                }
            }
        }

        public virtual DateTime ReadDateTime() {
            return new DateTime(_File.ReadInt64());
        }

        public virtual TimeSpan ReadTimeSpan() {
            return new TimeSpan(_File.ReadInt64());
        }

        public virtual long ReadLong() {
            return _File.ReadInt64();
        }

        public virtual ulong ReadULong() {
            return _File.ReadUInt64();
        }

        public virtual int ReadInt() {
            return _File.ReadInt32();
        }

        public virtual uint ReadUInt() {
            return _File.ReadUInt32();
        }

        public virtual short ReadShort() {
            return _File.ReadInt16();
        }

        public virtual ushort ReadUShort() {
            return _File.ReadUInt16();
        }

        public virtual double ReadDouble() {
            return _File.ReadDouble();
        }

        public virtual float ReadFloat() {
            return _File.ReadSingle();
        }

        public virtual byte ReadByte() {
            return _File.ReadByte();
        }

        public virtual byte[] ReadBytes(int count) {
            return _File.ReadBytes(count);
        }

        public virtual ushort[] ReadUShorts(int count) {
            byte[] data = ReadBytes(count * 2);
            ushort[] data_out = new ushort[count];
            Buffer.BlockCopy(data, 0, data_out, 0, count * 2);
            return data_out;
        }

        public virtual int[] ReadInts(int count) {
            byte[] data = ReadBytes(count * 4);
            int[] data_out = new int[count];
            Buffer.BlockCopy(data, 0, data_out, 0, count * 4);
            return data_out;
        }

        public virtual uint[] ReadUInts(int count) {
            byte[] data = ReadBytes(count * 4);
            uint[] data_out = new uint[count];
            Buffer.BlockCopy(data, 0, data_out, 0, count * 4);
            return data_out;
        }

        public virtual string ReadAsciiPrefix() {
            int length = Read7BitInt();
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < length; i++) {
                sb.Append((char)ReadByte());
            }
            return sb.ToString();
        }

        /// <summary>
        /// WARNING: INCOMPLETE, ONLY READS 2-byte UTF8 chars.
        /// </summary>
        /// <returns></returns>
        public virtual char ReadCharUTF8() {
            int value = 0;
            byte b0 = ReadByte();
            if ((b0 & 0x80) == 0x00) {
                value = (b0 & 0x7F);
            }
            else {
                value = (b0 & 0x3F);
                byte b1 = ReadByte();
                if ((b1 & 0xE0) == 0xC0) {
                    value += (b1 & 0x1F) << 6;
                }
            }
            return (char)value;
        }

        public virtual sbyte ReadSByte() {
            return _File.ReadSByte();
        }

        public virtual bool ReadBool() {
            return _File.ReadBoolean();
        }

        public virtual bool ReadFourBytes(string value) {
            if (value.Length < 4) {
                // throw new Exception($"Can't deserialize {s} as four bytes.");
                return false;
            }
            for (int i = 0; i < 4; i++) {
                byte b = ReadByte();
                if ((byte)value[i] != b) {
                    // throw new Exception($"Tried to read {s}, but '{(char)b}' ({b:X2}) is in index {i}.");
                    return false;
                }
            }
            return true;
        }

        public bool End() {
            return _File.PeekChar() == -1;
        }

        public override string ToString() => $"{Stream.Position} / {Stream.Length}";

        public void Dispose() {
            _File.Close();
        }
    }
}