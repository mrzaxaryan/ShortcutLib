namespace LNKLib;

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
    public LocalPathInfo? Local { get; init; }

    /// <summary>
    /// Network share information.
    /// When non-null, the CommonNetworkRelativeLinkAndPathSuffix bit is set.
    /// </summary>
    public NetworkPathInfo? Network { get; init; }
}

/// <summary>Local volume and base path for LinkInfo.</summary>
public sealed class LocalPathInfo
{
    /// <summary>Volume drive type (e.g., DRIVE_FIXED = 3).</summary>
    public uint DriveType { get; init; } = 3;

    /// <summary>Volume serial number.</summary>
    public uint DriveSerialNumber { get; init; }

    /// <summary>Volume label.</summary>
    public string VolumeLabel { get; init; } = "";

    /// <summary>Full local base path to the target.</summary>
    public required string BasePath { get; init; }
}

/// <summary>Network share information for LinkInfo.</summary>
public sealed class NetworkPathInfo
{
    /// <summary>UNC share name (e.g., "\\server\share").</summary>
    public required string ShareName { get; init; }

    /// <summary>Common path suffix appended after the share name.</summary>
    public string CommonPathSuffix { get; init; } = "";
}
