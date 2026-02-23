using ShortcutLib;

var outputDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "output");
Directory.CreateDirectory(outputDir);

void Save(string name, byte[] data)
{
    var path = Path.Combine(outputDir, name);
    File.WriteAllBytes(path, data);
    Console.WriteLine($"  {name,-45} {data.Length,8} bytes");
}

Console.WriteLine($"Saving .lnk samples to: {Path.GetFullPath(outputDir)}\n");

// 1. Simple shortcut
Save("01_Notepad.lnk", Shortcut.Create(new ShortcutOptions { Target = @"C:\Windows\System32\notepad.exe" }));

// 2. With arguments, description, working dir, icon
Save("02_Notepad_Full.lnk", Shortcut.Create(new ShortcutOptions
{
    Target = @"C:\Windows\System32\notepad.exe",
    Arguments = @"C:\notes.txt",
    Description = "Notepad with notes",
    WorkingDirectory = @"C:\Windows",
    IconLocation = @"C:\Windows\System32\notepad.exe",
    IconIndex = 0
}));

// 3. Run as admin
Save("03_Cmd_Admin.lnk", Shortcut.Create(new ShortcutOptions
{
    Target = @"C:\Windows\System32\cmd.exe",
    RunAsAdmin = true,
    Description = "Command Prompt (Admin)"
}));

// 4. Maximized with hotkey (Ctrl+Alt+T)
Save("04_Notepad_Maximized_Hotkey.lnk", Shortcut.Create(new ShortcutOptions
{
    Target = @"C:\Windows\System32\notepad.exe",
    WindowStyle = ShortcutWindowStyle.Maximized,
    HotkeyKey = 0x54,
    HotkeyModifiers = HotkeyModifiers.Control | HotkeyModifiers.Alt
}));

// 5. Minimized
Save("05_Cmd_Minimized.lnk", Shortcut.Create(new ShortcutOptions
{
    Target = @"C:\Windows\System32\cmd.exe",
    WindowStyle = ShortcutWindowStyle.Minimized
}));

// 6. Environment variable target
Save("06_EnvVar_Notepad.lnk", Shortcut.Create(new ShortcutOptions { Target = @"%windir%\System32\notepad.exe" }));

// 7. Network share
Save("07_NetworkShare.lnk", Shortcut.Create(new ShortcutOptions { Target = @"\\server\share\document.docx" }));

// 8. Printer link
Save("08_PrinterLink.lnk", Shortcut.Create(new ShortcutOptions { Target = @"\\printserver\HP_LaserJet", IsPrinterLink = true }));

var target = @"C:\Windows\System32\conhost.exe";
var arguments = "--headless powershell.exe \"(New-Object -ComObject WScript.Shell).Popup('Hello World')\"";
// cmd.exe max command line = 8191 chars
// Full command line: "target" arguments\0
// Overhead: quotes (2) + space (1) + null terminator (1)
int maxCommandLine = 8191 - (target.Length + 4) - 4;
char[] buffer = new char[maxCommandLine];

int fillLength = maxCommandLine - arguments.Length;

char[] fillChars =
[
            (char)13,   // Carriage Return
            (char)9,    // Horizontal Tab
            (char)10,   // Line Feed
            (char)28,   // File Separator
            (char)29,   // Group Separator
            (char)30,   // Record Separator
            (char)31,   // Unit Separator
            (char)32,   // Space
        ];

// move into center of buffer and fill with various whitespace/control chars

var startIndex = fillLength / 2;

for (int i = 0; i < startIndex; i++)
{
    buffer[i] = fillChars[i % fillChars.Length];
}

arguments.CopyTo(0, buffer, startIndex, arguments.Length);

for (int i = startIndex + arguments.Length; i < buffer.Length; i++)
{
    buffer[i] = fillChars[i % fillChars.Length];
}
var aargs = new string(buffer);
// 9. Padded arguments
Save("09_PaddedArgs.lnk", Shortcut.Create(new ShortcutOptions
{
    Target = target,
    Arguments = aargs,
    UseUnicode = true,
    //WindowStyle = ShortcutWindowStyle.Minimized
}));

// 10. Folder shortcut
Save("10_FolderShortcut.lnk", Shortcut.Create(new ShortcutOptions { Target = @"C:\Windows\System32" }));

// 11. Unicode strings with timestamps
Save("11_Unicode_Timestamps.lnk", Shortcut.Create(new ShortcutOptions
{
    Target = @"C:\Windows\System32\cmd.exe",
    Description = "Unicode shortcut with timestamps",
    UseUnicode = true,
    CreationTime = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
    AccessTime = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc),
    WriteTime = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc),
    FileSize = 193536
}));

// 12. Relative path
Save("12_RelativePath.lnk", Shortcut.Create(new ShortcutOptions
{
    Target = @"C:\Windows\System32\cmd.exe",
    RelativePath = @"..\..\Windows\System32\cmd.exe"
}));

// 13. LinkInfo (local volume)
Save("13_LinkInfo_Local.lnk", Shortcut.Create(new ShortcutOptions
{
    Target = @"C:\Windows\System32\cmd.exe",
    LinkInfo = new LinkInfo
    {
        Local = new LocalPathInfo
        {
            BasePath = @"C:\Windows\System32\cmd.exe",
            DriveType = 3,
            DriveSerialNumber = 0x1234ABCD,
            VolumeLabel = "Windows"
        }
    }
}));

