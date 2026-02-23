namespace ShortcutLib;

internal static class LinkFlags
{
    internal const int HasLinkTargetIdList          = 0x00000001; // Bit 0 (A)
    internal const int HasLinkInfo                  = 0x00000002; // Bit 1 (B)
    internal const int HasName                      = 0x00000004; // Bit 2 (C)
    internal const int HasRelativePath              = 0x00000008; // Bit 3 (D)
    internal const int HasWorkingDir                = 0x00000010; // Bit 4 (E)
    internal const int HasArguments                 = 0x00000020; // Bit 5 (F)
    internal const int HasIconLocation              = 0x00000040; // Bit 6 (G)
    internal const int IsUnicode                    = 0x00000080; // Bit 7 (H)
    internal const int ForceNoLinkInfo              = 0x00000100; // Bit 8 (I)
    internal const int HasExpSz                     = 0x00000200; // Bit 9 (J)
    internal const int RunInSeparateProcess         = 0x00000400; // Bit 10 (K)
    // Bit 11 (L) is unused
    internal const int HasDarwinID                  = 0x00001000; // Bit 12 (M)
    internal const int RunAsUser                    = 0x00002000; // Bit 13 (N)
    internal const int HasExpIcon                   = 0x00004000; // Bit 14 (O)
    internal const int NoPidlAlias                  = 0x00008000; // Bit 15 (P)
    // Bit 16 (Q) is unused
    internal const int RunWithShimLayer             = 0x00020000; // Bit 17 (R)
    internal const int ForceNoLinkTrack             = 0x00040000; // Bit 18 (S)
    internal const int EnableTargetMetadata         = 0x00080000; // Bit 19 (T)
    internal const int DisableLinkPathTracking      = 0x00100000; // Bit 20 (U)
    internal const int DisableKnownFolderTracking   = 0x00200000; // Bit 21 (V)
    internal const int DisableKnownFolderAlias      = 0x00400000; // Bit 22 (W)
    internal const int AllowLinkToLink              = 0x00800000; // Bit 23 (X)
    internal const int UnaliasOnSave                = 0x01000000; // Bit 24 (Y)
    internal const int PreferEnvironmentPath        = 0x02000000; // Bit 25 (Z)
    internal const int KeepLocalIDListForUNCTarget  = 0x04000000; // Bit 26 (AA)
}
