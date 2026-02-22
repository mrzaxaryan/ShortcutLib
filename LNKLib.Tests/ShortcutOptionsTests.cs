using System.Text;
using Xunit;

namespace LNKLib.Tests;

public class ShortcutOptionsTests
{
    private const uint HEADER_SIZE = 0x4C;
    private const uint ENV_BLOCK_SIGNATURE = 0xA0000001;
    private const uint ICON_ENV_BLOCK_SIGNATURE = 0xA0000007;
    private const uint TRACKER_BLOCK_SIGNATURE = 0xA0000003;
    private const uint SPECIAL_FOLDER_BLOCK_SIGNATURE = 0xA0000005;
    private const uint PROPERTY_STORE_BLOCK_SIGNATURE = 0xA0000009;
    private const uint KNOWN_FOLDER_BLOCK_SIGNATURE = 0xA000000B;

    // --- Parity: ShortcutOptions produces identical output to parameter overload ---

    [Fact]
    public void Create_Options_SimpleTarget_MatchesParameterOverload()
    {
        byte[] fromParams = Shortcut.Create(@"C:\Windows\notepad.exe");
        byte[] fromOptions = Shortcut.Create(new ShortcutOptions { Target = @"C:\Windows\notepad.exe" });
        Assert.Equal(fromParams, fromOptions);
    }

