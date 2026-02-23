namespace ShortcutLib;

/// <summary>
/// Configuration for creating a Windows Shell Link (.lnk) file.
/// </summary>
public sealed class ShortcutOptions
{
    /// <summary>Target path. Required.</summary>
    public string Target { get; set; } = "";

    /// <summary>Command-line arguments.</summary>
    public string? Arguments { get; set; }

    /// <summary>Icon file path.</summary>
    public string? IconLocation { get; set; }

    /// <summary>Icon index within file.</summary>
    public int IconIndex { get; set; }

    /// <summary>Shortcut description.</summary>
    public string? Description { get; set; }

    /// <summary>Working directory.</summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>Treat target as a network printer.</summary>
    public bool IsPrinterLink { get; set; }

    /// <summary>Initial window state.</summary>
    public ShortcutWindowStyle WindowStyle { get; set; } = ShortcutWindowStyle.Normal;

    /// <summary>Run target as administrator.</summary>
    public bool RunAsAdmin { get; set; }

    /// <summary>Virtual key code for hotkey (0 = none).</summary>
    public byte HotkeyKey { get; set; }

    /// <summary>Hotkey modifier keys.</summary>
    public HotkeyModifiers HotkeyModifiers { get; set; }

    // --- Core improvements ---

    /// <summary>
    /// When true, write string data fields as Unicode (sets IS_UNICODE flag).
    /// Default is false (ANSI encoding).
    /// </summary>
    public bool UseUnicode { get; set; }

    /// <summary>Target file creation time. Null = zero.</summary>
    public DateTime? CreationTime { get; set; }

    /// <summary>Target file last access time. Null = zero.</summary>
    public DateTime? AccessTime { get; set; }

    /// <summary>Target file last write time. Null = zero.</summary>
    public DateTime? WriteTime { get; set; }

    /// <summary>Target file size in bytes. Default 0.</summary>
    public uint FileSize { get; set; }

    /// <summary>
    /// Relative path from the .lnk file location to the target.
    /// When non-null, sets HasRelativePath flag.
    /// </summary>
    public string? RelativePath { get; set; }

    /// <summary>
    /// LinkInfo configuration. When non-null, writes the LinkInfo structure.
    /// </summary>
    public LinkInfo? LinkInfo { get; set; }

    // --- Extra data blocks ---

    /// <summary>
    /// Icon path with environment variables. Written as IconEnvironmentDataBlock.
    /// </summary>
    public string? IconEnvironmentPath { get; set; }

    /// <summary>
    /// Known folder data block configuration.
    /// </summary>
    public KnownFolderData? KnownFolder { get; set; }

    /// <summary>
    /// Distributed link tracker data.
    /// </summary>
    public TrackerData? Tracker { get; set; }

    /// <summary>
    /// Raw serialized property store bytes. The library wraps them with the block header.
    /// </summary>
    public byte[]? PropertyStoreData { get; set; }

    /// <summary>
    /// Special folder data block configuration.
    /// </summary>
    public SpecialFolderData? SpecialFolder { get; set; }
}
