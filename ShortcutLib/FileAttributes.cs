namespace ShortcutLib;

/// <summary>
/// File attribute flags for the shortcut target per [MS-SHLLINK] 2.1.
/// </summary>
[Flags]
public enum FileAttributes : uint
{
    /// <summary>Read-only file.</summary>
    ReadOnly = 0x00000001,

    /// <summary>Hidden file.</summary>
    Hidden = 0x00000002,

    /// <summary>System file.</summary>
    System = 0x00000004,

    /// <summary>Target is a directory.</summary>
    Directory = 0x00000010,

    /// <summary>Archive flag (normal file).</summary>
    Archive = 0x00000020,

    /// <summary>No other attributes are set.</summary>
    Normal = 0x00000080,

    /// <summary>Temporary file.</summary>
    Temporary = 0x00000100,

    /// <summary>Sparse file.</summary>
    SparseFile = 0x00000200,

    /// <summary>Has reparse point data.</summary>
    ReparsePoint = 0x00000400,

    /// <summary>Compressed file or directory.</summary>
    Compressed = 0x00000800,

    /// <summary>Offline storage.</summary>
    Offline = 0x00001000,

    /// <summary>Not indexed by content indexing service.</summary>
    NotContentIndexed = 0x00002000,

    /// <summary>Encrypted (EFS).</summary>
    Encrypted = 0x00004000,
}
