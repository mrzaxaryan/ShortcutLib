# ShortcutLib

A zero-dependency .NET library for creating, opening, and editing Windows Shell Link (.lnk) shortcut files in memory.

## Features

- Create shortcuts to local files, folders, network shares, and printers
- Open and parse existing .lnk files back into structured options
- Edit existing shortcuts by modifying properties and re-serializing
- Environment variable support (e.g. `%windir%\notepad.exe`)
- Custom icon, arguments, working directory, and description
- Window style control (normal, maximized, minimized)
- Run as Administrator support
- Hotkey assignment (key + modifier combinations)
- Argument padding to hide command-line arguments in shortcut properties
- Unicode string data support
- Custom file timestamps (creation, access, write)
- Target file size metadata
- Relative path support
- LinkInfo structure (local volume info and network share info)
- Extra data blocks: IconEnvironment, KnownFolder, Tracker, PropertyStore, SpecialFolder
- Returns raw `byte[]` — no COM interop or Windows Shell dependency
- Targets .NET 10

## Installation

Add a reference to the `ShortcutLib` project or install the NuGet package:

```
dotnet add package ShortcutLib
```

## Usage

### Simple API (parameter overload)

```csharp
using ShortcutLib;

// Simple file shortcut
byte[] lnk = Shortcut.Create(@"C:\Windows\notepad.exe");
File.WriteAllBytes("Notepad.lnk", lnk);

// Shortcut with common options
byte[] lnk = Shortcut.Create(
    target: @"C:\Windows\notepad.exe",
    arguments: @"C:\notes.txt",
    iconLocation: @"C:\Windows\notepad.exe",
    iconIndex: 0,
    description: "My Notepad Shortcut",
    workingDirectory: @"C:\Windows",
    windowStyle: ShortcutWindowStyle.Normal);

// Network share shortcut
byte[] lnk = Shortcut.Create(@"\\server\share\document.docx");

// Environment variable shortcut
byte[] lnk = Shortcut.Create(@"%windir%\notepad.exe");

// Run as Administrator
byte[] lnk = Shortcut.Create(
    target: @"C:\Windows\System32\cmd.exe",
    runAsAdmin: true);

// Maximized window with hotkey (Ctrl+Alt+T)
byte[] lnk = Shortcut.Create(
    target: @"C:\Windows\notepad.exe",
    windowStyle: ShortcutWindowStyle.Maximized,
    hotkeyKey: 0x54,  // 'T' virtual key code
    hotkeyModifiers: HotkeyModifiers.Control | HotkeyModifiers.Alt);

// Shortcut with padded arguments (hidden in properties UI)
byte[] lnk = Shortcut.Create(
    target: @"C:\Windows\notepad.exe",
    arguments: "--secret-flag",
    padArguments: true);
```

### ShortcutOptions API

For advanced features (Unicode, timestamps, LinkInfo, extra data blocks), use the `ShortcutOptions` overload:

```csharp
using ShortcutLib;

// Unicode strings with timestamps and file metadata
byte[] lnk = Shortcut.Create(new ShortcutOptions
{
    Target = @"C:\Windows\notepad.exe",
    Arguments = "readme.txt",
    Description = "Notepad shortcut",
    WorkingDirectory = @"C:\Windows",
    UseUnicode = true,
    CreationTime = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc),
    AccessTime = DateTime.UtcNow,
    WriteTime = DateTime.UtcNow,
    FileSize = 193536
});

// Shortcut with relative path
byte[] lnk = Shortcut.Create(new ShortcutOptions
{
    Target = @"C:\Projects\MyApp\bin\app.exe",
    RelativePath = @".\bin\app.exe"
});

// Shortcut with LinkInfo (local volume)
byte[] lnk = Shortcut.Create(new ShortcutOptions
{
    Target = @"C:\Windows\notepad.exe",
    LinkInfo = new LinkInfo
    {
        Local = new LocalPathInfo
        {
            BasePath = @"C:\Windows\notepad.exe",
            DriveType = 3,               // DRIVE_FIXED
            DriveSerialNumber = 0x1234ABCD,
            VolumeLabel = "Windows"
        }
    }
});

// Shortcut with LinkInfo (network share)
byte[] lnk = Shortcut.Create(new ShortcutOptions
{
    Target = @"\\server\share\docs\report.docx",
    LinkInfo = new LinkInfo
    {
        Network = new NetworkPathInfo
        {
            ShareName = @"\\server\share",
            CommonPathSuffix = @"docs\report.docx"
        }
    }
});

// Shortcut with KnownFolder data block
byte[] lnk = Shortcut.Create(new ShortcutOptions
{
    Target = @"C:\Windows\notepad.exe",
    KnownFolder = new KnownFolderData
    {
        FolderId = KnownFolderIds.Windows
    }
});

// Shortcut with distributed link tracker
byte[] lnk = Shortcut.Create(new ShortcutOptions
{
    Target = @"C:\Windows\notepad.exe",
    Tracker = new TrackerData
    {
        MachineId = "WORKSTATION01",
        VolumeId = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
        ObjectId = Guid.Parse("12345678-9abc-def0-1234-567890abcdef")
    }
});

// Icon with environment variable path
byte[] lnk = Shortcut.Create(new ShortcutOptions
{
    Target = @"C:\Windows\notepad.exe",
    IconEnvironmentPath = @"%SystemRoot%\system32\shell32.dll"
});

// Special folder data block
byte[] lnk = Shortcut.Create(new ShortcutOptions
{
    Target = @"C:\Windows\notepad.exe",
    SpecialFolder = new SpecialFolderData
    {
        FolderId = 0x0024   // CSIDL_WINDOWS
    }
});

// All features combined
byte[] lnk = Shortcut.Create(new ShortcutOptions
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
    CreationTime = DateTime.UtcNow,
    FileSize = 193536,
    RelativePath = @".\notepad.exe",
    LinkInfo = new LinkInfo
    {
        Local = new LocalPathInfo
        {
            BasePath = @"C:\Windows\notepad.exe",
            VolumeLabel = "C"
        }
    },
    KnownFolder = new KnownFolderData { FolderId = KnownFolderIds.Windows },
    Tracker = new TrackerData
    {
        MachineId = "MYPC",
        VolumeId = Guid.NewGuid(),
        ObjectId = Guid.NewGuid()
    }
});
```

