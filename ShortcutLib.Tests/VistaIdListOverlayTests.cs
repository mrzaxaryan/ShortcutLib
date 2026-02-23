using ShortcutLib;
using ShortcutLib.Tests.Helpers;
using Xunit;

namespace ShortcutLib.Tests;

public class VistaIdListOverlayTests
{
    [Fact]
    public void VistaIdListDataBlock_SignaturePresent()
    {
        byte[] idListData = [0x14, 0x00, 0x1F, 0x50, 0xE0, 0x4F, 0xD0, 0x20, 0xEA, 0x3A, 0x69, 0x10, 0xA2, 0xD8, 0x08, 0x00, 0x2B, 0x30, 0x30, 0x9D, 0x00, 0x00];
        byte[] lnk = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\test.exe",
            VistaIdListData = idListData
        });
        Assert.True(BinaryAssert.ContainsSignature(lnk, 0xA000000C));
    }

    [Fact]
    public void VistaIdListDataBlock_RoundTrips()
    {
        byte[] idListData = [0x14, 0x00, 0x1F, 0x50, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10, 0x00, 0x00];
        byte[] lnk = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\test.exe",
            VistaIdListData = idListData
        });
        var options = Shortcut.Open(lnk);
        Assert.NotNull(options.VistaIdListData);
        Assert.Equal(idListData, options.VistaIdListData);
    }

    [Fact]
    public void VistaIdListDataBlock_NotPresentWhenNull()
    {
        byte[] lnk = Shortcut.Create(new ShortcutOptions { Target = @"C:\test.exe" });
        Assert.False(BinaryAssert.ContainsSignature(lnk, 0xA000000C));
        var options = Shortcut.Open(lnk);
        Assert.Null(options.VistaIdListData);
    }

    [Fact]
    public void OverlayData_RoundTrips()
    {
        byte[] overlay = [0xCA, 0xFE, 0xBA, 0xBE, 0xDE, 0xAD];
        byte[] lnk = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\test.exe",
            OverlayData = overlay
        });
        var options = Shortcut.Open(lnk);
        Assert.NotNull(options.OverlayData);
        Assert.Equal(overlay, options.OverlayData);
    }

    [Fact]
    public void OverlayData_NullWhenNotSet()
    {
        byte[] lnk = Shortcut.Create(new ShortcutOptions { Target = @"C:\test.exe" });
        var options = Shortcut.Open(lnk);
        Assert.Null(options.OverlayData);
    }

    [Fact]
    public void OverlayData_EmptyArrayNotWritten()
    {
        byte[] lnk = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\test.exe",
            OverlayData = []
        });
        var options = Shortcut.Open(lnk);
        Assert.Null(options.OverlayData);
    }

    [Fact]
    public void OverlayData_LargePayload_RoundTrips()
    {
        byte[] overlay = new byte[1024];
        new Random(42).NextBytes(overlay);
        byte[] lnk = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\test.exe",
            OverlayData = overlay
        });
        var options = Shortcut.Open(lnk);
        Assert.Equal(overlay, options.OverlayData);
    }

    [Fact]
    public void VistaIdList_And_OverlayData_Combined_RoundTrip()
    {
        byte[] idListData = [0x01, 0x02, 0x03, 0x04, 0x05];
        byte[] overlay = [0xFF, 0xFE, 0xFD];

        byte[] lnk = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\test.exe",
            VistaIdListData = idListData,
            OverlayData = overlay
        });
        var options = Shortcut.Open(lnk);
        Assert.Equal(idListData, options.VistaIdListData);
        Assert.Equal(overlay, options.OverlayData);
    }
}
