using System;
using System.IO;
using System.Text;

namespace XPT.Core.IO {
    public sealed class BinaryFileWriter : IDisposable, IWriter {
        const int LARGE_BYTE_BUFFER_SIZE = 256;

        readonly byte[] _Buffer;

        readonly Encoding _Encoding;
        readonly Stream _File;
        readonly char[] _SingleCharBuffer = new char[1];
        int _Index;
        long _Position;

        public BinaryFileWriter(Stream strm, bool prefixStr) {
            _Encoding = Encoding.UTF8;
            _Buffer = new byte[BufferSize];
            _File = strm;
        }

        public BinaryFileWriter(string filename, bool prefixStr = false, bool openIfExists = false) {
            _Buffer = new byte[BufferSize];
            string directory = Path.GetDirectoryName(filename);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) {
                Directory.CreateDirectory(directory);
            }
            FileMode fileMode = openIfExists ? FileMode.OpenOrCreate : FileMode.Create;
            _File = new FileStream(filename, fileMode, FileAccess.Write, FileShare.None);
            _Encoding = Encoding.UTF8;
        }

        static int BufferSize => 64 * 1024;

        public long Position => _Position + _Index;

        public Stream UnderlyingStream {
            get {
                if (_Index > 0) {
                    Flush();
                }

                return _File;
            }
        }

        public void Flush() {
            if (_Index > 0) {
                _Position += _Index;

                _File.Write(_Buffer, 0, _Index);
                _Index = 0;
            }
        }

        public void Close() {
            if (_Index > 0) {
                Flush();
            }

            _File.Close();
        }

        public void Write7BitInt(int value) {
            uint v = (uint)value;
            while (v >= 0x80) {
                if ((_Index + 1) > _Buffer.Length) {
                    Flush();
                }
                _Buffer[_Index++] = (byte)(v | 0x80);
                v >>= 7;
            }
            if ((_Index + 1) > _Buffer.Length) {
                Flush();
            }

            _Buffer[_Index++] = (byte)v;
        }

        /// <summary>
        /// Writes a ASCII-encoded string value to the underlying stream. The data written to the stream is prefixed by the length in 7-bit int.
        /// </summary>
        public void WriteAsciiPrefix(string value) {
            if (value == null) {
                value = string.Empty;
            }
            Write7BitInt(value.Length);
            if ((_Index + value.Length) > _Buffer.Length) {
                Flush();
            }
            Encoding.ASCII.GetBytes(value, 0, value.Length, _Buffer, (int)_Index);
            _Index += value.Length;
        }

        public void Write(DateTime value) {
            Write(value.Ticks);
        }

        public void WriteDeltaTime(DateTime value) {
            long ticks = value.Ticks;
            long now = DateTime.Now.Ticks; // allowed access to DateTime.Now

            TimeSpan d;

            try {
                d = new TimeSpan(ticks - now);
            }
            catch {
                if (ticks < now) {
                    d = TimeSpan.MaxValue;
                }
                else {
                    d = TimeSpan.MaxValue;
                }
            }

            Write(d);
        }

        public void Write(TimeSpan value) {
            Write(value.Ticks);
        }

        public void Write(long value) {
            if ((_Index + 8) > _Buffer.Length) {
                Flush();
            }

            _Buffer[_Index] = (byte)value;
            _Buffer[_Index + 1] = (byte)(value >> 8);
            _Buffer[_Index + 2] = (byte)(value >> 16);
            _Buffer[_Index + 3] = (byte)(value >> 24);
            _Buffer[_Index + 4] = (byte)(value >> 32);
            _Buffer[_Index + 5] = (byte)(value >> 40);
            _Buffer[_Index + 6] = (byte)(value >> 48);
            _Buffer[_Index + 7] = (byte)(value >> 56);
            _Index += 8;
        }

        public void Write(ulong value) {
            if ((_Index + 8) > _Buffer.Length) {
                Flush();
            }

            _Buffer[_Index] = (byte)value;
            _Buffer[_Index + 1] = (byte)(value >> 8);
            _Buffer[_Index + 2] = (byte)(value >> 16);
            _Buffer[_Index + 3] = (byte)(value >> 24);
            _Buffer[_Index + 4] = (byte)(value >> 32);
            _Buffer[_Index + 5] = (byte)(value >> 40);
            _Buffer[_Index + 6] = (byte)(value >> 48);
            _Buffer[_Index + 7] = (byte)(value >> 56);
            _Index += 8;
        }

        public void Write(int value) {
            if ((_Index + 4) > _Buffer.Length) {
                Flush();
            }

            _Buffer[_Index] = (byte)value;
            _Buffer[_Index + 1] = (byte)(value >> 8);
            _Buffer[_Index + 2] = (byte)(value >> 16);
            _Buffer[_Index + 3] = (byte)(value >> 24);
            _Index += 4;
        }

        public void Write(uint value) {
            if ((_Index + 4) > _Buffer.Length) {
                Flush();
            }

            _Buffer[_Index] = (byte)value;
            _Buffer[_Index + 1] = (byte)(value >> 8);
            _Buffer[_Index + 2] = (byte)(value >> 16);
            _Buffer[_Index + 3] = (byte)(value >> 24);
            _Index += 4;
        }

        public void Write(short value) {
            if ((_Index + 2) > _Buffer.Length) {
                Flush();
            }

            _Buffer[_Index] = (byte)value;
            _Buffer[_Index + 1] = (byte)(value >> 8);
            _Index += 2;
        }

        public void Write(ushort value) {
            if ((_Index + 2) > _Buffer.Length) {
                Flush();
            }

            _Buffer[_Index] = (byte)value;
            _Buffer[_Index + 1] = (byte)(value >> 8);
            _Index += 2;
        }

        public void Write(double value) {
            Write(BitConverter.GetBytes(value));
        }

        public void Write(float value) {
            Write(BitConverter.GetBytes(value));
        }

        public void Write(char value) {
            if ((_Index + 8) > _Buffer.Length) {
                Flush();
            }

            _SingleCharBuffer[0] = value;

            int byteCount = _Encoding.GetBytes(_SingleCharBuffer, 0, 1, _Buffer, _Index);
            _Index += byteCount;
        }

        public void Write(byte value) {
            if ((_Index + 1) > _Buffer.Length) {
                Flush();
            }

            _Buffer[_Index++] = value;
        }

        public void Write(byte[] value) {
            Write(value, 0, value.Length);
        }

        public void Write(byte[] value, int begin, int length) {
            for (int i = 0; i < length; i++) {
                Write(value[i + begin]);
            }
        }

        public void Write(sbyte value) {
            if ((_Index + 1) > _Buffer.Length) {
                Flush();
            }

            _Buffer[_Index++] = (byte)value;
        }

        public void Write(bool value) {
            if ((_Index + 1) > _Buffer.Length) {
                Flush();
            }

            _Buffer[_Index++] = (byte)(value ? 1 : 0);
        }

        public void WriteFourBytes(string value) {
            if (value.Length < 4) {
                throw new Exception($"Can't serialize {value} as four bytes.");
            }
            Write((byte)value[0]);
            Write((byte)value[1]);
            Write((byte)value[2]);
            Write((byte)value[3]);
        }

        public void Dispose() {
            Close();
        }
    }
}