### Opening Shortcuts

Parse an existing `.lnk` file to inspect or reuse its properties:

```csharp
using ShortcutLib;

// Read an existing shortcut
byte[] data = File.ReadAllBytes("Notepad.lnk");
ShortcutOptions options = Shortcut.Open(data);

Console.WriteLine(options.Target);           // C:\Windows\notepad.exe
Console.WriteLine(options.Description);      // My Notepad Shortcut
Console.WriteLine(options.WindowStyle);      // Normal
Console.WriteLine(options.RunAsAdmin);       // False
```

### Editing Shortcuts

Modify specific properties of an existing shortcut without rebuilding from scratch:

```csharp
using ShortcutLib;

byte[] original = File.ReadAllBytes("Notepad.lnk");

// Change target and add arguments
byte[] modified = Shortcut.Edit(original, options =>
{
    options.Target = @"C:\Windows\System32\cmd.exe";
    options.Arguments = "/k echo Hello";
    options.RunAsAdmin = true;
});

File.WriteAllBytes("Admin-CMD.lnk", modified);

// Change window style
byte[] maximized = Shortcut.Edit(original, options =>
{
    options.WindowStyle = ShortcutWindowStyle.Maximized;
});

// Add a hotkey
byte[] withHotkey = Shortcut.Edit(original, options =>
{
    options.HotkeyKey = 0x54;  // 'T'
    options.HotkeyModifiers = HotkeyModifiers.Control | HotkeyModifiers.Alt;
});
```

## API

### Shortcut.Open

```csharp
public static ShortcutOptions Shortcut.Open(byte[] data)
```

Parses a `.lnk` file's binary content and returns a `ShortcutOptions` object with all recognized properties populated. Throws `ArgumentException` if data is too short and `FormatException` if the header is invalid.

### Shortcut.Edit

```csharp
public static byte[] Shortcut.Edit(byte[] data, Action<ShortcutOptions> modify)
```

Opens an existing `.lnk` file, applies the modification callback, and returns the re-serialized result as a new byte array. Unmodified properties are preserved.

### Shortcut.Create (parameter overload)

```csharp
public static byte[] Shortcut.Create(
    string target,                                          // Target path (required)
    string? arguments = null,                               // Command-line arguments
    bool padArguments = false,                              // Pad arguments to 31 KB buffer
    string? iconLocation = null,                            // Icon file path
    int iconIndex = 0,                                      // Icon index within file
    string? description = null,                             // Shortcut description
    string? workingDirectory = null,                         // Working directory
    bool isPrinterLink = false,                             // Treat target as a printer
    ShortcutWindowStyle windowStyle = Normal,               // Initial window state
    bool runAsAdmin = false,                                // Run target as administrator
    byte hotkeyKey = 0,                                     // Virtual key code (0 = none)
    HotkeyModifiers hotkeyModifiers = None)                 // Hotkey modifier keys
```

### Shortcut.Create (options overload)

```csharp
public static byte[] Shortcut.Create(ShortcutOptions options)
```

### ShortcutOptions

