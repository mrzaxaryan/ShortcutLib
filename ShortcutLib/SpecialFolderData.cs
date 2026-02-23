namespace ShortcutLib;

/// <summary>
/// Data for SpecialFolderDataBlock (signature 0xA0000005).
/// </summary>
public sealed class SpecialFolderData
{
    /// <summary>CSIDL value identifying the special folder.</summary>
    public uint FolderId { get; set; }

    /// <summary>Offset into the IDList.</summary>
    public uint Offset { get; set; }
}
