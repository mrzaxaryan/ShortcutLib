namespace ShortcutLib;

internal static class LnkConstants
{
    internal const int MaxPath = 260;

    internal const uint EnvVarBlockSignature = 0xA0000001;
    internal const uint ConsoleBlockSignature = 0xA0000002;
    internal const uint TrackerBlockSignature = 0xA0000003;
    internal const uint ConsoleFEBlockSignature = 0xA0000004;
    internal const uint SpecialFolderBlockSignature = 0xA0000005;
    internal const uint DarwinBlockSignature = 0xA0000006;
    internal const uint IconEnvBlockSignature = 0xA0000007;
    internal const uint ShimBlockSignature = 0xA0000008;
    internal const uint PropertyStoreBlockSignature = 0xA0000009;
    internal const uint KnownFolderBlockSignature = 0xA000000B;
    internal const uint VistaIdListBlockSignature = 0xA000000C;

    internal static readonly Guid LinkClsid = new("00021401-0000-0000-c000-000000000046");
    internal static readonly Guid ComputerClsid = new("20d04fe0-3aea-1069-a2d8-08002b30309d");
    internal static readonly Guid NetworkClsid = new("208d2c60-3aea-1069-a2d7-08002b30309d");
}
