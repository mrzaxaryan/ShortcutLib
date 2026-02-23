using ShortcutLib;
using ShortcutLib.Tests.Helpers;
using Xunit;

namespace ShortcutLib.Tests;

public class DarwinShimDataBlockTests
{
    [Fact]
    public void DarwinDataBlock_SignaturePresent()
    {
        byte[] lnk = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\test.exe",
            DarwinData = "[ProductCode]>Feature>Component"
        });
        Assert.True(BinaryAssert.ContainsSignature(lnk, 0xA0000006));
    }

    [Fact]
    public void DarwinDataBlock_BlockSizeIs788()
    {
        byte[] lnk = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\test.exe",
            DarwinData = "TestDarwin"
        });
        int offset = BinaryAssert.FindSignatureOffset(lnk, 0xA0000006);
        Assert.True(offset >= 4);
        uint blockSize = BitConverter.ToUInt32(lnk, offset - 4);
        Assert.Equal(788u, blockSize);
    }

    [Fact]
    public void DarwinDataBlock_RoundTrips()
    {
        string darwinStr = "[ProductCode]>Feature>Component";
        byte[] lnk = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\test.exe",
            DarwinData = darwinStr
        });
        var options = Shortcut.Open(lnk);
        Assert.Equal(darwinStr, options.DarwinData);
    }

    [Fact]
    public void DarwinDataBlock_NotPresentWhenNull()
    {
        byte[] lnk = Shortcut.Create(new ShortcutOptions { Target = @"C:\test.exe" });
        Assert.False(BinaryAssert.ContainsSignature(lnk, 0xA0000006));
        var options = Shortcut.Open(lnk);
        Assert.Null(options.DarwinData);
    }

    [Fact]
    public void DarwinDataBlock_SetsHasDarwinIDFlag()
    {
        byte[] lnk = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\test.exe",
            DarwinData = "test"
        });
        // LinkFlags at offset 20 (after 4-byte HeaderSize + 16-byte CLSID)
        uint flags = BitConverter.ToUInt32(lnk, 20);
        Assert.True((flags & 0x00001000) != 0, "HasDarwinID flag should be set");
    }

    [Fact]
    public void ShimDataBlock_SignaturePresent()
    {
        byte[] lnk = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\test.exe",
            ShimLayerName = "WINXP"
        });
        Assert.True(BinaryAssert.ContainsSignature(lnk, 0xA0000008));
    }

    [Fact]
    public void ShimDataBlock_RoundTrips()
    {
        byte[] lnk = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\test.exe",
            ShimLayerName = "WIN7RTM"
        });
        var options = Shortcut.Open(lnk);
        Assert.Equal("WIN7RTM", options.ShimLayerName);
    }

    [Fact]
    public void ShimDataBlock_SetsRunWithShimLayerFlag()
    {
        byte[] lnk = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\test.exe",
            ShimLayerName = "WINXP"
        });
        uint flags = BitConverter.ToUInt32(lnk, 20);
        Assert.True((flags & 0x00020000) != 0, "RunWithShimLayer flag should be set");
    }

    [Fact]
    public void ShimDataBlock_NotPresentWhenNull()
    {
        byte[] lnk = Shortcut.Create(new ShortcutOptions { Target = @"C:\test.exe" });
        Assert.False(BinaryAssert.ContainsSignature(lnk, 0xA0000008));
        var options = Shortcut.Open(lnk);
        Assert.Null(options.ShimLayerName);
    }

    [Fact]
    public void DarwinAndShim_Combined_RoundTrip()
    {
        byte[] lnk = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\OldApp\app.exe",
            DarwinData = "[Product]>Feature",
            ShimLayerName = "WIN95"
        });
        var options = Shortcut.Open(lnk);
        Assert.Equal("[Product]>Feature", options.DarwinData);
        Assert.Equal("WIN95", options.ShimLayerName);
    }
}