    [Fact]
    public void Create_Options_AllLegacyParams_MatchesParameterOverload()
    {
        byte[] fromParams = Shortcut.Create(
            target: @"C:\Windows\notepad.exe",
            arguments: "test.txt",
            iconLocation: @"C:\Windows\notepad.exe",
            iconIndex: 1,
            description: "Notepad",
            workingDirectory: @"C:\Windows",
            windowStyle: ShortcutWindowStyle.Maximized,
            runAsAdmin: true,
            hotkeyKey: 0x54,
            hotkeyModifiers: HotkeyModifiers.Control | HotkeyModifiers.Alt);

        byte[] fromOptions = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\notepad.exe",
            Arguments = "test.txt",
            IconLocation = @"C:\Windows\notepad.exe",
            IconIndex = 1,
            Description = "Notepad",
            WorkingDirectory = @"C:\Windows",
            WindowStyle = ShortcutWindowStyle.Maximized,
            RunAsAdmin = true,
            HotkeyKey = 0x54,
            HotkeyModifiers = HotkeyModifiers.Control | HotkeyModifiers.Alt
        });
        Assert.Equal(fromParams, fromOptions);
    }

    [Fact]
    public void Create_Options_NetworkPath_MatchesParameterOverload()
    {
        byte[] fromParams = Shortcut.Create(@"\\server\share\file.txt");
        byte[] fromOptions = Shortcut.Create(new ShortcutOptions { Target = @"\\server\share\file.txt" });
        Assert.Equal(fromParams, fromOptions);
    }

    [Fact]
    public void Create_Options_EnvVar_MatchesParameterOverload()
    {
        byte[] fromParams = Shortcut.Create(@"%windir%\notepad.exe");
        byte[] fromOptions = Shortcut.Create(new ShortcutOptions { Target = @"%windir%\notepad.exe" });
        Assert.Equal(fromParams, fromOptions);
    }

    [Fact]
    public void Create_Options_PrinterLink_MatchesParameterOverload()
    {
        byte[] fromParams = Shortcut.Create(@"\\server\printer", isPrinterLink: true);
        byte[] fromOptions = Shortcut.Create(new ShortcutOptions { Target = @"\\server\printer", IsPrinterLink = true });
        Assert.Equal(fromParams, fromOptions);
    }

    [Fact]
    public void Create_Options_PaddedArguments_MatchesParameterOverload()
    {
        byte[] fromParams = Shortcut.Create(@"C:\Windows\notepad.exe", arguments: "secret", padArguments: true);
        byte[] fromOptions = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\notepad.exe",
            Arguments = "secret",
            PadArguments = true
        });
        Assert.Equal(fromParams, fromOptions);
    }

    // --- Unicode ---

    [Fact]
    public void Create_UseUnicode_SetsIsUnicodeFlag()
    {
        byte[] result = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\notepad.exe",
            UseUnicode = true,
            Description = "Test"
        });
        uint linkFlags = BitConverter.ToUInt32(result, 20);
        Assert.True((linkFlags & 0x00000080) != 0, "IS_UNICODE flag should be set");
    }

    [Fact]
    public void Create_WithoutUnicode_ClearsIsUnicodeFlag()
    {
        byte[] result = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\notepad.exe",
            Description = "Test"
        });
        uint linkFlags = BitConverter.ToUInt32(result, 20);
        Assert.True((linkFlags & 0x00000080) == 0, "IS_UNICODE flag should not be set");
    }

    [Fact]
    public void Create_UseUnicode_ContainsUtf16Description()
    {
        string description = "Hello";
        byte[] result = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\notepad.exe",
            UseUnicode = true,
            Description = description
        });

        // UTF-16LE for "Hello" is: 48 00 65 00 6C 00 6C 00 6F 00
        byte[] utf16Bytes = Encoding.Unicode.GetBytes(description);
        bool found = ContainsBytes(result, utf16Bytes);
        Assert.True(found, "Output should contain UTF-16LE encoded description");
    }

    [Fact]
    public void Create_UseUnicode_OutputIsLargerThanAnsi()
    {
        byte[] ansi = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\notepad.exe",
            Description = "Test Description",
            Arguments = "--flag",
            WorkingDirectory = @"C:\Windows",
            UseUnicode = false
        });
        byte[] unicode = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\notepad.exe",
            Description = "Test Description",
            Arguments = "--flag",
            WorkingDirectory = @"C:\Windows",
            UseUnicode = true
        });
        Assert.True(unicode.Length > ansi.Length, "Unicode output should be larger than ANSI");
    }

    // --- Timestamps ---

    [Fact]
    public void Create_CreationTime_WritesFileTimeValue()
    {
        var dt = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        byte[] result = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\notepad.exe",
            CreationTime = dt
        });

        long expected = dt.ToFileTimeUtc();
        long actual = BitConverter.ToInt64(result, 28);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Create_AccessTime_WritesFileTimeValue()
    {
        var dt = new DateTime(2024, 6, 1, 8, 30, 0, DateTimeKind.Utc);
        byte[] result = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\notepad.exe",
            AccessTime = dt
        });

        long expected = dt.ToFileTimeUtc();
        long actual = BitConverter.ToInt64(result, 36); // AccessTime at offset 36
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Create_WriteTime_WritesFileTimeValue()
    {
        var dt = new DateTime(2025, 12, 25, 0, 0, 0, DateTimeKind.Utc);
        byte[] result = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\notepad.exe",
            WriteTime = dt
        });

        long expected = dt.ToFileTimeUtc();
        long actual = BitConverter.ToInt64(result, 44); // WriteTime at offset 44
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Create_NullTimestamps_WritesZero()
    {
        byte[] result = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\notepad.exe"
        });

        Assert.Equal(0L, BitConverter.ToInt64(result, 28)); // CreationTime
        Assert.Equal(0L, BitConverter.ToInt64(result, 36)); // AccessTime
        Assert.Equal(0L, BitConverter.ToInt64(result, 44)); // WriteTime
    }

    [Fact]
    public void Create_AllTimestamps_WritesDifferentValues()
    {
        var create = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var access = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var write = new DateTime(2023, 3, 10, 8, 0, 0, DateTimeKind.Utc);

        byte[] result = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\notepad.exe",
            CreationTime = create,
            AccessTime = access,
            WriteTime = write
        });

        Assert.Equal(create.ToFileTimeUtc(), BitConverter.ToInt64(result, 28));
        Assert.Equal(access.ToFileTimeUtc(), BitConverter.ToInt64(result, 36));
        Assert.Equal(write.ToFileTimeUtc(), BitConverter.ToInt64(result, 44));
    }

    // --- File Size ---

    [Fact]
    public void Create_FileSize_WritesCorrectValue()
    {
        byte[] result = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\notepad.exe",
            FileSize = 123456
        });

        uint size = BitConverter.ToUInt32(result, 52); // FileSize at offset 52
        Assert.Equal(123456u, size);
    }

    [Fact]
    public void Create_DefaultFileSize_IsZero()
    {
        byte[] result = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\notepad.exe"
        });

        uint size = BitConverter.ToUInt32(result, 52);
        Assert.Equal(0u, size);
    }

    // --- Relative Path ---

    [Fact]
    public void Create_RelativePath_SetsFlag()
    {
        byte[] result = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\notepad.exe",
            RelativePath = @".\notepad.exe"
        });

        uint linkFlags = BitConverter.ToUInt32(result, 20);
        Assert.True((linkFlags & 0x00000008) != 0, "HasRelativePath flag should be set");
    }

    [Fact]
    public void Create_WithoutRelativePath_ClearsFlag()
    {
        byte[] result = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\notepad.exe"
        });

        uint linkFlags = BitConverter.ToUInt32(result, 20);
        Assert.True((linkFlags & 0x00000008) == 0, "HasRelativePath flag should not be set");
    }

    [Fact]
    public void Create_RelativePath_ContainsPathInOutput()
    {
        string relPath = @".\subfolder\notepad.exe";
        byte[] result = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\notepad.exe",
            RelativePath = relPath
        });

        byte[] pathBytes = Encoding.Default.GetBytes(relPath);
        Assert.True(ContainsBytes(result, pathBytes), "Output should contain relative path string");
    }

    // --- LinkInfo ---

    [Fact]
    public void Create_LinkInfo_Local_SetsFlag()
    {
        byte[] result = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\notepad.exe",
            LinkInfo = new LinkInfo
            {
                Local = new LocalPathInfo
                {
                    BasePath = @"C:\Windows\notepad.exe",
                    DriveSerialNumber = 0x12345678,
                    VolumeLabel = "Windows"
                }
            }
        });

        uint linkFlags = BitConverter.ToUInt32(result, 20);
        Assert.True((linkFlags & 0x00000002) != 0, "HasLinkInfo flag should be set");
    }

    [Fact]
    public void Create_LinkInfo_Network_SetsFlag()
    {
        byte[] result = Shortcut.Create(new ShortcutOptions
        {
            Target = @"\\server\share\file.txt",
            LinkInfo = new LinkInfo
            {
                Network = new NetworkPathInfo
                {
                    ShareName = @"\\server\share",
                    CommonPathSuffix = "file.txt"
                }
            }
        });

        uint linkFlags = BitConverter.ToUInt32(result, 20);
        Assert.True((linkFlags & 0x00000002) != 0, "HasLinkInfo flag should be set");
    }

    [Fact]
    public void Create_WithoutLinkInfo_ClearsFlag()
    {
        byte[] result = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\notepad.exe"
        });

        uint linkFlags = BitConverter.ToUInt32(result, 20);
        Assert.True((linkFlags & 0x00000002) == 0, "HasLinkInfo flag should not be set");
    }

    [Fact]
    public void Create_LinkInfo_Local_ContainsBasePath()
    {
        string basePath = @"C:\Windows\notepad.exe";
        byte[] result = Shortcut.Create(new ShortcutOptions
        {
            Target = basePath,
            LinkInfo = new LinkInfo
            {
                Local = new LocalPathInfo { BasePath = basePath }
            }
        });

        // The base path should appear null-terminated in the output
        byte[] pathBytes = Encoding.Default.GetBytes(basePath + "\0");
        Assert.True(ContainsBytes(result, pathBytes), "Output should contain local base path");
    }

    [Fact]
    public void Create_LinkInfo_Local_ContainsVolumeLabel()
    {
        byte[] result = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\notepad.exe",
            LinkInfo = new LinkInfo
            {
                Local = new LocalPathInfo
                {
                    BasePath = @"C:\Windows\notepad.exe",
                    VolumeLabel = "SYSTEM"
                }
            }
        });

        byte[] labelBytes = Encoding.Default.GetBytes("SYSTEM\0");
        Assert.True(ContainsBytes(result, labelBytes), "Output should contain volume label");
    }

    [Fact]
    public void Create_LinkInfo_Local_ContainsDriveSerialNumber()
    {
        uint serial = 0xDEADBEEF;
        byte[] result = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\notepad.exe",
            LinkInfo = new LinkInfo
            {
                Local = new LocalPathInfo
                {
                    BasePath = @"C:\Windows\notepad.exe",
                    DriveSerialNumber = serial
                }
            }
        });

        byte[] serialBytes = BitConverter.GetBytes(serial);
        Assert.True(ContainsBytes(result, serialBytes), "Output should contain drive serial number");
    }

    [Fact]
    public void Create_LinkInfo_Network_ContainsShareName()
    {
        string shareName = @"\\myserver\myshare";
        byte[] result = Shortcut.Create(new ShortcutOptions
        {
            Target = @"\\myserver\myshare\file.txt",
            LinkInfo = new LinkInfo
            {
                Network = new NetworkPathInfo
                {
                    ShareName = shareName,
                    CommonPathSuffix = "file.txt"
                }
            }
        });

        byte[] shareBytes = Encoding.Default.GetBytes(shareName + "\0");
        Assert.True(ContainsBytes(result, shareBytes), "Output should contain share name");
    }

    [Fact]
    public void Create_LinkInfo_OutputIsLarger()
    {
        byte[] without = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\notepad.exe"
        });

        byte[] with = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\notepad.exe",
            LinkInfo = new LinkInfo
            {
                Local = new LocalPathInfo
                {
                    BasePath = @"C:\Windows\notepad.exe",
                    VolumeLabel = "C"
                }
            }
        });

        Assert.True(with.Length > without.Length, "Output with LinkInfo should be larger");
    }

    // --- IconEnvironmentDataBlock ---

    [Fact]
    public void Create_IconEnvironmentPath_ContainsBlock()
    {
        byte[] result = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\notepad.exe",
            IconEnvironmentPath = @"%SystemRoot%\system32\shell32.dll"
        });

        Assert.True(ContainsSignature(result, ICON_ENV_BLOCK_SIGNATURE),
            "IconEnvironmentDataBlock signature should be present");
    }

    [Fact]
    public void Create_WithoutIconEnvironmentPath_NoBlock()
    {
        byte[] result = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\notepad.exe"
        });

        Assert.False(ContainsSignature(result, ICON_ENV_BLOCK_SIGNATURE),
            "IconEnvironmentDataBlock signature should not be present");
    }

    [Fact]
    public void Create_IconEnvironmentPath_BlockIs788Bytes()
    {
        byte[] result = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\notepad.exe",
            IconEnvironmentPath = @"%SystemRoot%\system32\shell32.dll"
        });

        int blockOffset = FindSignatureOffset(result, ICON_ENV_BLOCK_SIGNATURE);
        Assert.True(blockOffset >= 0, "Block should be found");
        uint blockSize = BitConverter.ToUInt32(result, blockOffset - 4);
        Assert.Equal(788u, blockSize);
    }

    // --- KnownFolderDataBlock ---

    [Fact]
    public void Create_KnownFolder_ContainsBlock()
    {
        byte[] result = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\notepad.exe",
            KnownFolder = new KnownFolderData
            {
                FolderId = KnownFolderIds.Documents,
                Offset = 0
            }
        });

        Assert.True(ContainsSignature(result, KNOWN_FOLDER_BLOCK_SIGNATURE),
            "KnownFolderDataBlock signature should be present");
    }

    [Fact]
    public void Create_WithoutKnownFolder_NoBlock()
    {
        byte[] result = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\notepad.exe"
        });

        Assert.False(ContainsSignature(result, KNOWN_FOLDER_BLOCK_SIGNATURE),
            "KnownFolderDataBlock signature should not be present");
    }

    [Fact]
    public void Create_KnownFolder_ContainsCorrectGuid()
    {
        var folderId = KnownFolderIds.Documents;
        byte[] result = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\notepad.exe",
            KnownFolder = new KnownFolderData { FolderId = folderId }
        });

        int sigOffset = FindSignatureOffset(result, KNOWN_FOLDER_BLOCK_SIGNATURE);
        Assert.True(sigOffset >= 0, "Block should be found");

        // GUID starts right after signature (sigOffset + 4)
        byte[] guidBytes = new byte[16];
        Array.Copy(result, sigOffset + 4, guidBytes, 0, 16);
        Assert.Equal(folderId, new Guid(guidBytes));
    }

    [Fact]
    public void Create_KnownFolder_BlockIs28Bytes()
    {
        byte[] result = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\notepad.exe",
            KnownFolder = new KnownFolderData { FolderId = KnownFolderIds.Desktop }
        });

        int blockOffset = FindSignatureOffset(result, KNOWN_FOLDER_BLOCK_SIGNATURE);
        uint blockSize = BitConverter.ToUInt32(result, blockOffset - 4);
        Assert.Equal(28u, blockSize);
    }

    // --- TrackerDataBlock ---

    [Fact]
    public void Create_Tracker_ContainsBlock()
    {
        byte[] result = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\notepad.exe",
            Tracker = new TrackerData
            {
                MachineId = "WORKSTATION",
                VolumeId = Guid.NewGuid(),
                ObjectId = Guid.NewGuid()
            }
        });

        Assert.True(ContainsSignature(result, TRACKER_BLOCK_SIGNATURE),
            "TrackerDataBlock signature should be present");
    }

    [Fact]
    public void Create_WithoutTracker_NoBlock()
    {
        byte[] result = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\notepad.exe"
        });

        Assert.False(ContainsSignature(result, TRACKER_BLOCK_SIGNATURE),
            "TrackerDataBlock signature should not be present");
    }

    [Fact]
    public void Create_Tracker_BlockIs96Bytes()
    {
        byte[] result = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\notepad.exe",
            Tracker = new TrackerData
            {
                MachineId = "PC01",
                VolumeId = Guid.NewGuid(),
                ObjectId = Guid.NewGuid()
            }
        });

        int blockOffset = FindSignatureOffset(result, TRACKER_BLOCK_SIGNATURE);
        Assert.True(blockOffset >= 0);
        uint blockSize = BitConverter.ToUInt32(result, blockOffset - 4);
        Assert.Equal(96u, blockSize);
    }

    [Fact]
    public void Create_Tracker_ContainsMachineId()
    {
        string machineId = "MYPC";
        byte[] result = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\notepad.exe",
            Tracker = new TrackerData
            {
                MachineId = machineId,
                VolumeId = Guid.NewGuid(),
                ObjectId = Guid.NewGuid()
            }
        });

        byte[] machineBytes = Encoding.ASCII.GetBytes(machineId);
        Assert.True(ContainsBytes(result, machineBytes), "Output should contain machine ID");
    }

    [Fact]
    public void Create_Tracker_ContainsVolumeAndObjectIds()
    {
        var volumeId = Guid.NewGuid();
        var objectId = Guid.NewGuid();

        byte[] result = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\notepad.exe",
            Tracker = new TrackerData
            {
                MachineId = "PC",
                VolumeId = volumeId,
                ObjectId = objectId
            }
        });

        Assert.True(ContainsBytes(result, volumeId.ToByteArray()), "Output should contain volume GUID");
        Assert.True(ContainsBytes(result, objectId.ToByteArray()), "Output should contain object GUID");
    }

    [Fact]
    public void Create_Tracker_BirthIdsDefaultToMainIds()
    {
        var volumeId = Guid.NewGuid();
        var objectId = Guid.NewGuid();

        byte[] result = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\notepad.exe",
            Tracker = new TrackerData
            {
                MachineId = "PC",
                VolumeId = volumeId,
                ObjectId = objectId
                // BirthVolumeId and BirthObjectId are null — should default to main IDs
            }
        });

        // Volume and object IDs should each appear twice (main + birth)
        int volumeCount = CountOccurrences(result, volumeId.ToByteArray());
        int objectCount = CountOccurrences(result, objectId.ToByteArray());
        Assert.Equal(2, volumeCount);
        Assert.Equal(2, objectCount);
    }

    [Fact]
    public void Create_Tracker_CustomBirthIds()
    {
        var volumeId = Guid.NewGuid();
        var objectId = Guid.NewGuid();
        var birthVolumeId = Guid.NewGuid();
        var birthObjectId = Guid.NewGuid();

        byte[] result = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\notepad.exe",
            Tracker = new TrackerData
            {
                MachineId = "PC",
                VolumeId = volumeId,
                ObjectId = objectId,
                BirthVolumeId = birthVolumeId,
                BirthObjectId = birthObjectId
            }
        });

        Assert.True(ContainsBytes(result, volumeId.ToByteArray()));
        Assert.True(ContainsBytes(result, objectId.ToByteArray()));
        Assert.True(ContainsBytes(result, birthVolumeId.ToByteArray()));
        Assert.True(ContainsBytes(result, birthObjectId.ToByteArray()));
    }

    // --- PropertyStoreDataBlock ---

    [Fact]
    public void Create_PropertyStore_ContainsBlock()
    {
        byte[] storeData = { 0x01, 0x02, 0x03, 0x04 };
        byte[] result = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\notepad.exe",
            PropertyStoreData = storeData
        });

        Assert.True(ContainsSignature(result, PROPERTY_STORE_BLOCK_SIGNATURE),
            "PropertyStoreDataBlock signature should be present");
    }

    [Fact]
    public void Create_WithoutPropertyStore_NoBlock()
    {
        byte[] result = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\notepad.exe"
        });

        Assert.False(ContainsSignature(result, PROPERTY_STORE_BLOCK_SIGNATURE),
            "PropertyStoreDataBlock signature should not be present");
    }

    [Fact]
    public void Create_PropertyStore_ContainsRawData()
    {
        byte[] storeData = { 0xDE, 0xAD, 0xBE, 0xEF, 0xCA, 0xFE };
        byte[] result = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\notepad.exe",
            PropertyStoreData = storeData
        });

        Assert.True(ContainsBytes(result, storeData), "Output should contain raw property store data");
    }

    [Fact]
    public void Create_PropertyStore_BlockSizeIsCorrect()
    {
        byte[] storeData = new byte[100];
        byte[] result = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\notepad.exe",
            PropertyStoreData = storeData
        });

        int sigOffset = FindSignatureOffset(result, PROPERTY_STORE_BLOCK_SIGNATURE);
        uint blockSize = BitConverter.ToUInt32(result, sigOffset - 4);
        Assert.Equal((uint)(8 + storeData.Length), blockSize); // 4 size + 4 sig + data
    }

    // --- SpecialFolderDataBlock ---

    [Fact]
    public void Create_SpecialFolder_ContainsBlock()
    {
        byte[] result = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\notepad.exe",
            SpecialFolder = new SpecialFolderData { FolderId = 0x0024 } // CSIDL_WINDOWS
        });

        Assert.True(ContainsSignature(result, SPECIAL_FOLDER_BLOCK_SIGNATURE),
            "SpecialFolderDataBlock signature should be present");
    }

    [Fact]
    public void Create_WithoutSpecialFolder_NoBlock()
    {
        byte[] result = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\notepad.exe"
        });

        Assert.False(ContainsSignature(result, SPECIAL_FOLDER_BLOCK_SIGNATURE),
            "SpecialFolderDataBlock signature should not be present");
    }

    [Fact]
    public void Create_SpecialFolder_BlockIs16Bytes()
    {
        byte[] result = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\notepad.exe",
            SpecialFolder = new SpecialFolderData { FolderId = 0x0024 }
        });

        int blockOffset = FindSignatureOffset(result, SPECIAL_FOLDER_BLOCK_SIGNATURE);
        uint blockSize = BitConverter.ToUInt32(result, blockOffset - 4);
        Assert.Equal(16u, blockSize);
    }

    [Fact]
    public void Create_SpecialFolder_ContainsFolderId()
    {
        uint csidl = 0x0024;
        byte[] result = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\notepad.exe",
            SpecialFolder = new SpecialFolderData { FolderId = csidl, Offset = 42 }
        });

        int sigOffset = FindSignatureOffset(result, SPECIAL_FOLDER_BLOCK_SIGNATURE);
        uint folderId = BitConverter.ToUInt32(result, sigOffset + 4);
        uint offset = BitConverter.ToUInt32(result, sigOffset + 8);
        Assert.Equal(csidl, folderId);
        Assert.Equal(42u, offset);
    }

    // --- Multiple data blocks coexistence ---

    [Fact]
    public void Create_MultipleExtraDataBlocks_AllPresent()
    {
        byte[] result = Shortcut.Create(new ShortcutOptions
        {
            Target = @"%windir%\notepad.exe",
            IconEnvironmentPath = @"%SystemRoot%\system32\shell32.dll",
            KnownFolder = new KnownFolderData { FolderId = KnownFolderIds.Windows },
            SpecialFolder = new SpecialFolderData { FolderId = 0x0024 },
            Tracker = new TrackerData
            {
                MachineId = "PC",
                VolumeId = Guid.NewGuid(),
                ObjectId = Guid.NewGuid()
            },
            PropertyStoreData = new byte[] { 0x01, 0x02 }
        });

        Assert.True(ContainsSignature(result, ENV_BLOCK_SIGNATURE), "EnvironmentVariableDataBlock");
        Assert.True(ContainsSignature(result, ICON_ENV_BLOCK_SIGNATURE), "IconEnvironmentDataBlock");
        Assert.True(ContainsSignature(result, KNOWN_FOLDER_BLOCK_SIGNATURE), "KnownFolderDataBlock");
        Assert.True(ContainsSignature(result, SPECIAL_FOLDER_BLOCK_SIGNATURE), "SpecialFolderDataBlock");
        Assert.True(ContainsSignature(result, TRACKER_BLOCK_SIGNATURE), "TrackerDataBlock");
        Assert.True(ContainsSignature(result, PROPERTY_STORE_BLOCK_SIGNATURE), "PropertyStoreDataBlock");
    }

    // --- Terminal block always present ---

    [Fact]
    public void Create_WithAllFeatures_EndsWithTerminator()
    {
        byte[] result = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\notepad.exe",
            UseUnicode = true,
            Description = "Test",
            Arguments = "--flag",
            WorkingDirectory = @"C:\Windows",
            RelativePath = @".\notepad.exe",
            CreationTime = DateTime.UtcNow,
            FileSize = 500000,
            LinkInfo = new LinkInfo
            {
                Local = new LocalPathInfo { BasePath = @"C:\Windows\notepad.exe" }
            },
            KnownFolder = new KnownFolderData { FolderId = KnownFolderIds.Windows },
            Tracker = new TrackerData
            {
                MachineId = "PC",
                VolumeId = Guid.NewGuid(),
                ObjectId = Guid.NewGuid()
            }
        });

        uint terminator = BitConverter.ToUInt32(result, result.Length - 4);
        Assert.Equal(0u, terminator);
    }

    // --- Integration: all new features combined ---

    [Fact]
    public void Create_AllNewFeatures_CreatesValidOutput()
    {
        byte[] result = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\notepad.exe",
            UseUnicode = true,
            Description = "Test shortcut",
            Arguments = "test.txt",
            WorkingDirectory = @"C:\Windows",
            IconLocation = @"C:\Windows\notepad.exe",
            IconIndex = 1,
            WindowStyle = ShortcutWindowStyle.Maximized,
            RunAsAdmin = true,
            HotkeyKey = 0x54,
            HotkeyModifiers = HotkeyModifiers.Control | HotkeyModifiers.Alt,
            RelativePath = @".\notepad.exe",
            CreationTime = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            FileSize = 500000,
            LinkInfo = new LinkInfo
            {
                Local = new LocalPathInfo
                {
                    BasePath = @"C:\Windows\notepad.exe",
                    VolumeLabel = "C",
                    DriveSerialNumber = 0xABCDEF01
                }
            },
            KnownFolder = new KnownFolderData { FolderId = KnownFolderIds.Windows },
            Tracker = new TrackerData
            {
                MachineId = "WORKSTATION",
                VolumeId = Guid.NewGuid(),
                ObjectId = Guid.NewGuid()
            },
            SpecialFolder = new SpecialFolderData { FolderId = 0x0024 },
            PropertyStoreData = new byte[] { 0xAA, 0xBB, 0xCC }
        });

        // Structural validation
        Assert.Equal(HEADER_SIZE, BitConverter.ToUInt32(result, 0));

        // Flags
        uint linkFlags = BitConverter.ToUInt32(result, 20);
        Assert.True((linkFlags & 0x00000001) != 0, "HasLinkTargetIDList");
        Assert.True((linkFlags & 0x00000002) != 0, "HasLinkInfo");
        Assert.True((linkFlags & 0x00000004) != 0, "HasName");
        Assert.True((linkFlags & 0x00000008) != 0, "HasRelativePath");
        Assert.True((linkFlags & 0x00000010) != 0, "HasWorkingDir");
        Assert.True((linkFlags & 0x00000020) != 0, "HasArguments");
        Assert.True((linkFlags & 0x00000040) != 0, "HasIconLocation");
        Assert.True((linkFlags & 0x00000080) != 0, "IsUnicode");
        Assert.True((linkFlags & 0x00002000) != 0, "RunAsUser");

        // WindowStyle
        Assert.Equal(3u, BitConverter.ToUInt32(result, 60));

        // Hotkey
        Assert.Equal(0x54, result[64]);
        Assert.Equal(0x06, result[65]);

        // Timestamp
        Assert.Equal(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToFileTimeUtc(),
            BitConverter.ToInt64(result, 28));

        // File size
        Assert.Equal(500000u, BitConverter.ToUInt32(result, 52));

        // Terminator
        Assert.Equal(0u, BitConverter.ToUInt32(result, result.Length - 4));
    }

    // --- Helper methods ---

    private static bool ContainsBytes(byte[] haystack, byte[] needle)
    {
        for (int i = 0; i <= haystack.Length - needle.Length; i++)
        {
            bool match = true;
            for (int j = 0; j < needle.Length; j++)
            {
                if (haystack[i + j] != needle[j]) { match = false; break; }
            }
            if (match) return true;
        }
        return false;
    }

    private static bool ContainsSignature(byte[] data, uint signature)
    {
        for (int i = 0; i <= data.Length - 4; i++)
        {
            if (BitConverter.ToUInt32(data, i) == signature)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Finds the offset of the signature value. Returns -1 if not found.
    /// Searches for size+signature pair pattern.
    /// </summary>
    private static int FindSignatureOffset(byte[] data, uint signature)
    {
        for (int i = 4; i <= data.Length - 4; i++)
        {
            if (BitConverter.ToUInt32(data, i) == signature)
                return i;
        }
        return -1;
    }

    private static int CountOccurrences(byte[] haystack, byte[] needle)
    {
        int count = 0;
        for (int i = 0; i <= haystack.Length - needle.Length; i++)
        {
            bool match = true;
            for (int j = 0; j < needle.Length; j++)
            {
                if (haystack[i + j] != needle[j]) { match = false; break; }
            }
            if (match) count++;
        }
        return count;
    }
}
