using System.Text;

namespace ShortcutLib;

internal static class BinaryReaderExtensions
{
    internal static string ReadNullTerminatedAnsiString(this BinaryReader reader)
    {
        var bytes = new List<byte>();
        while (true)
        {
            byte b = reader.ReadByte();
            if (b == 0) break;
            bytes.Add(b);
        }
        return Encoding.Default.GetString(bytes.ToArray());
    }

    internal static string ReadFixedAnsiString(this BinaryReader reader, int bufferSize)
    {
        byte[] buffer = reader.ReadBytes(bufferSize);
        int end = Array.IndexOf(buffer, (byte)0);
        if (end < 0) end = bufferSize;
        return Encoding.Default.GetString(buffer, 0, end);
    }

    internal static string ReadFixedUnicodeString(this BinaryReader reader, int byteCount)
    {
        byte[] buffer = reader.ReadBytes(byteCount);
        return Encoding.Unicode.GetString(buffer).TrimEnd('\0');
    }
}
