using LNKLib.Tests.Helpers;
using System.Text;
using Xunit;

namespace LNKLib.Tests;

public class ShortcutTests
{
    private const uint HEADER_SIZE = 0x4C; // 76 bytes
    private const uint ENV_BLOCK_SIGNATURE = 0xA0000001;

    // Expected LinkCLSID bytes for {00021401-0000-0000-C000-000000000046}
    private static readonly byte[] ExpectedLinkCLSID =
    [
        0x01, 0x14, 0x02, 0x00,
        0x00, 0x00,
        0x00, 0x00,
        0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46
    ];

    [Fact]
    public void Create_HeaderSize_Is76Bytes()
    {
        byte[] result = Shortcut.Create(new ShortcutOptions { Target = @"C:\Windows\notepad.exe" });

        uint headerSize = BitConverter.ToUInt32(result, 0);
        Assert.Equal(HEADER_SIZE, headerSize);
    }

    [Fact]
    public void Create_LinkCLSID_IsCorrect()
    {
        byte[] result = Shortcut.Create(new ShortcutOptions { Target = @"C:\Windows\notepad.exe" });

        byte[] clsid = new byte[16];
        Array.Copy(result, 4, clsid, 0, 16);
        Assert.Equal(ExpectedLinkCLSID, clsid);
    }

    [Fact]
    public void Create_LocalFile_SetsFileAttributes()
    {
        byte[] result = Shortcut.Create(new ShortcutOptions { Target = @"C:\Windows\notepad.exe" });

        // File attributes at offset 24 (after header size 4 + CLSID 16 + LinkFlags 4)
        uint fileAttr = BitConverter.ToUInt32(result, 24);
        Assert.Equal(0x00000020u, fileAttr); // FILE_ATTRIBUTE_ARCHIVE for files
    }

    [Fact]
    public void Create_LocalFolder_SetsDirectoryAttributes()
    {
        byte[] result = Shortcut.Create(new ShortcutOptions { Target = @"C:\Windows" });

        uint fileAttr = BitConverter.ToUInt32(result, 24);
        Assert.Equal(0x00000010u, fileAttr); // FILE_ATTRIBUTE_DIRECTORY for folders
    }

    [Fact]
    public void Create_IconIndex_IsWrittenCorrectly()
    {
        byte[] result = Shortcut.Create(new ShortcutOptions { Target = @"C:\Windows\notepad.exe", IconIndex = 42 });

        // Icon index at offset 56 (4 header + 16 CLSID + 4 flags + 4 attrs + 24 timestamps + 4 filesize)
        int iconIndex = BitConverter.ToInt32(result, 56);
        Assert.Equal(42, iconIndex);
    }

    [Fact]
    public void Create_LinkFlags_HasTargetIdList()
    {
        byte[] result = Shortcut.Create(new ShortcutOptions { Target = @"C:\Windows\notepad.exe" });

        uint linkFlags = BitConverter.ToUInt32(result, 20);
        Assert.True((linkFlags & 0x00000001) != 0, "FLAG_HAS_LINK_TARGET_ID_LIST should be set");
    }

    [Fact]
    public void Create_WithName_SetsNameFlag()
    {
        byte[] result = Shortcut.Create(new ShortcutOptions { Target = @"C:\Windows\notepad.exe", Description = "Notepad" });

        uint linkFlags = BitConverter.ToUInt32(result, 20);
        Assert.True((linkFlags & 0x00000004) != 0, "FLAG_HAS_NAME should be set");
    }

    [Fact]
    public void Create_WithWorkingDir_SetsWorkingDirFlag()
    {
        byte[] result = Shortcut.Create(new ShortcutOptions { Target = @"C:\Windows\notepad.exe", WorkingDirectory = @"C:\Windows" });

        uint linkFlags = BitConverter.ToUInt32(result, 20);
        Assert.True((linkFlags & 0x00000010) != 0, "FLAG_HAS_WORKING_DIR should be set");
    }

