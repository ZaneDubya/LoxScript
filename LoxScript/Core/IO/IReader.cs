using System.IO;

namespace XPT.Core.IO {
    public interface IReader {
        long ReadLong();
        int ReadInt();
        short ReadShort();
        byte ReadByte();
        uint ReadUInt();
        ushort ReadUShort();
        sbyte ReadSByte();
        bool ReadBool();
        int Read7BitInt();
        string ReadAsciiPrefix();
        byte[] ReadBytes(int count);
        bool ReadFourBytes(string value);
        ushort[] ReadUShorts(int count);
        long Position { get; set; }
        long Length { get; }
        void Close();
        long Seek(long offset, SeekOrigin origin);
    }
}