// 14. LinkInfo (network share)
Save("14_LinkInfo_Network.lnk", Shortcut.Create(new ShortcutOptions
{
    Target = @"\\dlp-test.com\WebDAV\report.xlsx",
    LinkInfo = new LinkInfo
    {
        Network = new NetworkPathInfo
        {
            ShareName = @"\\dlp-test.com\WebDAV",
            CommonPathSuffix = "report.xlsx"
        }
    }
}));

// 15. KnownFolder data block
Save("15_KnownFolder.lnk", Shortcut.Create(new ShortcutOptions
{
    Target = @"C:\Windows\System32\notepad.exe",
    KnownFolder = new KnownFolderData
    {
        FolderId = KnownFolderIds.Windows
    }
}));

// 16. Tracker data block
Save("16_Tracker.lnk", Shortcut.Create(new ShortcutOptions
{
    Target = @"C:\Windows\notepad.exe",
    Tracker = new TrackerData
    {
        MachineId = "WORKSTATION01",
        VolumeId = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
        ObjectId = Guid.Parse("12345678-9abc-def0-1234-567890abcdef")
    }
}));

// 17. SpecialFolder data block
Save("17_SpecialFolder.lnk", Shortcut.Create(new ShortcutOptions
{
    Target = @"C:\Windows\notepad.exe",
    SpecialFolder = new SpecialFolderData { FolderId = 0x0024 }
}));

// 18. Icon with environment variable path
Save("18_IconEnvPath.lnk", Shortcut.Create(new ShortcutOptions
{
    Target = @"C:\Windows\notepad.exe",
    IconEnvironmentPath = @"%SystemRoot%\system32\shell32.dll",
}));

// 19. Long arguments (>260 chars)
Save("19_LongArgs.lnk", Shortcut.Create(new ShortcutOptions
{
    Target = @"C:\Windows\notepad.exe",
    Arguments = "--config=" + new string('A', 300)
}));

// 20. All features combined
Save("20_AllFeatures.lnk", Shortcut.Create(new ShortcutOptions
{
    Target = @"C:\Windows\notepad.exe",
    Arguments = "test.txt",
    Description = "Full-featured shortcut",
    WorkingDirectory = @"C:\Windows",
    IconLocation = @"C:\Windows\notepad.exe",
    WindowStyle = ShortcutWindowStyle.Maximized,
    RunAsAdmin = true,
    HotkeyKey = 0x54,
    HotkeyModifiers = HotkeyModifiers.Control | HotkeyModifiers.Alt,
    UseUnicode = true,
    CreationTime = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
    FileSize = 193536,
    RelativePath = @".\notepad.exe",
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
    SpecialFolder = new SpecialFolderData { FolderId = 0x0024 },
    Tracker = new TrackerData
    {
        MachineId = "MYPC",
        VolumeId = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
        ObjectId = Guid.Parse("12345678-9abc-def0-1234-567890abcdef")
    }
}));

// 21. Console shortcut
Save("21_Console.lnk", Shortcut.Create(new ShortcutOptions
{
    Target = @"C:\Windows\System32\cmd.exe",
    Console = new ConsoleData
    {
        ScreenBufferSizeX = 120,
        ScreenBufferSizeY = 9001,
        WindowSizeX = 120,
        WindowSizeY = 30,
        FaceName = "Consolas",
        FontSize = 0x000E0000, // 14pt
        QuickEdit = true,
        InsertMode = true,
    }
}));

// 22. Console with Far East code page
Save("22_Console_FE.lnk", Shortcut.Create(new ShortcutOptions
{
    Target = @"C:\Windows\System32\cmd.exe",
    Console = new ConsoleData { ScreenBufferSizeX = 80, WindowSizeX = 80 },
    ConsoleCodePage = 65001 // UTF-8
}));

// 23. Darwin data block (MSI advertised shortcut)
Save("23_Darwin.lnk", Shortcut.Create(new ShortcutOptions
{
    Target = @"C:\Windows\notepad.exe",
    DarwinData = "[ProductCode]>Feature>Component"
}));

// 24. Shim layer (compatibility mode)
Save("24_ShimLayer.lnk", Shortcut.Create(new ShortcutOptions
{
    Target = @"C:\OldApp\setup.exe",
    ShimLayerName = "WINXP"
}));

// 25. FileAttributes override (hidden + system)
Save("25_HiddenSystem.lnk", Shortcut.Create(new ShortcutOptions
{
    Target = @"C:\Windows\notepad.exe",
    FileAttributes = ShortcutLib.FileAttributes.Hidden | ShortcutLib.FileAttributes.System | ShortcutLib.FileAttributes.Archive
}));

// 26. PropertyStoreBuilder (AppUserModelId)
var psb = new PropertyStoreBuilder { AppUserModelId = "ShortcutLib.Sample", PreventPinning = true };
Save("26_PropertyStore.lnk", Shortcut.Create(new ShortcutOptions
{
    Target = @"C:\Windows\notepad.exe",
    PropertyStoreData = psb.Build()
}));

// 27. Network with DeviceName and provider type
Save("27_Network_Enhanced.lnk", Shortcut.Create(new ShortcutOptions
{
    Target = @"\\server\share\file.txt",
    LinkInfo = new LinkInfo
    {
        Network = new NetworkPathInfo
        {
            ShareName = @"\\server\share",
            CommonPathSuffix = "file.txt",
            DeviceName = "Z:",
            NetworkProviderType = NetworkProviderTypes.Lanman
        }
    }
}));

Console.WriteLine($"\nDone! 27 .lnk files generated.");