    [Fact]
    public void Create_WithArguments_SetsArgumentsFlag()
    {
        byte[] result = Shortcut.Create(new ShortcutOptions { Target = @"C:\Windows\notepad.exe", Arguments = "/test" });

        uint linkFlags = BitConverter.ToUInt32(result, 20);
        Assert.True((linkFlags & 0x00000020) != 0, "FLAG_HAS_ARGUMENTS should be set");
    }

    [Fact]
    public void Create_WithIconLocation_SetsIconFlag()
    {
        byte[] result = Shortcut.Create(new ShortcutOptions { Target = @"C:\Windows\notepad.exe", IconLocation = @"C:\icon.ico" });

        uint linkFlags = BitConverter.ToUInt32(result, 20);
        Assert.True((linkFlags & 0x00000040) != 0, "FLAG_HAS_ICON_LOCATION should be set");
    }

    [Fact]
    public void Create_WithoutOptionals_ClearsOptionalFlags()
    {
        byte[] result = Shortcut.Create(new ShortcutOptions { Target = @"C:\Windows\notepad.exe" });

        uint linkFlags = BitConverter.ToUInt32(result, 20);
        Assert.True((linkFlags & 0x00000004) == 0, "FLAG_HAS_NAME should not be set");
        Assert.True((linkFlags & 0x00000010) == 0, "FLAG_HAS_WORKING_DIR should not be set");
        Assert.True((linkFlags & 0x00000020) == 0, "FLAG_HAS_ARGUMENTS should not be set");
        Assert.True((linkFlags & 0x00000040) == 0, "FLAG_HAS_ICON_LOCATION should not be set");
    }

    [Fact]
    public void Create_WithEnvVar_SetsEnvFlags()
    {
        byte[] result = Shortcut.Create(new ShortcutOptions { Target = @"%windir%\notepad.exe" });

        uint linkFlags = BitConverter.ToUInt32(result, 20);
        Assert.True((linkFlags & 0x00000200) != 0, "FLAG_HAS_EXP_SZ should be set");
        Assert.True((linkFlags & 0x02000000) != 0, "FLAG_PREFER_ENVIRONMENT_PATH should be set");
    }

    [Fact]
    public void Create_WithEnvVar_ContainsEnvDataBlock()
    {
        byte[] result = Shortcut.Create(new ShortcutOptions { Target = @"%windir%\notepad.exe" });

        // Search for environment variable data block signature
        bool found = false;
        for (int i = 0; i <= result.Length - 4; i++)
        {
            uint val = BitConverter.ToUInt32(result, i);
            if (val == ENV_BLOCK_SIGNATURE)
            {
                found = true;
                break;
            }
        }
        Assert.True(found, "Environment variable data block signature (0xA0000001) should be present");
    }

    [Fact]
    public void Create_WithoutEnvVar_NoEnvDataBlock()
    {
        byte[] result = Shortcut.Create(new ShortcutOptions { Target = @"C:\Windows\notepad.exe" });

        // Verify environment variable data block signature is NOT present
        bool found = false;
        for (int i = 0; i <= result.Length - 4; i++)
        {
            uint val = BitConverter.ToUInt32(result, i);
            if (val == ENV_BLOCK_SIGNATURE)
            {
                found = true;
                break;
            }
        }
        Assert.False(found, "Environment variable data block should not be present for non-env-var targets");
    }

    [Fact]
    public void Create_EndsWithTerminator()
    {
        byte[] result = Shortcut.Create(new ShortcutOptions { Target = @"C:\Windows\notepad.exe" });

        // Last 4 bytes should be the extra data chain terminator (0x00000000)
        uint terminator = BitConverter.ToUInt32(result, result.Length - 4);
        Assert.Equal(0u, terminator);
    }

    [Fact]
    public void Create_NetworkPath_CreatesValidOutput()
    {
        byte[] result = Shortcut.Create(new ShortcutOptions { Target = @"\\server\share\file.txt" });

        // Should still have valid header
        uint headerSize = BitConverter.ToUInt32(result, 0);
        Assert.Equal(HEADER_SIZE, headerSize);

        // File attributes should be FILE_ATTRIBUTE_ARCHIVE for .txt file
        uint fileAttr = BitConverter.ToUInt32(result, 24);
        Assert.Equal(0x00000020u, fileAttr);
    }

