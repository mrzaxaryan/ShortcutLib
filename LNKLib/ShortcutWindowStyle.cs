namespace LNKLib;

/// <summary>
/// Specifies the initial window state when the shortcut target is launched.
/// </summary>
public enum ShortcutWindowStyle
{
    /// <summary>Normal window (SW_SHOWNORMAL).</summary>
    Normal = 1,

    /// <summary>Maximized window (SW_SHOWMAXIMIZED).</summary>
    Maximized = 3,

    /// <summary>Minimized window (SW_SHOWMINNOACTIVE).</summary>
    Minimized = 7
}
