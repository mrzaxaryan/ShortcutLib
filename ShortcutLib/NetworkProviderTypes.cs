namespace ShortcutLib;

/// <summary>
/// Well-known network provider type constants (WNNC_NET_*) per [MS-SHLLINK] 2.3.2.
/// </summary>
public static class NetworkProviderTypes
{
    /// <summary>SMB / CIFS (Microsoft Windows Network).</summary>
    public const uint Lanman = 0x00020000;

    /// <summary>Novell NetWare.</summary>
    public const uint Netware = 0x00030000;

    /// <summary>Sun PC NFS.</summary>
    public const uint SunPcNfs = 0x00070000;

    /// <summary>Banyan VINES.</summary>
    public const uint Vines = 0x000D0000;

    /// <summary>Distributed File System.</summary>
    public const uint Dfs = 0x003B0000;

    /// <summary>Terminal Services.</summary>
    public const uint TerminalServices = 0x00360000;

    /// <summary>OpenAFS.</summary>
    public const uint OpenAfs = 0x00390000;

    /// <summary>Microsoft NFS.</summary>
    public const uint MsNfs = 0x00420000;

    /// <summary>Google file system.</summary>
    public const uint Google = 0x00430000;

    /// <summary>VMware shared folders.</summary>
    public const uint VMware = 0x00410000;
}
