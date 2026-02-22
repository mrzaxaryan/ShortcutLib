# LNKLib

A zero-dependency .NET library for creating Windows Shell Link (.lnk) shortcut files in memory.

## Features

- Create shortcuts to local files, folders, network shares, and printers
- Environment variable support (e.g. `%windir%\notepad.exe`)
- Custom icon, arguments, working directory, and description
- Window style control (normal, maximized, minimized)
- Run as Administrator support
- Hotkey assignment (key + modifier combinations)
- Argument padding to hide command-line arguments in shortcut properties
- Returns raw `byte[]` — no COM interop or Windows Shell dependency
- Targets .NET 10

## Installation

Add a reference to the `LNKLib` project or install the NuGet package:

```
dotnet add package LNKLib
```

## Usage

```csharp
using LNKLib;

// Simple file shortcut
byte[] lnk = Shortcut.Create(@"C:\Windows\notepad.exe");
File.WriteAllBytes("Notepad.lnk", lnk);

// Shortcut with all options
byte[] lnk = Shortcut.Create(
    target: @"C:\Windows\notepad.exe",
    arguments: @"C:\notes.txt",
    iconLocation: @"C:\Windows\notepad.exe",
    iconIndex: 0,
    description: "My Notepad Shortcut",
    workingDirectory: @"C:\Windows",
    windowStyle: ShortcutWindowStyle.Normal);
File.WriteAllBytes("Notepad.lnk", lnk);

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

## API

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

### Argument Padding

When `padArguments` is `true`, the arguments string is placed at the end of a 31 KB buffer filled with whitespace and control characters (CR, Tab, LF, file/group/record/unit separators, Space). This pushes the actual arguments beyond what the Windows shortcut properties dialog displays, effectively hiding them from casual inspection.

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
