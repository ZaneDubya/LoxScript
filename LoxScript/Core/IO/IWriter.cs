namespace XPT.Core.IO {
    public interface IWriter {
        void Write(bool value);
        void Write(byte value);
        void Write(sbyte value);
        void Write(short value);
        void Write(ushort value);
        void Write(int value);
        void Write(uint value);
        void Write(long value);
        void Write(ulong value);
        void Write7BitInt(int value);
        void WriteAsciiPrefix(string value);
        void WriteFourBytes(string value);
        void Write(byte[] value);
        long Position { get; }
        void Close();
    }
}
