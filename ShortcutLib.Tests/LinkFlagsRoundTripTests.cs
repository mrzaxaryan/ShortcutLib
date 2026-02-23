using ShortcutLib;
using Xunit;

namespace ShortcutLib.Tests;

public class LinkFlagsRoundTripTests
{
    [Fact]
    public void ForceNoLinkInfo_RoundTrips()
    {
        byte[] lnk = Shortcut.Create(new ShortcutOptions { Target = @"C:\test.exe", ForceNoLinkInfo = true });
        var options = Shortcut.Open(lnk);
        Assert.True(options.ForceNoLinkInfo);
    }

    [Fact]
    public void ForceNoLinkInfo_DefaultFalse()
    {
        byte[] lnk = Shortcut.Create(new ShortcutOptions { Target = @"C:\test.exe" });
        var options = Shortcut.Open(lnk);
        Assert.False(options.ForceNoLinkInfo);
    }

    [Fact]
    public void RunInSeparateProcess_RoundTrips()
    {
        byte[] lnk = Shortcut.Create(new ShortcutOptions { Target = @"C:\test.exe", RunInSeparateProcess = true });
        var options = Shortcut.Open(lnk);
        Assert.True(options.RunInSeparateProcess);
    }

    [Fact]
    public void NoPidlAlias_RoundTrips()
    {
        byte[] lnk = Shortcut.Create(new ShortcutOptions { Target = @"C:\test.exe", NoPidlAlias = true });
        var options = Shortcut.Open(lnk);
        Assert.True(options.NoPidlAlias);
    }

    [Fact]
    public void ForceNoLinkTrack_RoundTrips()
    {
        byte[] lnk = Shortcut.Create(new ShortcutOptions { Target = @"C:\test.exe", ForceNoLinkTrack = true });
        var options = Shortcut.Open(lnk);
        Assert.True(options.ForceNoLinkTrack);
    }

    [Fact]
    public void EnableTargetMetadata_RoundTrips()
    {
        byte[] lnk = Shortcut.Create(new ShortcutOptions { Target = @"C:\test.exe", EnableTargetMetadata = true });
        var options = Shortcut.Open(lnk);
        Assert.True(options.EnableTargetMetadata);
    }

    [Fact]
    public void DisableLinkPathTracking_RoundTrips()
    {
        byte[] lnk = Shortcut.Create(new ShortcutOptions { Target = @"C:\test.exe", DisableLinkPathTracking = true });
        var options = Shortcut.Open(lnk);
        Assert.True(options.DisableLinkPathTracking);
    }

    [Fact]
    public void DisableKnownFolderTracking_RoundTrips()
    {
        byte[] lnk = Shortcut.Create(new ShortcutOptions { Target = @"C:\test.exe", DisableKnownFolderTracking = true });
        var options = Shortcut.Open(lnk);
        Assert.True(options.DisableKnownFolderTracking);
    }

    [Fact]
    public void DisableKnownFolderAlias_RoundTrips()
    {
        byte[] lnk = Shortcut.Create(new ShortcutOptions { Target = @"C:\test.exe", DisableKnownFolderAlias = true });
        var options = Shortcut.Open(lnk);
        Assert.True(options.DisableKnownFolderAlias);
    }

    [Fact]
    public void AllowLinkToLink_RoundTrips()
    {
        byte[] lnk = Shortcut.Create(new ShortcutOptions { Target = @"C:\test.exe", AllowLinkToLink = true });
        var options = Shortcut.Open(lnk);
        Assert.True(options.AllowLinkToLink);
    }

    [Fact]
    public void UnaliasOnSave_RoundTrips()
    {
        byte[] lnk = Shortcut.Create(new ShortcutOptions { Target = @"C:\test.exe", UnaliasOnSave = true });
        var options = Shortcut.Open(lnk);
        Assert.True(options.UnaliasOnSave);
    }

    [Fact]
    public void KeepLocalIDListForUNCTarget_RoundTrips()
    {
        byte[] lnk = Shortcut.Create(new ShortcutOptions { Target = @"C:\test.exe", KeepLocalIDListForUNCTarget = true });
        var options = Shortcut.Open(lnk);
        Assert.True(options.KeepLocalIDListForUNCTarget);
    }

    [Fact]
    public void MultipleFlags_RoundTrip()
    {
        byte[] lnk = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\test.exe",
            AllowLinkToLink = true,
            ForceNoLinkTrack = true,
            EnableTargetMetadata = true,
            NoPidlAlias = true
        });
        var options = Shortcut.Open(lnk);
        Assert.True(options.AllowLinkToLink);
        Assert.True(options.ForceNoLinkTrack);
        Assert.True(options.EnableTargetMetadata);
        Assert.True(options.NoPidlAlias);
        Assert.False(options.RunInSeparateProcess);
    }

    [Fact]
    public void FileAttributes_ExplicitValue_RoundTrips()
    {
        byte[] lnk = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\test.exe",
            FileAttributes = FileAttributes.Hidden | FileAttributes.System | FileAttributes.Archive
        });
        var options = Shortcut.Open(lnk);
        Assert.Equal(FileAttributes.Hidden | FileAttributes.System | FileAttributes.Archive, options.FileAttributes);
    }

    [Fact]
    public void FileAttributes_AutoDetectedFile_ReturnsArchive()
    {
        byte[] lnk = Shortcut.Create(new ShortcutOptions { Target = @"C:\test.exe" });
        var options = Shortcut.Open(lnk);
        Assert.Equal(FileAttributes.Archive, options.FileAttributes);
    }

    [Fact]
    public void FileAttributes_AutoDetectedDirectory_ReturnsDirectory()
    {
        byte[] lnk = Shortcut.Create(new ShortcutOptions { Target = @"C:\MyFolder" });
        var options = Shortcut.Open(lnk);
        Assert.Equal(FileAttributes.Directory, options.FileAttributes);
    }

    [Fact]
    public void FileAttributes_ReadOnly_RoundTrips()
    {
        byte[] lnk = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\test.exe",
            FileAttributes = FileAttributes.ReadOnly | FileAttributes.Archive
        });
        var options = Shortcut.Open(lnk);
        Assert.True(options.FileAttributes!.Value.HasFlag(FileAttributes.ReadOnly));
        Assert.True(options.FileAttributes!.Value.HasFlag(FileAttributes.Archive));
    }

    [Fact]
    public void FileAttributes_Encrypted_RoundTrips()
    {
        byte[] lnk = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\secret.doc",
            FileAttributes = FileAttributes.Encrypted | FileAttributes.Archive
        });
        var options = Shortcut.Open(lnk);
        Assert.True(options.FileAttributes!.Value.HasFlag(FileAttributes.Encrypted));
    }
}
