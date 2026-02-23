namespace ShortcutLib;

/// <summary>
/// Console display settings for ConsoleDataBlock (signature 0xA0000002).
/// Per [MS-SHLLINK] 2.5.1. Fixed size: 204 bytes.
/// </summary>
public sealed class ConsoleData
{
    /// <summary>Fill (text) attributes: foreground/background color flags.</summary>
    public ushort FillAttributes { get; set; }

    /// <summary>Popup fill attributes: foreground/background for popups.</summary>
    public ushort PopupFillAttributes { get; set; }

    /// <summary>Screen buffer width in columns.</summary>
    public short ScreenBufferSizeX { get; set; } = 80;

    /// <summary>Screen buffer height in rows.</summary>
    public short ScreenBufferSizeY { get; set; } = 300;

    /// <summary>Console window width in columns.</summary>
    public short WindowSizeX { get; set; } = 80;

    /// <summary>Console window height in rows.</summary>
    public short WindowSizeY { get; set; } = 25;

    /// <summary>Window origin X position in pixels.</summary>
    public short WindowOriginX { get; set; }

    /// <summary>Window origin Y position in pixels.</summary>
    public short WindowOriginY { get; set; }

    /// <summary>Font size. High word = height, low word = width.</summary>
    public uint FontSize { get; set; }

    /// <summary>Font family and pitch flags (FF_* and TMPF_* values).</summary>
    public uint FontFamily { get; set; }

    /// <summary>Font weight (400 = normal, 700 = bold).</summary>
    public uint FontWeight { get; set; }

    /// <summary>Console font face name. Max 31 characters (stored as 32 WCHARs = 64 bytes).</summary>
    public string FaceName { get; set; } = "Consolas";

    /// <summary>Cursor size percentage: small (1-25), medium (26-50), large (51-100).</summary>
    public uint CursorSize { get; set; } = 25;

    /// <summary>Full screen mode.</summary>
    public bool FullScreen { get; set; }

    /// <summary>QuickEdit mode (mouse selection for copy/paste).</summary>
    public bool QuickEdit { get; set; }

    /// <summary>Insert mode for text input.</summary>
    public bool InsertMode { get; set; }

    /// <summary>Auto-position the console window.</summary>
    public bool AutoPosition { get; set; } = true;

    /// <summary>Command history buffer size.</summary>
    public uint HistoryBufferSize { get; set; } = 50;

    /// <summary>Number of command history buffers.</summary>
    public uint NumberOfHistoryBuffers { get; set; } = 4;

    /// <summary>Remove duplicate entries from history.</summary>
    public bool HistoryNoDup { get; set; }

    /// <summary>
    /// 16-entry color table. Each value is 0x00BBGGRR.
    /// Default is the classic Windows console palette.
    /// </summary>
    public uint[] ColorTable { get; set; } =
    [
        0x00000000, 0x00800000, 0x00008000, 0x00808000,
        0x00000080, 0x00800080, 0x00008080, 0x00C0C0C0,
        0x00808080, 0x00FF0000, 0x0000FF00, 0x00FFFF00,
        0x000000FF, 0x00FF00FF, 0x0000FFFF, 0x00FFFFFF,
    ];
}
