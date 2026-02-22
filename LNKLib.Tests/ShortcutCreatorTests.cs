using System.Text;
using Xunit;

namespace LNKLib.Tests;

public class ShortcutCreatorTests
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
    public void CreateShortcut_HeaderSize_Is76Bytes()
    {
        byte[] result = ShortcutCreator.CreateShortcut(@"C:\Windows\notepad.exe");

        uint headerSize = BitConverter.ToUInt32(result, 0);
        Assert.Equal(HEADER_SIZE, headerSize);
    }

    [Fact]
    public void CreateShortcut_LinkCLSID_IsCorrect()
    {
        byte[] result = ShortcutCreator.CreateShortcut(@"C:\Windows\notepad.exe");

        byte[] clsid = new byte[16];
        Array.Copy(result, 4, clsid, 0, 16);
        Assert.Equal(ExpectedLinkCLSID, clsid);
    }

    [Fact]
    public void CreateShortcut_LocalFile_SetsFileAttributes()
    {
        byte[] result = ShortcutCreator.CreateShortcut(@"C:\Windows\notepad.exe");

        // File attributes at offset 24 (after header size 4 + CLSID 16 + LinkFlags 4)
        uint fileAttr = BitConverter.ToUInt32(result, 24);
        Assert.Equal(0x00000020u, fileAttr); // FILE_ATTRIBUTE_ARCHIVE for files
    }

    [Fact]
    public void CreateShortcut_LocalFolder_SetsDirectoryAttributes()
    {
        byte[] result = ShortcutCreator.CreateShortcut(@"C:\Windows");

        uint fileAttr = BitConverter.ToUInt32(result, 24);
        Assert.Equal(0x00000010u, fileAttr); // FILE_ATTRIBUTE_DIRECTORY for folders
    }

    [Fact]
    public void CreateShortcut_ShowCommand_IsNormal()
    {
        byte[] result = ShortcutCreator.CreateShortcut(@"C:\Windows\notepad.exe");

        // ShowCommand at offset 60 (4 header + 16 CLSID + 4 flags + 4 attrs + 24 timestamps + 4 filesize + 4 iconindex)
        uint showCommand = BitConverter.ToUInt32(result, 60);
        Assert.Equal(1u, showCommand); // SW_SHOWNORMAL
    }

    [Fact]
    public void CreateShortcut_IconIndex_IsWrittenCorrectly()
    {
        byte[] result = ShortcutCreator.CreateShortcut(@"C:\Windows\notepad.exe", iconIndex: 42);

        // Icon index at offset 56 (4 header + 16 CLSID + 4 flags + 4 attrs + 24 timestamps + 4 filesize)
        int iconIndex = BitConverter.ToInt32(result, 56);
        Assert.Equal(42, iconIndex);
    }

    [Fact]
    public void CreateShortcut_LinkFlags_HasTargetIdList()
    {
        byte[] result = ShortcutCreator.CreateShortcut(@"C:\Windows\notepad.exe");

        uint linkFlags = BitConverter.ToUInt32(result, 20);
        Assert.True((linkFlags & 0x00000001) != 0, "FLAG_HAS_LINK_TARGET_ID_LIST should be set");
    }

    [Fact]
    public void CreateShortcut_WithName_SetsNameFlag()
    {
        byte[] result = ShortcutCreator.CreateShortcut(@"C:\Windows\notepad.exe", name: "Notepad");

        uint linkFlags = BitConverter.ToUInt32(result, 20);
        Assert.True((linkFlags & 0x00000004) != 0, "FLAG_HAS_NAME should be set");
    }

    [Fact]
    public void CreateShortcut_WithWorkingDir_SetsWorkingDirFlag()
    {
        byte[] result = ShortcutCreator.CreateShortcut(@"C:\Windows\notepad.exe", workingDirectory: @"C:\Windows");

        uint linkFlags = BitConverter.ToUInt32(result, 20);
        Assert.True((linkFlags & 0x00000010) != 0, "FLAG_HAS_WORKING_DIR should be set");
    }

    [Fact]
    public void CreateShortcut_WithArguments_SetsArgumentsFlag()
    {
        byte[] result = ShortcutCreator.CreateShortcut(@"C:\Windows\notepad.exe", arguments: "/test");

        uint linkFlags = BitConverter.ToUInt32(result, 20);
        Assert.True((linkFlags & 0x00000020) != 0, "FLAG_HAS_ARGUMENTS should be set");
    }

    [Fact]
    public void CreateShortcut_WithIconLocation_SetsIconFlag()
    {
        byte[] result = ShortcutCreator.CreateShortcut(@"C:\Windows\notepad.exe", iconLocation: @"C:\icon.ico");

        uint linkFlags = BitConverter.ToUInt32(result, 20);
        Assert.True((linkFlags & 0x00000040) != 0, "FLAG_HAS_ICON_LOCATION should be set");
    }

    [Fact]
    public void CreateShortcut_WithoutOptionals_ClearsOptionalFlags()
    {
        byte[] result = ShortcutCreator.CreateShortcut(@"C:\Windows\notepad.exe");

        uint linkFlags = BitConverter.ToUInt32(result, 20);
        Assert.True((linkFlags & 0x00000004) == 0, "FLAG_HAS_NAME should not be set");
        Assert.True((linkFlags & 0x00000010) == 0, "FLAG_HAS_WORKING_DIR should not be set");
        Assert.True((linkFlags & 0x00000020) == 0, "FLAG_HAS_ARGUMENTS should not be set");
        Assert.True((linkFlags & 0x00000040) == 0, "FLAG_HAS_ICON_LOCATION should not be set");
    }

    [Fact]
    public void CreateShortcut_WithEnvVar_SetsEnvFlags()
    {
        byte[] result = ShortcutCreator.CreateShortcut(@"%windir%\notepad.exe");

        uint linkFlags = BitConverter.ToUInt32(result, 20);
        Assert.True((linkFlags & 0x00000200) != 0, "FLAG_HAS_EXP_SZ should be set");
        Assert.True((linkFlags & 0x02000000) != 0, "FLAG_PREFER_ENVIRONMENT_PATH should be set");
    }

    [Fact]
    public void CreateShortcut_WithEnvVar_PreservesUpperBitFlags()
    {
        // Regression test: previously linkFlags was truncated to a single byte,
        // losing FLAG_PREFER_ENVIRONMENT_PATH (0x02000000)
        byte[] result = ShortcutCreator.CreateShortcut(@"%windir%\notepad.exe");

        uint linkFlags = BitConverter.ToUInt32(result, 20);
        Assert.True((linkFlags & 0x02000000) != 0,
            "Upper-bit flags must not be truncated when writing LinkFlags");
    }

    [Fact]
    public void CreateShortcut_WithEnvVar_ContainsEnvDataBlock()
    {
        byte[] result = ShortcutCreator.CreateShortcut(@"%windir%\notepad.exe");

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
    public void CreateShortcut_WithoutEnvVar_NoEnvDataBlock()
    {
        byte[] result = ShortcutCreator.CreateShortcut(@"C:\Windows\notepad.exe");

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
    public void CreateShortcut_EndsWithTerminator()
    {
        byte[] result = ShortcutCreator.CreateShortcut(@"C:\Windows\notepad.exe");

        // Last 4 bytes should be the extra data chain terminator (0x00000000)
        uint terminator = BitConverter.ToUInt32(result, result.Length - 4);
        Assert.Equal(0u, terminator);
    }

    [Fact]
    public void CreateShortcut_NetworkPath_CreatesValidOutput()
    {
        byte[] result = ShortcutCreator.CreateShortcut(@"\\server\share\file.txt");

        // Should still have valid header
        uint headerSize = BitConverter.ToUInt32(result, 0);
        Assert.Equal(HEADER_SIZE, headerSize);

        // File attributes should be FILE_ATTRIBUTE_ARCHIVE for .txt file
        uint fileAttr = BitConverter.ToUInt32(result, 24);
        Assert.Equal(0x00000020u, fileAttr);
    }

    [Fact]
    public void CreateShortcut_NetworkFolder_SetsDirectoryAttributes()
    {
        byte[] result = ShortcutCreator.CreateShortcut(@"\\server\share\folder");

        uint fileAttr = BitConverter.ToUInt32(result, 24);
        Assert.Equal(0x00000010u, fileAttr); // Directory
    }

    [Fact]
    public void CreateShortcut_PrinterLink_CreatesValidOutput()
    {
        byte[] result = ShortcutCreator.CreateShortcut(@"\\server\printer", isPrinterLink: true);

        uint headerSize = BitConverter.ToUInt32(result, 0);
        Assert.Equal(HEADER_SIZE, headerSize);
    }

    [Fact]
    public void CreateShortcut_RootDriveOnly_CreatesValidOutput()
    {
        // Target is just "C:" which should become a root link
        byte[] result = ShortcutCreator.CreateShortcut(@"C:");

        uint headerSize = BitConverter.ToUInt32(result, 0);
        Assert.Equal(HEADER_SIZE, headerSize);
    }

    [Fact]
    public void CreateShortcut_AllOptionalFields_CreatesValidOutput()
    {
        byte[] result = ShortcutCreator.CreateShortcut(
            target: @"C:\Windows\notepad.exe",
            name: "Notepad",
            workingDirectory: @"C:\Windows",
            arguments: "test.txt",
            iconLocation: @"C:\Windows\notepad.exe",
            iconIndex: 0);

        // Verify all flags are set
        uint linkFlags = BitConverter.ToUInt32(result, 20);
        Assert.True((linkFlags & 0x00000001) != 0, "FLAG_HAS_LINK_TARGET_ID_LIST");
        Assert.True((linkFlags & 0x00000004) != 0, "FLAG_HAS_NAME");
        Assert.True((linkFlags & 0x00000010) != 0, "FLAG_HAS_WORKING_DIR");
        Assert.True((linkFlags & 0x00000020) != 0, "FLAG_HAS_ARGUMENTS");
        Assert.True((linkFlags & 0x00000040) != 0, "FLAG_HAS_ICON_LOCATION");
    }

    [Fact]
    public void CreateShortcut_OutputSize_IsReasonable()
    {
        byte[] result = ShortcutCreator.CreateShortcut(@"C:\Windows\notepad.exe");

        // A minimal .lnk file should be at least 76 bytes (header) + IDList + terminator
        Assert.True(result.Length >= 76, "Output should be at least 76 bytes (header size)");
        // And not unreasonably large for a simple shortcut
        Assert.True(result.Length < 4096, "Output should be reasonably sized for a simple shortcut");
    }

    [Fact]
    public void CreateShortcut_LongExtension_TreatedAsFolder()
    {
        // Extension longer than 3 chars should be treated as folder
        byte[] result = ShortcutCreator.CreateShortcut(@"C:\path\file.longext");

        uint fileAttr = BitConverter.ToUInt32(result, 24);
        Assert.Equal(0x00000010u, fileAttr); // Directory attributes
    }

    [Fact]
    public void CreateShortcut_ThreeCharExtension_TreatedAsFile()
    {
        byte[] result = ShortcutCreator.CreateShortcut(@"C:\path\file.exe");

        uint fileAttr = BitConverter.ToUInt32(result, 24);
        Assert.Equal(0x00000020u, fileAttr); // File attributes
    }

    [Fact]
    public void CreateShortcut_OneCharExtension_TreatedAsFile()
    {
        byte[] result = ShortcutCreator.CreateShortcut(@"C:\path\file.a");

        uint fileAttr = BitConverter.ToUInt32(result, 24);
        Assert.Equal(0x00000020u, fileAttr); // File attributes
    }

    [Fact]
    public void CreateShortcut_PadArguments_OutputIsLarger()
    {
        byte[] normal = ShortcutCreator.CreateShortcut(@"C:\Windows\notepad.exe", arguments: "test.txt");
        byte[] padded = ShortcutCreator.CreateShortcut(@"C:\Windows\notepad.exe", arguments: "test.txt", padArguments: true);

        // Padded output should be significantly larger (31 KB padding)
        Assert.True(padded.Length > normal.Length + 30000,
            $"Padded output ({padded.Length}) should be >30KB larger than normal ({normal.Length})");
    }

    [Fact]
    public void CreateShortcut_PadArguments_ContainsActualArguments()
    {
        string args = "myarg123";
        byte[] result = ShortcutCreator.CreateShortcut(@"C:\Windows\notepad.exe", arguments: args, padArguments: true);

        // The actual arguments should appear in the output bytes
        byte[] argBytes = Encoding.Default.GetBytes(args);
        bool found = false;
        for (int i = 0; i <= result.Length - argBytes.Length; i++)
        {
            bool match = true;
            for (int j = 0; j < argBytes.Length; j++)
            {
                if (result[i + j] != argBytes[j]) { match = false; break; }
            }
            if (match) { found = true; break; }
        }
        Assert.True(found, "Padded output should contain the actual argument string");
    }

    [Fact]
    public void CreateShortcut_PadArguments_WithoutArguments_NoEffect()
    {
        byte[] withPad = ShortcutCreator.CreateShortcut(@"C:\Windows\notepad.exe", padArguments: true);
        byte[] withoutPad = ShortcutCreator.CreateShortcut(@"C:\Windows\notepad.exe", padArguments: false);

        // When arguments is null, padArguments should have no effect
        Assert.Equal(withoutPad.Length, withPad.Length);
    }
}
