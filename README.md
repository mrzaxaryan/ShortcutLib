# ShortcutLib

[![Build & Test](https://github.com/mrzaxaryan/ShortcutLib/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/mrzaxaryan/ShortcutLib/actions/workflows/build-and-test.yml)
[![NuGet](https://img.shields.io/nuget/v/ShortcutLib?style=flat-square&logo=nuget&color=004880)](https://www.nuget.org/packages/ShortcutLib)
![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white)
![License: MIT](https://img.shields.io/badge/License-MIT-yellow?style=for-the-badge)
![Zero Dependencies](https://img.shields.io/badge/Dependencies-Zero-brightgreen?style=for-the-badge)
![MS-SHLLINK](https://img.shields.io/badge/MS--SHLLINK-Spec%20Compliant-blue?style=for-the-badge)

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
- Extra data blocks: Console, ConsoleFE, Darwin (MSI), Shim, IconEnvironment, KnownFolder, Tracker, PropertyStore, SpecialFolder, VistaAndAboveIDList
- Unknown extra data block preservation (round-trips unrecognized blocks)
- All 27 LinkFlags from the MS-SHLLINK spec (AllowLinkToLink, ForceNoLinkTrack, etc.)
- FileAttributes enum (ReadOnly, Hidden, System, Encrypted, etc.)
- Network path enhancements (DeviceName, NetworkProviderType)
- LinkInfo Unicode support (0x24 header) for non-ASCII paths, volume labels, and share names
- PropertyStoreBuilder for typed AppUserModelID, ToastActivatorCLSID, PreventPinning, and 30+ additional properties
- PropertyStoreReader for parsing/deserializing property store binary data back into typed entries
- 10 VT types: VT_LPWSTR, VT_BOOL, VT_UI4, VT_CLSID, VT_I2, VT_I4, VT_UI2, VT_I8, VT_UI8, VT_FILETIME, VT_BLOB, VT_LPSTR
- Arbitrary named property support in PropertyStoreBuilder (string, bool, int, uint, DateTime, blob, etc.)
- WinXHasher for computing WinX Power User Menu hashes
- DarwinDescriptor for decoding MSI advertised shortcut descriptors (product/component GUIDs, feature ID)
- TrackerData forensic extraction (MAC address, timestamp from Version 1 UUIDs)
- Named constants: DriveTypes, CsidlFolderIds, VirtualKeys, ConsoleFillAttributes, ConsoleFontFamilies, ShimLayerNames
- ShortcutSanitizer for stripping privacy-sensitive metadata (machine name, MAC address, unknown blocks, etc.)
- Overlay data (post-terminal block data) preservation
- Returns raw `byte[]` — no COM interop or Windows Shell dependency
- Targets .NET 10

## Installation

Add a reference to the `ShortcutLib` project or install the NuGet package:

```
dotnet add package ShortcutLib
```

## Usage

### Creating Shortcuts

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
            DriveType = DriveTypes.Fixed,
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
        FolderId = CsidlFolderIds.Windows
    }
});

// Console shortcut with display settings
byte[] lnk = Shortcut.Create(new ShortcutOptions
{
    Target = @"C:\Windows\System32\cmd.exe",
    Console = new ConsoleData
    {
        ScreenBufferSizeX = 120,
        ScreenBufferSizeY = 9001,
        WindowSizeX = 120,
        WindowSizeY = 30,
        FaceName = "Consolas",
        QuickEdit = true
    },
    ConsoleCodePage = 65001   // UTF-8
});

// MSI advertised shortcut (Darwin data block)
byte[] lnk = Shortcut.Create(new ShortcutOptions
{
    Target = @"C:\Windows\notepad.exe",
    DarwinData = "[ProductCode]>Feature>Component"
});

// App compatibility shim
byte[] lnk = Shortcut.Create(new ShortcutOptions
{
    Target = @"C:\OldApp\setup.exe",
    ShimLayerName = ShimLayerNames.WinXPSP3
});

// Explicit file attributes
byte[] lnk = Shortcut.Create(new ShortcutOptions
{
    Target = @"C:\Windows\notepad.exe",
    FileAttributes = FileAttributes.Hidden | FileAttributes.System | FileAttributes.Archive
});

// Network share with mapped drive and provider
byte[] lnk = Shortcut.Create(new ShortcutOptions
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
});

// Typed property store (AppUserModelId for taskbar grouping)
var builder = new PropertyStoreBuilder
{
    AppUserModelId = "MyCompany.MyApp",
    PreventPinning = true
};
byte[] lnk = Shortcut.Create(new ShortcutOptions
{
    Target = @"C:\MyApp\app.exe",
    PropertyStoreData = builder.Build()
});

// PropertyStore with System.Link and named properties
var builder = new PropertyStoreBuilder
{
    AppUserModelId = "MyCompany.MyApp",
    TargetParsingPath = @"C:\MyApp\app.exe",
    ItemTypeText = "Application"
};
builder.AddNamedStringProperty("CustomTag", "MyValue");
byte[] lnk = Shortcut.Create(new ShortcutOptions
{
    Target = @"C:\MyApp\app.exe",
    PropertyStoreData = builder.Build()
});

// Unicode LinkInfo for non-ASCII paths (auto-detects when needed)
byte[] lnk = Shortcut.Create(new ShortcutOptions
{
    Target = @"C:\Users\ユーザー\Documents\レポート.docx",
    LinkInfo = new LinkInfo
    {
        Local = new LocalPathInfo
        {
            BasePath = @"C:\Users\ユーザー\Documents\レポート.docx",
            VolumeLabel = "データ"
        }
    }
});

// Force Unicode LinkInfo explicitly
byte[] lnk = Shortcut.Create(new ShortcutOptions
{
    Target = @"C:\test.exe",
    UseUnicodeLinkInfo = true,
    LinkInfo = new LinkInfo
    {
        Local = new LocalPathInfo { BasePath = @"C:\test.exe" }
    }
});

// WinX Power User Menu shortcut (Win+X menu)
byte[] propStore = WinXHasher.BuildPropertyStore(@"C:\Windows\System32\cmd.exe");
byte[] lnk = Shortcut.Create(new ShortcutOptions
{
    Target = @"C:\Windows\System32\cmd.exe",
    PropertyStoreData = propStore
});

// Parse property store data from an existing shortcut
ShortcutOptions opts = Shortcut.Open(File.ReadAllBytes("shortcut.lnk"));
if (opts.PropertyStoreData != null)
{
    var entries = PropertyStoreReader.Parse(opts.PropertyStoreData);
    foreach (var entry in entries)
        Console.WriteLine($"{entry.FormatId} PID={entry.PropertyId} Name={entry.Name} Value={entry.Value}");
}

// Decode a Darwin (MSI) descriptor
var darwin = DarwinDescriptor.TryDecode(opts.DarwinData);
if (darwin != null)
{
    Console.WriteLine($"Product: {darwin.ProductCode}");
    Console.WriteLine($"Feature: {darwin.FeatureId}");
    Console.WriteLine($"Component: {darwin.ComponentCode}");
}

// Forensic extraction from TrackerData
if (opts.Tracker != null)
{
    Console.WriteLine($"MAC: {opts.Tracker.ExtractMacAddressString()}");
    Console.WriteLine($"Timestamp: {opts.Tracker.ExtractTimestamp()}");
    Console.WriteLine($"Birth MAC: {opts.Tracker.ExtractBirthMacAddressString()}");
}

// Preserve unknown extra data blocks during round-trip
var parsed = Shortcut.Open(File.ReadAllBytes("shortcut.lnk"));
// Any unrecognized extra data blocks are preserved in UnknownExtraDataBlocks
byte[] roundTripped = Shortcut.Create(parsed);

// Strip privacy-sensitive metadata (machine name, MAC address, unknown blocks, etc.)
byte[] original = File.ReadAllBytes("shortcut.lnk");
byte[] sanitized = ShortcutSanitizer.SanitizeBytes(original);
File.WriteAllBytes("clean.lnk", sanitized);

// Allow shortcut to link to another .lnk file
byte[] lnk = Shortcut.Create(new ShortcutOptions
{
    Target = @"C:\Users\Desktop\other.lnk",
    AllowLinkToLink = true
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
    HotkeyKey = VirtualKeys.T,
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
    options.HotkeyKey = VirtualKeys.T;
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

### Shortcut.Create

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
| `Console` | `ConsoleData?` | `null` | Console display settings |
| `ConsoleCodePage` | `uint?` | `null` | Far East console code page |
| `DarwinData` | `string?` | `null` | MSI advertised shortcut descriptor |
| `ShimLayerName` | `string?` | `null` | App compatibility layer (e.g. `"WINXP"`) |
| `VistaIdListData` | `byte[]?` | `null` | Vista+ alternative IDList |
| `OverlayData` | `byte[]?` | `null` | Data after terminal block |
| `UnknownExtraDataBlocks` | `List<RawExtraDataBlock>?` | `null` | Preserved unrecognized extra data blocks |
| `UseUnicodeLinkInfo` | `bool?` | `null` | Force Unicode (0x24) LinkInfo header; null = auto-detect |
| `FileAttributes` | `FileAttributes?` | `null` | Explicit file attributes (auto-detected when null) |
| `ForceNoLinkInfo` | `bool` | `false` | Ignore LinkInfo during resolution |
| `RunInSeparateProcess` | `bool` | `false` | 16-bit target in separate VDM |
| `NoPidlAlias` | `bool` | `false` | No shell namespace alias |
| `ForceNoLinkTrack` | `bool` | `false` | Ignore TrackerDataBlock |
| `EnableTargetMetadata` | `bool` | `false` | Populate PropertyStore on target set |
| `DisableLinkPathTracking` | `bool` | `false` | Ignore EnvironmentVariableDataBlock |
| `DisableKnownFolderTracking` | `bool` | `false` | Ignore KnownFolder/SpecialFolder |
| `DisableKnownFolderAlias` | `bool` | `false` | Use unaliased known folder IDList |
| `AllowLinkToLink` | `bool` | `false` | Allow shortcut to another .lnk |
| `UnaliasOnSave` | `bool` | `false` | Unalias target IDList on save |
| `KeepLocalIDListForUNCTarget` | `bool` | `false` | Store local IDList for UNC targets |

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

Use `VirtualKeys` constants for `HotkeyKey`: `VirtualKeys.A`–`VirtualKeys.Z`, `VirtualKeys.D0`–`VirtualKeys.D9`, `VirtualKeys.F1`–`VirtualKeys.F24`, `VirtualKeys.NumLock`, `VirtualKeys.ScrollLock`.

### LinkInfo

Describes the target's location per [MS-SHLLINK] 2.3. Provide `Local`, `Network`, or both.

**LocalPathInfo**:

| Property | Type | Default | Description |
|---|---|---|---|
| `BasePath` | `string` | *(required)* | Full local path to target |
| `DriveType` | `uint` | `DriveTypes.Fixed` | Drive type (see `DriveTypes`) |
| `DriveSerialNumber` | `uint` | `0` | Volume serial number |
| `VolumeLabel` | `string` | `""` | Volume label |

**NetworkPathInfo**:

| Property | Type | Default | Description |
|---|---|---|---|
| `ShareName` | `string` | *(required)* | UNC share name |
| `CommonPathSuffix` | `string` | `""` | Path suffix after share |
| `DeviceName` | `string?` | `null` | Mapped drive letter (e.g. `"Z:"`) |
| `NetworkProviderType` | `uint?` | `null` | Network provider (see `NetworkProviderTypes`) |

### KnownFolderData

| Property | Type | Description |
|---|---|---|
| `FolderId` | `Guid` | Known folder GUID (use `KnownFolderIds` constants) |
| `Offset` | `uint` | Offset into the IDList |

### KnownFolderIds

Predefined GUIDs for common known folders (55 constants):

**User folders:** `Desktop`, `Documents`, `Downloads`, `Music`, `Pictures`, `Videos`, `Profile`, `SavedGames`, `Contacts`, `Searches`, `Favorites`, `Links`, `Templates`

**System:** `ProgramFiles`, `ProgramFilesX86`, `ProgramFilesCommon`, `ProgramFilesCommonX86`, `System`, `SystemX86`, `Windows`, `Fonts`

**Application data:** `AppData`, `LocalAppData`, `LocalAppDataLow`, `ProgramData`

**Start menu:** `StartMenu`, `Programs`, `Startup`, `AdminTools`

**Common (all-users):** `CommonStartMenu`, `CommonPrograms`, `CommonStartup`, `CommonDesktopDir`, `CommonTemplates`, `CommonAdminTools`

**Public:** `UserProfiles`, `Public`, `PublicDesktop`, `PublicDocuments`, `PublicDownloads`, `PublicMusic`, `PublicPictures`, `PublicVideos`

**Shell locations:** `RecycleBin`, `QuickLaunch`, `SendTo`, `Recent`, `PrintHood`, `NetHood`, `Cookies`, `History`, `InternetCache`, `UserProgramFiles`

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

### FileAttributes

`[Flags]` enum for explicit target file attributes:

`ReadOnly`, `Hidden`, `System`, `Directory`, `Archive`, `Normal`, `Temporary`, `SparseFile`, `ReparsePoint`, `Compressed`, `Offline`, `NotContentIndexed`, `Encrypted`

When `ShortcutOptions.FileAttributes` is null, attributes are auto-detected from the target path.

### ConsoleData

Console display settings for ConsoleDataBlock (signature `0xA0000002`, 204 bytes).

| Property | Type | Default | Description |
|---|---|---|---|
| `FillAttributes` | `ushort` | `0` | Text foreground/background color |
| `PopupFillAttributes` | `ushort` | `0` | Popup color |
| `ScreenBufferSizeX` | `short` | `80` | Buffer width (columns) |
| `ScreenBufferSizeY` | `short` | `300` | Buffer height (rows) |
| `WindowSizeX` | `short` | `80` | Window width (columns) |
| `WindowSizeY` | `short` | `25` | Window height (rows) |
| `WindowOriginX/Y` | `short` | `0` | Window position |
| `FontSize` | `uint` | `0` | Font height (high) + width (low) |
| `FontFamily` | `uint` | `0` | Font family + pitch flags |
| `FontWeight` | `uint` | `0` | 400=normal, 700=bold |
| `FaceName` | `string` | `"Consolas"` | Font name (max 31 chars) |
| `CursorSize` | `uint` | `25` | Cursor size % (1-100) |
| `FullScreen` | `bool` | `false` | Full screen mode |
| `QuickEdit` | `bool` | `false` | Mouse selection |
| `InsertMode` | `bool` | `false` | Insert mode |
| `AutoPosition` | `bool` | `true` | Auto-position window |
| `HistoryBufferSize` | `uint` | `50` | History buffer size |
| `NumberOfHistoryBuffers` | `uint` | `4` | History buffer count |
| `HistoryNoDup` | `bool` | `false` | Remove history duplicates |
| `ColorTable` | `uint[16]` | *(classic palette)* | 16 RGB colors (0x00BBGGRR) |

### NetworkProviderTypes

Well-known `WNNC_NET_*` constants (45 values): `Lanman`, `Netware`, `SunPcNfs`, `Vines`, `Avid`, `Docuspace`, `Mangosoft`, `Sernet`, `Riverfront1`, `Riverfront2`, `Decorb`, `Protstor`, `FjRedir`, `Distinct`, `Twins`, `Rdr2Sample`, `Csc`, `ThreeIn1`, `ExtendNet`, `Stac`, `Foxbat`, `Yahoo`, `Exifs`, `Dav`, `Knoware`, `ObjectDire`, `Masfax`, `HobNfs`, `Shiva`, `Ibmal`, `Lock`, `TerminalServices`, `Srt`, `Quincy`, `OpenAfs`, `Avid1`, `Dfs`, `Kwnp`, `Zenworks`, `DriveOnWeb`, `VMware`, `Rsfx`, `Mfiles`, `MsNfs`, `Google`

### PropertyStoreBuilder

Builds typed MS-PROPSTORE data for `ShortcutOptions.PropertyStoreData`:

```csharp
var builder = new PropertyStoreBuilder
{
    AppUserModelId = "MyCompany.MyApp",
    PreventPinning = true,
    ToastActivatorCLSID = Guid.Parse("..."),
    RelaunchCommand = "myapp.exe --relaunch"
};
byte[] lnk = Shortcut.Create(new ShortcutOptions
{
    Target = @"C:\MyApp\app.exe",
    PropertyStoreData = builder.Build()
});
```

**AppUserModel properties** (Format ID: `9F4C2855-9CDB-4D7B-82BF-440971C8D266`):

| Property | Type | Description |
|---|---|---|
| `AppUserModelId` | `string?` | Taskbar grouping / jump list ID (PID 5) |
| `ToastActivatorCLSID` | `Guid?` | Toast notification COM CLSID (PID 26) |
| `PreventPinning` | `bool?` | Prevent taskbar/Start pinning (PID 9) |
| `RelaunchCommand` | `string?` | Taskbar relaunch command (PID 2) |
| `RelaunchDisplayNameResource` | `string?` | Relaunch display name (PID 4) |
| `RelaunchIconResource` | `string?` | Relaunch icon (PID 3) |
| `ExcludeFromShowInNewInstall` | `bool?` | Hide from "New programs" list (PID 8) |
| `IsDestListSeparator` | `bool?` | Jump list separator (PID 6) |
| `IsDestListLink` | `bool?` | Destination list link (PID 7) |
| `BestShortcut` | `bool?` | Best shortcut flag (PID 10) |
| `IsDualMode` | `bool?` | Dual mode app flag (PID 11) |
| `StartPinOption` | `uint?` | Start pin option (PID 12) |
| `PackageRelativeApplicationID` | `string?` | Package-relative app ID (PID 13) |
| `HostEnvironment` | `uint?` | Host environment (PID 14) |
| `PackageFamilyName` | `string?` | Package family name (PID 15) |
| `PackageFullName` | `string?` | Package full name (PID 16) |
| `PackageInstallPath` | `string?` | Package install path (PID 17) |
| `InstalledBy` | `string?` | Installed by (PID 18) |
| `RecordState` | `uint?` | Record state (PID 19) |
| `ParentID` | `string?` | Parent ID (PID 20) |
| `Relevance` | `uint?` | Relevance (PID 21) |
| `DestListProvidedTitle` | `string?` | Destination list title (PID 22) |
| `DestListProvidedDescription` | `string?` | Destination list description (PID 23) |
| `DestListProvidedGroupName` | `string?` | Destination list group name (PID 24) |
| `DestListLogoUri` | `string?` | Destination list logo URI (PID 25) |
| `RunFlags` | `uint?` | Run flags (PID 27) |
| `ActivationContext` | `string?` | Activation context (PID 28) |
| `VisualElementsManifestHintPath` | `string?` | Visual elements manifest hint path (PID 29) |
| `ExcludedFromLauncher` | `bool?` | Excluded from launcher (PID 30) |
| `FeatureOnDemand` | `bool?` | Feature on demand (PID 31) |
| `TileUniqueId` | `string?` | Tile unique ID (PID 32) |

**System.Link properties** (Format ID: `B9B4B3FC-2B51-4A42-B5D8-324146AFCF25`):

| Property | Type | Description |
|---|---|---|
| `TargetParsingPath` | `string?` | Canonical target path (PID 2) |
| `LinkComment` | `string?` | Link comment (PID 3) |
| `DateVisited` | `DateTime?` | Date visited (PID 4, VT_FILETIME) |
| `FeedUrl` | `string?` | Feed URL (PID 5) |
| `LinkStatus` | `int?` | Link status (PID 6, VT_I4) |
| `TargetSFGAOFlags` | `uint?` | Shell attributes (PID 8) |

**Other storage properties**:

| Property | Type | Description |
|---|---|---|
| `ItemTypeText` | `string?` | System.ItemTypeText |
| `MimeType` | `string?` | System.MIMEType |
| `TargetUrl` | `string?` | System.Link.TargetUrl |
| `TargetExtension` | `string?` | System.Link.TargetExtension |
| `WinXHash` | `uint?` | WinX Power User Menu hash |

**Named properties** (arbitrary string-keyed name/value pairs):

```csharp
var builder = new PropertyStoreBuilder();
builder.AddNamedStringProperty("CustomKey", "CustomValue")
       .AddNamedUInt32Property("Count", 42)
       .AddNamedBoolProperty("Enabled", true)
       .AddNamedInt32Property("Status", -1)
       .AddNamedFileTimeProperty("Timestamp", DateTime.UtcNow);
```

| Method | Description |
|---|---|
| `AddNamedStringProperty(name, value)` | Named string (VT_LPWSTR) |
| `AddNamedUInt32Property(name, value)` | Named uint32 (VT_UI4) |
| `AddNamedBoolProperty(name, value)` | Named bool (VT_BOOL) |
| `AddNamedInt32Property(name, value)` | Named int32 (VT_I4) |
| `AddNamedInt16Property(name, value)` | Named int16 (VT_I2) |
| `AddNamedUInt16Property(name, value)` | Named uint16 (VT_UI2) |
| `AddNamedInt64Property(name, value)` | Named int64 (VT_I8) |
| `AddNamedUInt64Property(name, value)` | Named uint64 (VT_UI8) |
| `AddNamedFileTimeProperty(name, value)` | Named DateTime (VT_FILETIME) |
| `AddNamedAnsiStringProperty(name, value)` | Named ANSI string (VT_LPSTR) |
| `AddNamedBlobProperty(name, value)` | Named byte[] (VT_BLOB) |

### ShortcutSanitizer

Strips privacy-sensitive metadata from shortcut files. LNK files may contain forensic artifacts: machine name and MAC address (TrackerData), file owner and computer name (PropertyStoreData), and unstructured data after the terminal block (OverlayData).

```csharp
// Sanitize in-place
ShortcutSanitizer.Sanitize(options);

// Or sanitize a raw byte array
byte[] clean = ShortcutSanitizer.SanitizeBytes(File.ReadAllBytes("shortcut.lnk"));
```

### PropertyStoreReader

Parses serialized MS-PROPSTORE binary data back into typed entries:

```csharp
var entries = PropertyStoreReader.Parse(options.PropertyStoreData);
foreach (var entry in entries)
{
    Console.WriteLine($"FormatId={entry.FormatId} PID={entry.PropertyId} Name={entry.Name}");
    Console.WriteLine($"  VtType={entry.VtType} Value={entry.Value}");
}
```

Returns a `List<PropertyStoreEntry>` with each entry containing:

| Property | Type | Description |
|---|---|---|
| `FormatId` | `Guid` | Property storage format ID |
| `PropertyId` | `uint?` | Numeric property ID (null for named properties) |
| `Name` | `string?` | Property name (null for PID-based properties) |
| `VtType` | `ushort` | VARIANT type code |
| `Value` | `object?` | Deserialized value (string, bool, uint, int, Guid, DateTime, etc.) |

Supported VT types: VT_LPWSTR (31), VT_BOOL (11), VT_UI4 (19), VT_I4 (3), VT_CLSID (72), VT_FILETIME (64), VT_I2 (2), VT_UI2 (18), VT_I8 (20), VT_UI8 (21), VT_BLOB (65), VT_LPSTR (30). Unknown types return the raw bytes as `byte[]`.

### WinXHasher

Computes the hash required for Windows 10+ Power User Menu (Win+X) shortcuts:

```csharp
// Compute hash only
uint hash = WinXHasher.ComputeHash(@"C:\Windows\System32\cmd.exe");

// With arguments
uint hash = WinXHasher.ComputeHash(@"C:\Windows\System32\cmd.exe", "/k echo test");

// Build a complete property store with the hash embedded
byte[] propStore = WinXHasher.BuildPropertyStore(@"C:\Windows\System32\cmd.exe");
byte[] lnk = Shortcut.Create(new ShortcutOptions
{
    Target = @"C:\Windows\System32\cmd.exe",
    PropertyStoreData = propStore
});

// Or use PropertyStoreBuilder for more control
var builder = new PropertyStoreBuilder
{
    WinXHash = WinXHasher.ComputeHash(@"C:\test.exe"),
    AppUserModelId = "MyApp"
};
```

### DarwinDescriptor

Decodes MSI (Windows Installer) Darwin descriptor strings from DarwinDataBlock:

```csharp
var darwin = DarwinDescriptor.TryDecode(options.DarwinData);
if (darwin != null)
{
    Console.WriteLine($"Product: {darwin.ProductCode}");
    Console.WriteLine($"Feature: {darwin.FeatureId}");
    Console.WriteLine($"Component: {darwin.ComponentCode}");
}

// Encode a GUID to MSI compressed format
string packed = DarwinDescriptor.EncodeCompressedGuid(someGuid);
```

| Property | Type | Description |
|---|---|---|
| `ProductCode` | `Guid` | MSI product code |
| `FeatureId` | `string` | Feature identifier |
| `ComponentCode` | `Guid` | MSI component code |

### TrackerData Forensic Methods

Version 1 UUIDs in TrackerData contain embedded MAC addresses and timestamps. These methods extract that forensic information:

```csharp
var tracker = options.Tracker;
if (tracker != null && tracker.IsObjectIdVersion1())
{
    byte[] mac = tracker.ExtractMacAddress();       // [0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF]
    string macStr = tracker.ExtractMacAddressString(); // "AA:BB:CC:DD:EE:FF"
    DateTime? ts = tracker.ExtractTimestamp();       // UTC timestamp from UUID

    // Birth IDs (original machine that created the file)
    string birthMac = tracker.ExtractBirthMacAddressString();
    DateTime? birthTs = tracker.ExtractBirthTimestamp();
}
```

| Method | Returns | Description |
|---|---|---|
| `IsObjectIdVersion1()` | `bool` | True if ObjectId is a Version 1 UUID |
| `IsBirthObjectIdVersion1()` | `bool` | True if BirthObjectId is a Version 1 UUID |
| `ExtractMacAddress()` | `byte[]?` | 6-byte MAC from ObjectId (null if not V1) |
| `ExtractBirthMacAddress()` | `byte[]?` | 6-byte MAC from BirthObjectId |
| `ExtractMacAddressString()` | `string?` | MAC as "AA:BB:CC:DD:EE:FF" |
| `ExtractBirthMacAddressString()` | `string?` | Birth MAC as "AA:BB:CC:DD:EE:FF" |
| `ExtractTimestamp()` | `DateTime?` | UTC timestamp from ObjectId |
| `ExtractBirthTimestamp()` | `DateTime?` | UTC timestamp from BirthObjectId |

### RawExtraDataBlock

Unrecognized extra data blocks are preserved during round-trips:

| Property | Type | Description |
|---|---|---|
| `Signature` | `uint` | Block signature identifier |
| `Data` | `byte[]` | Raw block payload |

### DriveTypes

Drive type constants for `LocalPathInfo.DriveType`:

`Unknown` (0), `NoRootDir` (1), `Removable` (2), `Fixed` (3), `Remote` (4), `CDRom` (5), `RamDisk` (6)

### CsidlFolderIds

CSIDL folder ID constants for `SpecialFolderData.FolderId` (45 values):

`Desktop`, `Internet`, `Programs`, `Controls`, `Printers`, `Personal`, `Favorites`, `Startup`, `Recent`, `SendTo`, `RecycleBin`, `StartMenu`, `MyMusic`, `MyVideo`, `DesktopDirectory`, `Drives`, `Network`, `NetHood`, `Fonts`, `Templates`, `CommonStartMenu`, `CommonPrograms`, `CommonStartup`, `CommonDesktopDirectory`, `AppData`, `PrintHood`, `LocalAppData`, `CommonAppData`, `Windows`, `System`, `ProgramFiles`, `MyPictures`, `Profile`, `SystemX86`, `ProgramFilesX86`, `CommonFiles`, `CommonFilesX86`, `CommonTemplates`, `CommonDocuments`, `AdminTools`, `CommonAdminTools`, `Cookies`, `History`, `InternetCache`

### VirtualKeys

Virtual key code constants for `ShortcutOptions.HotkeyKey`:

**Letters:** `A`–`Z` (0x41–0x5A) | **Digits:** `D0`–`D9` (0x30–0x39) | **Function keys:** `F1`–`F24` (0x70–0x87) | **Toggle:** `NumLock`, `ScrollLock`

### ConsoleFillAttributes

Console color flag constants for `ConsoleData.FillAttributes` and `PopupFillAttributes`:

`ForegroundBlue`, `ForegroundGreen`, `ForegroundRed`, `ForegroundIntensity`, `BackgroundBlue`, `BackgroundGreen`, `BackgroundRed`, `BackgroundIntensity`

### ConsoleFontFamilies

Font family and pitch constants for `ConsoleData.FontFamily`:

**Families:** `DontCare`, `Roman`, `Swiss`, `Modern`, `Script`, `Decorative` | **Pitch:** `FixedPitch`, `Vector`, `TrueType`, `Device`

### ShimLayerNames

Common compatibility shim layer names for `ShortcutOptions.ShimLayerName`:

**OS compatibility:** `Win95`, `Win98`, `WinNT4SP5`, `Win2000`, `Win2000SP3`, `WinXPSP1`, `WinXPSP2`, `WinXPSP3`, `WinVistaSP1`, `WinVistaSP2`, `Win7RTM`, `Win8RTM`, `Win81RTM`, `Win10RTM`

**Display:** `Color256`, `Resolution640x480`, `ReducedColorMode`, `DisableDWM`, `GdiScalingOff`, `GdiDpiScaling`, `HighDpiAware`, `DpiUnaware`, `PerProcessSystemDpiForceOn`, `PerMonitorV2`

**Behavior:** `DisableNXShowUI`, `DisableThemes`, `RunAsAdmin`, `ForceDirectDrawEmulation`, `ElevateCreateProcess`, `DisableUserCallbackException`

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

## Responsible Use

See [RESPONSIBLE_USE.md](RESPONSIBLE_USE.md) for usage guidelines related to security research and forensic analysis.

## License

[MIT](LICENSE)
