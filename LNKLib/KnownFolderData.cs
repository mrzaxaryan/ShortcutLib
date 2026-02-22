namespace LNKLib;

/// <summary>
/// Data for KnownFolderDataBlock (signature 0xA000000B).
/// </summary>
public sealed class KnownFolderData
{
    /// <summary>GUID of the known folder.</summary>
    public required Guid FolderId { get; init; }

    /// <summary>Offset into the IDList.</summary>
    public uint Offset { get; init; }
}
