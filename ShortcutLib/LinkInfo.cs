namespace ShortcutLib;

/// <summary>
/// Describes the target's location information per [MS-SHLLINK] 2.3.
/// Provide either Local or Network (or both).
/// </summary>
public sealed class LinkInfo
{
    /// <summary>
    /// Local volume information and base path.
    /// When non-null, the VolumeIDAndLocalBasePath bit is set.
    /// </summary>
    public LocalPathInfo? Local { get; set; }

    /// <summary>
    /// Network share information.
    /// When non-null, the CommonNetworkRelativeLinkAndPathSuffix bit is set.
    /// </summary>
    public NetworkPathInfo? Network { get; set; }
}

/// <summary>Local volume and base path for LinkInfo.</summary>
public sealed class LocalPathInfo
{
    /// <summary>Volume drive type (e.g., DRIVE_FIXED = 3).</summary>
    public uint DriveType { get; set; } = 3;

    /// <summary>Volume serial number.</summary>
    public uint DriveSerialNumber { get; set; }

    /// <summary>Volume label.</summary>
    public string VolumeLabel { get; set; } = "";

    /// <summary>Full local base path to the target.</summary>
    public string BasePath { get; set; } = "";
}

/// <summary>Network share information for LinkInfo.</summary>
public sealed class NetworkPathInfo
{
    /// <summary>UNC share name (e.g., "\\server\share").</summary>
    public string ShareName { get; set; } = "";

    /// <summary>Common path suffix appended after the share name.</summary>
    public string CommonPathSuffix { get; set; } = "";
}