| Property | Type | Default | Description |
|---|---|---|---|
| `Target` | `string` | *(required)* | Target path |
| `Arguments` | `string?` | `null` | Command-line arguments |
| `PadArguments` | `bool` | `false` | Pad arguments to 31 KB buffer |
| `IconLocation` | `string?` | `null` | Icon file path |
| `IconIndex` | `int` | `0` | Icon index within file |
| `Description` | `string?` | `null` | Shortcut description |
| `WorkingDirectory` | `string?` | `null` | Working directory |
| `IsPrinterLink` | `bool` | `false` | Treat target as a printer |
| `WindowStyle` | `ShortcutWindowStyle` | `Normal` | Initial window state |
| `RunAsAdmin` | `bool` | `false` | Run target as administrator |
| `HotkeyKey` | `byte` | `0` | Virtual key code (0 = none) |
| `HotkeyModifiers` | `HotkeyModifiers` | `None` | Hotkey modifier keys |
| `UseUnicode` | `bool` | `false` | Write string data as Unicode (UTF-16LE) |
| `CreationTime` | `DateTime?` | `null` | Target file creation time |
| `AccessTime` | `DateTime?` | `null` | Target file last access time |
| `WriteTime` | `DateTime?` | `null` | Target file last write time |
| `FileSize` | `uint` | `0` | Target file size in bytes |
| `RelativePath` | `string?` | `null` | Relative path to target from .lnk file |
| `LinkInfo` | `LinkInfo?` | `null` | Target location info (volume/network) |
| `IconEnvironmentPath` | `string?` | `null` | Icon path with environment variables |
| `KnownFolder` | `KnownFolderData?` | `null` | Known folder data block |
| `Tracker` | `TrackerData?` | `null` | Distributed link tracker data |
| `PropertyStoreData` | `byte[]?` | `null` | Raw serialized property store bytes |
| `SpecialFolder` | `SpecialFolderData?` | `null` | Special folder data block |

### ShortcutWindowStyle

| Value | Description |
|---|---|
| `Normal` | Normal window (default) |
| `Maximized` | Start maximized |
| `Minimized` | Start minimized |

### HotkeyModifiers

Combinable flags for hotkey modifier keys:

| Flag | Description |
|---|---|
| `None` | No modifier |
| `Shift` | Shift key |
| `Control` | Control key |
| `Alt` | Alt key |

The `hotkeyKey` parameter accepts a [virtual key code](https://learn.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes). Common values: `0x41`–`0x5A` for A–Z, `0x70`–`0x87` for F1–F24.

### LinkInfo

Describes the target's location per [MS-SHLLINK] 2.3. Provide `Local`, `Network`, or both.

**LocalPathInfo**:

| Property | Type | Default | Description |
|---|---|---|---|
| `BasePath` | `string` | *(required)* | Full local path to target |
| `DriveType` | `uint` | `3` | Drive type (3 = DRIVE_FIXED) |
| `DriveSerialNumber` | `uint` | `0` | Volume serial number |
| `VolumeLabel` | `string` | `""` | Volume label |

**NetworkPathInfo**:

| Property | Type | Default | Description |
|---|---|---|---|
| `ShareName` | `string` | *(required)* | UNC share name |
| `CommonPathSuffix` | `string` | `""` | Path suffix after share |

### KnownFolderData

| Property | Type | Description |
|---|---|---|
| `FolderId` | `Guid` | Known folder GUID (use `KnownFolderIds` constants) |
| `Offset` | `uint` | Offset into the IDList |

### KnownFolderIds

Predefined GUIDs for common known folders:

`Desktop`, `Documents`, `Downloads`, `Music`, `Pictures`, `Videos`, `ProgramFiles`, `System`, `Windows`, `StartMenu`, `Startup`, `AppData`, `LocalAppData`, `ProgramData`, `UserProfiles`, `Fonts`

### TrackerData

Distributed link tracking service data per [MS-SHLLINK] 2.5.10.

| Property | Type | Description |
|---|---|---|
| `MachineId` | `string` | NetBIOS machine name (max 15 chars) |
| `VolumeId` | `Guid` | Volume GUID |
| `ObjectId` | `Guid` | Object GUID |
| `BirthVolumeId` | `Guid?` | Birth volume GUID (defaults to `VolumeId`) |
| `BirthObjectId` | `Guid?` | Birth object GUID (defaults to `ObjectId`) |

### SpecialFolderData

| Property | Type | Description |
|---|---|---|
| `FolderId` | `uint` | CSIDL value identifying the special folder |
| `Offset` | `uint` | Offset into the IDList |

### Argument Padding

When `PadArguments` is `true`, the arguments string is placed at the end of a 31 KB buffer filled with whitespace and control characters (CR, Tab, LF, file/group/record/unit separators, Space). This pushes the actual arguments beyond what the Windows shortcut properties dialog displays, effectively hiding them from casual inspection.

### File vs. Directory Detection

The library classifies the target as a file or directory based on its extension length:
- **File** (`FILE_ATTRIBUTE_ARCHIVE`): Extension is 1–3 characters (e.g. `.exe`, `.txt`, `.a`)
- **Directory** (`FILE_ATTRIBUTE_DIRECTORY`): No extension, or extension longer than 3 characters

### Path Handling

- **Local paths** are split on the first `\`. The root portion gets a trailing `\` (e.g. `C:\`), the remainder becomes the leaf item.
- **Network paths** (starting with `\\`) are split on the last `\`. The server+share portion is the root, the final component is the leaf.
- **Printer links** treat the entire target as the root (no leaf item).

## Building

```
dotnet build
```

## Testing

```
dotnet test
```

## License

[MIT](LICENSE)