    [Fact]
    public void Create_NetworkFolder_SetsDirectoryAttributes()
    {
        byte[] result = Shortcut.Create(new ShortcutOptions { Target = @"\\server\share\folder" });

        uint fileAttr = BitConverter.ToUInt32(result, 24);
        Assert.Equal(0x00000010u, fileAttr); // Directory
    }

    [Fact]
    public void Create_PrinterLink_CreatesValidOutput()
    {
        byte[] result = Shortcut.Create(new ShortcutOptions { Target = @"\\server\printer", IsPrinterLink = true });

        uint headerSize = BitConverter.ToUInt32(result, 0);
        Assert.Equal(HEADER_SIZE, headerSize);
    }

    [Fact]
    public void Create_RootDriveOnly_CreatesValidOutput()
    {
        // Target is just "C:" which should become a root link
        byte[] result = Shortcut.Create(new ShortcutOptions { Target = @"C:" });

        uint headerSize = BitConverter.ToUInt32(result, 0);
        Assert.Equal(HEADER_SIZE, headerSize);
    }

    [Fact]
    public void Create_AllOptionalFields_CreatesValidOutput()
    {
        byte[] result = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\notepad.exe",
            Description = "Notepad",
            WorkingDirectory = @"C:\Windows",
            Arguments = "test.txt",
            IconLocation = @"C:\Windows\notepad.exe",
            IconIndex = 0
        });

        // Verify all flags are set
        uint linkFlags = BitConverter.ToUInt32(result, 20);
        Assert.True((linkFlags & 0x00000001) != 0, "FLAG_HAS_LINK_TARGET_ID_LIST");
        Assert.True((linkFlags & 0x00000004) != 0, "FLAG_HAS_NAME");
        Assert.True((linkFlags & 0x00000010) != 0, "FLAG_HAS_WORKING_DIR");
        Assert.True((linkFlags & 0x00000020) != 0, "FLAG_HAS_ARGUMENTS");
        Assert.True((linkFlags & 0x00000040) != 0, "FLAG_HAS_ICON_LOCATION");
    }

    [Fact]
    public void Create_OutputSize_IsReasonable()
    {
        byte[] result = Shortcut.Create(new ShortcutOptions { Target = @"C:\Windows\notepad.exe" });

        // A minimal .lnk file should be at least 76 bytes (header) + IDList + terminator
        Assert.True(result.Length >= 76, "Output should be at least 76 bytes (header size)");
        // And not unreasonably large for a simple shortcut
        Assert.True(result.Length < 4096, "Output should be reasonably sized for a simple shortcut");
    }

    [Fact]
    public void Create_LongExtension_TreatedAsFolder()
    {
        // Extension longer than 3 chars should be treated as folder
        byte[] result = Shortcut.Create(new ShortcutOptions { Target = @"C:\path\file.longext" });

        uint fileAttr = BitConverter.ToUInt32(result, 24);
        Assert.Equal(0x00000010u, fileAttr); // Directory attributes
    }

    [Fact]
    public void Create_ThreeCharExtension_TreatedAsFile()
    {
        byte[] result = Shortcut.Create(new ShortcutOptions { Target = @"C:\path\file.exe" });

        uint fileAttr = BitConverter.ToUInt32(result, 24);
        Assert.Equal(0x00000020u, fileAttr); // File attributes
    }

    [Fact]
    public void Create_OneCharExtension_TreatedAsFile()
    {
        byte[] result = Shortcut.Create(new ShortcutOptions { Target = @"C:\path\file.a" });

        uint fileAttr = BitConverter.ToUInt32(result, 24);
        Assert.Equal(0x00000020u, fileAttr); // File attributes
    }

    // --- WindowStyle tests ---

    [Fact]
    public void Create_WindowStyleNormal_WritesCorrectValue()
    {
        byte[] result = Shortcut.Create(new ShortcutOptions { Target = @"C:\Windows\notepad.exe", WindowStyle = ShortcutWindowStyle.Normal });

        uint showCommand = BitConverter.ToUInt32(result, 60);
        Assert.Equal(1u, showCommand);
    }

    [Fact]
    public void Create_WindowStyleMaximized_WritesCorrectValue()
    {
        byte[] result = Shortcut.Create(new ShortcutOptions { Target = @"C:\Windows\notepad.exe", WindowStyle = ShortcutWindowStyle.Maximized });

        uint showCommand = BitConverter.ToUInt32(result, 60);
        Assert.Equal(3u, showCommand); // SW_SHOWMAXIMIZED
    }

    [Fact]
    public void Create_WindowStyleMinimized_WritesCorrectValue()
    {
        byte[] result = Shortcut.Create(new ShortcutOptions { Target = @"C:\Windows\notepad.exe", WindowStyle = ShortcutWindowStyle.Minimized });

        uint showCommand = BitConverter.ToUInt32(result, 60);
        Assert.Equal(7u, showCommand); // SW_SHOWMINNOACTIVE
    }

    // --- RunAsAdmin tests ---

    [Fact]
    public void Create_RunAsAdmin_SetsRunAsUserFlag()
    {
        byte[] result = Shortcut.Create(new ShortcutOptions { Target = @"C:\Windows\notepad.exe", RunAsAdmin = true });

        uint linkFlags = BitConverter.ToUInt32(result, 20);
        Assert.True((linkFlags & 0x00002000) != 0, "FLAG_RUN_AS_USER should be set");
    }

    [Fact]
    public void Create_WithoutRunAsAdmin_ClearsRunAsUserFlag()
    {
        byte[] result = Shortcut.Create(new ShortcutOptions { Target = @"C:\Windows\notepad.exe" });

        uint linkFlags = BitConverter.ToUInt32(result, 20);
        Assert.True((linkFlags & 0x00002000) == 0, "FLAG_RUN_AS_USER should not be set");
    }

    [Fact]
    public void Create_RunAsAdmin_WithOtherFlags_PreservesAllFlags()
    {
        byte[] result = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\notepad.exe",
            Description = "Test",
            Arguments = "/test",
            RunAsAdmin = true
        });

        uint linkFlags = BitConverter.ToUInt32(result, 20);
        Assert.True((linkFlags & 0x00002000) != 0, "FLAG_RUN_AS_USER");
        Assert.True((linkFlags & 0x00000004) != 0, "FLAG_HAS_NAME");
        Assert.True((linkFlags & 0x00000020) != 0, "FLAG_HAS_ARGUMENTS");
    }

    // --- Hotkey tests ---

    [Fact]
    public void Create_Hotkey_WritesKeyAndModifiers()
    {
        // Ctrl+Alt+T: key = 0x54 ('T'), modifiers = Control | Alt = 0x06
        byte[] result = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\notepad.exe",
            HotkeyKey = 0x54,
            HotkeyModifiers = HotkeyModifiers.Control | HotkeyModifiers.Alt
        });

        // Hotkey at offset 64 (2 bytes: low = key, high = modifiers)
        Assert.Equal(0x54, result[64]); // Virtual key code for 'T'
        Assert.Equal(0x06, result[65]); // Control (0x02) | Alt (0x04)
    }

    [Fact]
    public void Create_DefaultHotkey_IsZero()
    {
        byte[] result = Shortcut.Create(new ShortcutOptions { Target = @"C:\Windows\notepad.exe" });

        Assert.Equal(0, result[64]);
        Assert.Equal(0, result[65]);
    }

    [Fact]
    public void Create_Hotkey_ShiftOnly()
    {
        byte[] result = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\notepad.exe",
            HotkeyKey = 0x41, // 'A'
            HotkeyModifiers = HotkeyModifiers.Shift
        });

        Assert.Equal(0x41, result[64]);
        Assert.Equal(0x01, result[65]); // Shift
    }

}
