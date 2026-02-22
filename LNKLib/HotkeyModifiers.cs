namespace LNKLib;

/// <summary>
/// Modifier keys for a shortcut hotkey combination.
/// </summary>
[Flags]
public enum HotkeyModifiers : byte
{
    /// <summary>No modifier keys.</summary>
    None = 0x00,

    /// <summary>Shift key.</summary>
    Shift = 0x01,

    /// <summary>Control key.</summary>
    Control = 0x02,

    /// <summary>Alt key.</summary>
    Alt = 0x04
}
