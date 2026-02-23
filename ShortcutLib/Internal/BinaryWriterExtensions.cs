using System.Text;

namespace ShortcutLib;

internal static class BinaryWriterExtensions
{
    /// <summary>
    /// Writes a 16-bit unsigned integer in little-endian byte order.
    /// </summary>
    internal static void WriteUInt16Le(this BinaryWriter writer, int value)
    {
        writer.Write((byte)(value % 256));
        writer.Write((byte)(value / 256));
    }

    /// <summary>
    /// Writes an optional string to the BinaryWriter (2-byte length then string bytes).
    /// When unicode is false, uses ANSI encoding per the .lnk spec (IS_UNICODE not set).
    /// When unicode is true, uses UTF-16LE encoding.
    /// </summary>
    internal static void WriteStringData(this BinaryWriter writer, string? value, bool unicode)
    {
        if (value is null)
            return;
        writer.WriteUInt16Le(value.Length);
        if (unicode)
            writer.Write(Encoding.Unicode.GetBytes(value));
        else
            writer.Write(Encoding.Default.GetBytes(value));
    }

}
