using System.Text;

namespace ShortcutLib;

/// <summary>
/// Builds a serialized MS-PROPSTORE byte array for use with
/// <see cref="ShortcutOptions.PropertyStoreData"/>.
/// </summary>
public sealed class PropertyStoreBuilder
{
    // Well-known format IDs
    private static readonly Guid AppUserModelFormatId =
        new("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3");

    // Property IDs under AppUserModelFormatId
    private const uint PID_RelaunchCommand = 2;
    private const uint PID_RelaunchIconResource = 3;
    private const uint PID_RelaunchDisplayNameResource = 4;
    private const uint PID_AppUserModelId = 5;
    private const uint PID_IsDestListSeparator = 6;
    private const uint PID_ExcludeFromShowInNewInstall = 8;
    private const uint PID_PreventPinning = 9;
    private const uint PID_ToastActivatorCLSID = 26;

    // VT type constants from MS-OLEPS
    private const ushort VT_LPWSTR = 31;
    private const ushort VT_BOOL = 11;
    private const ushort VT_CLSID = 72;

    /// <summary>AppUserModel.ID (string). Controls taskbar grouping, jump lists, and toast notifications.</summary>
    public string? AppUserModelId { get; set; }

    /// <summary>Toast activator CLSID for COM notification activation.</summary>
    public Guid? ToastActivatorCLSID { get; set; }

    /// <summary>Prevents the shortcut from being pinned to taskbar or Start.</summary>
    public bool? PreventPinning { get; set; }

    /// <summary>Relaunch command for taskbar pinning.</summary>
    public string? RelaunchCommand { get; set; }

    /// <summary>Display name resource for relaunch.</summary>
    public string? RelaunchDisplayNameResource { get; set; }

    /// <summary>Icon resource for relaunch.</summary>
    public string? RelaunchIconResource { get; set; }

    /// <summary>Excludes from "New programs" highlight in Start Menu.</summary>
    public bool? ExcludeFromShowInNewInstall { get; set; }

    /// <summary>Marks this as a destination list separator in a jump list.</summary>
    public bool? IsDestListSeparator { get; set; }

    /// <summary>
    /// Serializes all set properties into the MS-PROPSTORE binary format.
    /// Returns the byte array suitable for <see cref="ShortcutOptions.PropertyStoreData"/>.
    /// </summary>
    public byte[] Build()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        WriteAppUserModelStorage(writer);

        // Terminal: 0-size storage
        writer.Write(0);

        writer.Flush();
        return ms.ToArray();
    }

    private void WriteAppUserModelStorage(BinaryWriter writer)
    {
        var entries = new List<(uint propertyId, byte[] serializedValue)>();

        if (RelaunchCommand != null)
            entries.Add((PID_RelaunchCommand, SerializeString(RelaunchCommand)));
        if (RelaunchIconResource != null)
            entries.Add((PID_RelaunchIconResource, SerializeString(RelaunchIconResource)));
        if (RelaunchDisplayNameResource != null)
            entries.Add((PID_RelaunchDisplayNameResource, SerializeString(RelaunchDisplayNameResource)));
        if (AppUserModelId != null)
            entries.Add((PID_AppUserModelId, SerializeString(AppUserModelId)));
        if (IsDestListSeparator.HasValue)
            entries.Add((PID_IsDestListSeparator, SerializeBool(IsDestListSeparator.Value)));
        if (ExcludeFromShowInNewInstall.HasValue)
            entries.Add((PID_ExcludeFromShowInNewInstall, SerializeBool(ExcludeFromShowInNewInstall.Value)));
        if (PreventPinning.HasValue)
            entries.Add((PID_PreventPinning, SerializeBool(PreventPinning.Value)));
        if (ToastActivatorCLSID.HasValue)
            entries.Add((PID_ToastActivatorCLSID, SerializeClsid(ToastActivatorCLSID.Value)));

        if (entries.Count == 0) return;

        WriteStorage(writer, AppUserModelFormatId, entries);
    }

    private static void WriteStorage(BinaryWriter writer, Guid formatId, List<(uint pid, byte[] value)> entries)
    {
        // Build the storage body first to compute total size
        using var storageMs = new MemoryStream();
        using var storageWriter = new BinaryWriter(storageMs);

        // Version: "1SPS" = 0x53505331
        storageWriter.Write(0x53505331u);
        // FormatID (16 bytes)
        storageWriter.Write(formatId.ToByteArray());

        // Write each property entry
        foreach (var (pid, value) in entries)
        {
            // ValueSize includes itself (4) + nameOrId (4) + reserved (1) + value bytes
            int valueSize = 4 + 4 + 1 + value.Length;
            storageWriter.Write(valueSize);
            storageWriter.Write(pid);
            storageWriter.Write((byte)0); // Reserved
            storageWriter.Write(value);
        }

        // Terminal entry: ValueSize = 0
        storageWriter.Write(0);

        storageWriter.Flush();
        byte[] storageData = storageMs.ToArray();

        // StorageSize = 4 (this field) + storageData.Length
        writer.Write(4 + storageData.Length);
        writer.Write(storageData);
    }

    private static byte[] SerializeString(string value)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write(VT_LPWSTR);
        w.Write((ushort)0); // padding
        byte[] strBytes = Encoding.Unicode.GetBytes(value + "\0");
        w.Write(strBytes.Length);
        w.Write(strBytes);
        w.Flush();
        return ms.ToArray();
    }

    private static byte[] SerializeBool(bool value)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write(VT_BOOL);
        w.Write((ushort)0); // padding
        w.Write(value ? (short)-1 : (short)0); // VT_BOOL: -1 = true, 0 = false
        w.Write((short)0); // padding to 4-byte boundary
        w.Flush();
        return ms.ToArray();
    }

    private static byte[] SerializeClsid(Guid value)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write(VT_CLSID);
        w.Write((ushort)0); // padding
        w.Write(value.ToByteArray());
        w.Flush();
        return ms.ToArray();
    }
}
