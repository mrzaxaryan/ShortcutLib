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

    // --- File attributes ---

    /// <summary>
    /// Explicit file attributes for the header. When null, attributes are
    /// auto-detected from the target path (Archive for files, Directory for folders).
    /// </summary>
    public FileAttributes? FileAttributes { get; set; }

    // --- Additional LinkFlags ---

    /// <summary>When true, the LinkInfo structure is ignored during resolution.</summary>
    public bool ForceNoLinkInfo { get; set; }

    /// <summary>When true, 16-bit target runs in a separate Virtual DOS Machine.</summary>
    public bool RunInSeparateProcess { get; set; }

    /// <summary>When true, the shell namespace IDList should not use an alias.</summary>
    public bool NoPidlAlias { get; set; }

    /// <summary>When true, TrackerDataBlock is ignored during resolution.</summary>
    public bool ForceNoLinkTrack { get; set; }

    /// <summary>When true, PropertyStoreDataBlock is populated when link target is set.</summary>
    public bool EnableTargetMetadata { get; set; }

    /// <summary>When true, EnvironmentVariableDataBlock is ignored during resolution.</summary>
    public bool DisableLinkPathTracking { get; set; }

    /// <summary>When true, SpecialFolder and KnownFolder data blocks are ignored on load.</summary>
    public bool DisableKnownFolderTracking { get; set; }

    /// <summary>When true, unaliased IDList form is used for known folder targets.</summary>
    public bool DisableKnownFolderAlias { get; set; }

    /// <summary>When true, allows the link target to be another .lnk file.</summary>
    public bool AllowLinkToLink { get; set; }

    /// <summary>When true, target IDList is stored in unaliased form for known folders on save.</summary>
    public bool UnaliasOnSave { get; set; }

    /// <summary>When true, local IDList is stored even when target is a UNC path.</summary>
    public bool KeepLocalIDListForUNCTarget { get; set; }

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

    /// <summary>
    /// Console display settings for ConsoleDataBlock (signature 0xA0000002).
    /// </summary>
    public ConsoleData? Console { get; set; }

    /// <summary>
    /// Far East console code page for ConsoleFEDataBlock (signature 0xA0000004).
    /// </summary>
    public uint? ConsoleCodePage { get; set; }

    /// <summary>
    /// MSI advertised shortcut descriptor for DarwinDataBlock (signature 0xA0000006).
    /// Written in the same 260-ANSI + 520-Unicode layout as environment variable blocks.
    /// </summary>
    public string? DarwinData { get; set; }

    /// <summary>
    /// Application compatibility layer name for ShimDataBlock (signature 0xA0000008).
    /// Example values: "WIN95", "WINXP", "WIN7RTM", "WIN8RTM".
    /// </summary>
    public string? ShimLayerName { get; set; }

    /// <summary>
    /// Raw IDList bytes for VistaAndAboveIDListDataBlock (signature 0xA000000C).
    /// Provides an alternative Vista+ representation of the target IDList.
    /// </summary>
    public byte[]? VistaIdListData { get; set; }

    /// <summary>
    /// Overlay data appended after the terminal block. Preserved on round-trip.
    /// </summary>
    public byte[]? OverlayData { get; set; }
}
