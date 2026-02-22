namespace LNKLib;

/// <summary>
/// Configuration for creating a Windows Shell Link (.lnk) file.
/// </summary>
public sealed class ShortcutOptions
{
    /// <summary>Target path. Required.</summary>
    public required string Target { get; init; }

    /// <summary>Command-line arguments.</summary>
    public string? Arguments { get; init; }

    /// <summary>Pad arguments to 31 KB buffer to hide them from the properties UI.</summary>
    public bool PadArguments { get; init; }

    /// <summary>Icon file path.</summary>
    public string? IconLocation { get; init; }

    /// <summary>Icon index within file.</summary>
    public int IconIndex { get; init; }

    /// <summary>Shortcut description.</summary>
    public string? Description { get; init; }

    /// <summary>Working directory.</summary>
    public string? WorkingDirectory { get; init; }

    /// <summary>Treat target as a network printer.</summary>
    public bool IsPrinterLink { get; init; }

    /// <summary>Initial window state.</summary>
    public ShortcutWindowStyle WindowStyle { get; init; } = ShortcutWindowStyle.Normal;

    /// <summary>Run target as administrator.</summary>
    public bool RunAsAdmin { get; init; }

    /// <summary>Virtual key code for hotkey (0 = none).</summary>
    public byte HotkeyKey { get; init; }

    /// <summary>Hotkey modifier keys.</summary>
    public HotkeyModifiers HotkeyModifiers { get; init; }

    // --- Core improvements ---

    /// <summary>
    /// When true, write string data fields as Unicode (sets IS_UNICODE flag).
    /// Default is false (ANSI encoding).
    /// </summary>
    public bool UseUnicode { get; init; }

    /// <summary>Target file creation time. Null = zero.</summary>
    public DateTime? CreationTime { get; init; }

    /// <summary>Target file last access time. Null = zero.</summary>
    public DateTime? AccessTime { get; init; }

    /// <summary>Target file last write time. Null = zero.</summary>
    public DateTime? WriteTime { get; init; }

    /// <summary>Target file size in bytes. Default 0.</summary>
    public uint FileSize { get; init; }

    /// <summary>
    /// Relative path from the .lnk file location to the target.
    /// When non-null, sets HasRelativePath flag.
    /// </summary>
    public string? RelativePath { get; init; }

    /// <summary>
    /// LinkInfo configuration. When non-null, writes the LinkInfo structure.
    /// </summary>
    public LinkInfo? LinkInfo { get; init; }

    // --- Extra data blocks ---

    /// <summary>
    /// Icon path with environment variables. Written as IconEnvironmentDataBlock.
    /// </summary>
    public string? IconEnvironmentPath { get; init; }

    /// <summary>
    /// Known folder data block configuration.
    /// </summary>
    public KnownFolderData? KnownFolder { get; init; }

    /// <summary>
    /// Distributed link tracker data.
    /// </summary>
    public TrackerData? Tracker { get; init; }

    /// <summary>
    /// Raw serialized property store bytes. The library wraps them with the block header.
    /// </summary>
    public byte[]? PropertyStoreData { get; init; }

    /// <summary>
    /// Special folder data block configuration.
    /// </summary>
    public SpecialFolderData? SpecialFolder { get; init; }
}
