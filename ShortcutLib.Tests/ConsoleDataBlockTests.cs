using ShortcutLib;
using ShortcutLib.Tests.Helpers;
using Xunit;

namespace ShortcutLib.Tests;

public class ConsoleDataBlockTests
{
    [Fact]
    public void ConsoleDataBlock_SignaturePresent()
    {
        byte[] lnk = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\System32\cmd.exe",
            Console = new ConsoleData()
        });
        Assert.True(BinaryAssert.ContainsSignature(lnk, 0xA0000002));
    }

    [Fact]
    public void ConsoleDataBlock_BlockSizeIs204()
    {
        byte[] lnk = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\System32\cmd.exe",
            Console = new ConsoleData()
        });
        int offset = BinaryAssert.FindSignatureOffset(lnk, 0xA0000002);
        Assert.True(offset >= 4);
        uint blockSize = BitConverter.ToUInt32(lnk, offset - 4);
        Assert.Equal(204u, blockSize);
    }

    [Fact]
    public void ConsoleDataBlock_ScreenBufferSize_RoundTrips()
    {
        byte[] lnk = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\System32\cmd.exe",
            Console = new ConsoleData { ScreenBufferSizeX = 120, ScreenBufferSizeY = 9001 }
        });
        var options = Shortcut.Open(lnk);
        Assert.NotNull(options.Console);
        Assert.Equal(120, options.Console!.ScreenBufferSizeX);
        Assert.Equal(9001, options.Console.ScreenBufferSizeY);
    }

    [Fact]
    public void ConsoleDataBlock_WindowSize_RoundTrips()
    {
        byte[] lnk = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\System32\cmd.exe",
            Console = new ConsoleData { WindowSizeX = 100, WindowSizeY = 40 }
        });
        var options = Shortcut.Open(lnk);
        Assert.Equal(100, options.Console!.WindowSizeX);
        Assert.Equal(40, options.Console.WindowSizeY);
    }

    [Fact]
    public void ConsoleDataBlock_FaceName_RoundTrips()
    {
        byte[] lnk = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\System32\cmd.exe",
            Console = new ConsoleData { FaceName = "Lucida Console" }
        });
        var options = Shortcut.Open(lnk);
        Assert.Equal("Lucida Console", options.Console!.FaceName);
    }

    [Fact]
    public void ConsoleDataBlock_BooleanFields_RoundTrip()
    {
        byte[] lnk = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\System32\cmd.exe",
            Console = new ConsoleData
            {
                FullScreen = true,
                QuickEdit = true,
                InsertMode = true,
                AutoPosition = false,
                HistoryNoDup = true
            }
        });
        var options = Shortcut.Open(lnk);
        Assert.True(options.Console!.FullScreen);
        Assert.True(options.Console.QuickEdit);
        Assert.True(options.Console.InsertMode);
        Assert.False(options.Console.AutoPosition);
        Assert.True(options.Console.HistoryNoDup);
    }

    [Fact]
    public void ConsoleDataBlock_FillAttributes_RoundTrips()
    {
        byte[] lnk = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\System32\cmd.exe",
            Console = new ConsoleData { FillAttributes = 0x0007, PopupFillAttributes = 0x00F5 }
        });
        var options = Shortcut.Open(lnk);
        Assert.Equal(0x0007, options.Console!.FillAttributes);
        Assert.Equal(0x00F5, options.Console.PopupFillAttributes);
    }

    [Fact]
    public void ConsoleDataBlock_FontFields_RoundTrip()
    {
        byte[] lnk = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\System32\cmd.exe",
            Console = new ConsoleData
            {
                FontSize = 0x000E0000,
                FontFamily = 0x36,
                FontWeight = 700
            }
        });
        var options = Shortcut.Open(lnk);
        Assert.Equal(0x000E0000u, options.Console!.FontSize);
        Assert.Equal(0x36u, options.Console.FontFamily);
        Assert.Equal(700u, options.Console.FontWeight);
    }

    [Fact]
    public void ConsoleDataBlock_HistoryFields_RoundTrip()
    {
        byte[] lnk = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\System32\cmd.exe",
            Console = new ConsoleData
            {
                CursorSize = 50,
                HistoryBufferSize = 100,
                NumberOfHistoryBuffers = 8
            }
        });
        var options = Shortcut.Open(lnk);
        Assert.Equal(50u, options.Console!.CursorSize);
        Assert.Equal(100u, options.Console.HistoryBufferSize);
        Assert.Equal(8u, options.Console.NumberOfHistoryBuffers);
    }

    [Fact]
    public void ConsoleDataBlock_ColorTable_RoundTrips()
    {
        var colorTable = new uint[16];
        for (int i = 0; i < 16; i++)
            colorTable[i] = (uint)(i * 0x111111);

        byte[] lnk = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\System32\cmd.exe",
            Console = new ConsoleData { ColorTable = colorTable }
        });
        var options = Shortcut.Open(lnk);
        Assert.Equal(colorTable, options.Console!.ColorTable);
    }

    [Fact]
    public void ConsoleDataBlock_NotPresentWhenNull()
    {
        byte[] lnk = Shortcut.Create(new ShortcutOptions { Target = @"C:\test.exe" });
        Assert.False(BinaryAssert.ContainsSignature(lnk, 0xA0000002));
    }

    [Fact]
    public void ConsoleFEDataBlock_SignaturePresent()
    {
        byte[] lnk = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\test.exe",
            ConsoleCodePage = 932
        });
        Assert.True(BinaryAssert.ContainsSignature(lnk, 0xA0000004));
    }

    [Fact]
    public void ConsoleFEDataBlock_BlockSizeIs12()
    {
        byte[] lnk = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\test.exe",
            ConsoleCodePage = 932
        });
        int offset = BinaryAssert.FindSignatureOffset(lnk, 0xA0000004);
        Assert.True(offset >= 4);
        uint blockSize = BitConverter.ToUInt32(lnk, offset - 4);
        Assert.Equal(12u, blockSize);
    }

    [Fact]
    public void ConsoleFEDataBlock_CodePage_RoundTrips()
    {
        byte[] lnk = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\test.exe",
            ConsoleCodePage = 932 // Japanese
        });
        var options = Shortcut.Open(lnk);
        Assert.Equal(932u, options.ConsoleCodePage);
    }

    [Fact]
    public void ConsoleFEDataBlock_NotPresentWhenNull()
    {
        byte[] lnk = Shortcut.Create(new ShortcutOptions { Target = @"C:\test.exe" });
        Assert.False(BinaryAssert.ContainsSignature(lnk, 0xA0000004));
        var options = Shortcut.Open(lnk);
        Assert.Null(options.ConsoleCodePage);
    }

    [Fact]
    public void ConsoleAndConsoleFE_Combined_RoundTrip()
    {
        byte[] lnk = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\System32\cmd.exe",
            Console = new ConsoleData
            {
                ScreenBufferSizeX = 120,
                WindowSizeX = 120,
                QuickEdit = true
            },
            ConsoleCodePage = 65001 // UTF-8
        });
        var options = Shortcut.Open(lnk);
        Assert.NotNull(options.Console);
        Assert.Equal(120, options.Console!.ScreenBufferSizeX);
        Assert.True(options.Console.QuickEdit);
        Assert.Equal(65001u, options.ConsoleCodePage);
    }
}
