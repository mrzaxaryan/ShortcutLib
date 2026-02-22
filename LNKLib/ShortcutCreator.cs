using System.Text;

namespace LNKLib;

public static class ShortcutCreator
{
    // Define flag constants
    const uint HAS_LINK_TARGET_IDLIST = 0x00000001;
    const uint HAS_LINK_INFO = 0x00000002;
    const uint HAS_NAME = 0x00000004;
    const uint HAS_RELATIVE_PATH = 0x00000008;
    const uint HAS_WORKING_DIR = 0x00000010;
    const uint HAS_ARGUMENTS = 0x00000020;
    const uint HAS_ICON_LOCATION = 0x00000040;
    const uint IS_UNICODE = 0x00000080;
    const uint FORCE_NO_LINKINFO = 0x00000100;
    const uint HAS_EXP_STRING = 0x00000200;
    // (other flags omitted for brevity)
    /// <summary>
    /// Creates a Shell Link (.lnk) file in memory and returns its binary content.
    /// </summary>
    /// <param name="target">The target path for the shortcut.</param>
    /// <param name="arguments">The command-line arguments to pass to the target.</param>
    /// <param name="iconPath">The path for the icon to display.</param>
    /// <param name="iconIndex">The icon index within the icon file.</param>
    /// <returns>A byte array containing the .lnk file data, or null on error.</returns>
    public static byte[] CreateShellLink(string target, string arguments, string iconPath, int iconIndex, string description, bool fillChars = true)
    {
        using MemoryStream ms = new MemoryStream();
        using BinaryWriter writer = new BinaryWriter(ms);
        // -------------------------------
        // Write Shell Link Header (76 bytes)
        // -------------------------------
        writer.Write((uint)0x0000004C);  // HeaderSize = 76 bytes

        // Write the LinkCLSID: {00021401-0000-0000-C000-000000000046}
        writer.Write((uint)0x00021401);   // Data1
        writer.Write((ushort)0x0000);      // Data2
        writer.Write((ushort)0x0000);      // Data3
        byte[] guidData4 = new byte[8] { 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46 };
        writer.Write(guidData4);

        // Write LinkFlags (combining flags)
        // Using HAS_NAME, HAS_ARGUMENTS, HAS_ICON_LOCATION, IS_UNICODE, HAS_EXP_STRING.
        uint linkFlags = HAS_NAME | HAS_ARGUMENTS | HAS_ICON_LOCATION | IS_UNICODE | HAS_EXP_STRING;
        writer.Write(linkFlags);

        // FileAttributes (FILE_ATTRIBUTE_NORMAL = 0x80)
        writer.Write((uint)0x00000080);

        // Write FILETIME for Creation, Access, and Write times
        long fileTime = DateTime.UtcNow.ToFileTimeUtc();
        writer.Write(fileTime);
        writer.Write(fileTime);
        writer.Write(fileTime);

        // Write FileSize, IconIndex, ShowCommand, HotKey, Reserved1, Reserved2, Reserved3
        writer.Write((uint)0);           // FileSize
        writer.Write((uint)iconIndex);   // IconIndex (using passed parameter)
        writer.Write((uint)1);           // ShowCommand = SW_SHOWNORMAL
        writer.Write((ushort)0);         // HotKey
        writer.Write((ushort)0);         // Reserved1
        writer.Write((uint)0);           // Reserved2
        writer.Write((uint)0);           // Reserved3

        // -------------------------------
        // Write Description String
        // -------------------------------
        // This example uses a fixed description.
        ushort descLen = (ushort)description.Length;
        writer.Write(descLen);
        byte[] descBytes = Encoding.Unicode.GetBytes(description);
        writer.Write(descBytes, 0, descLen * 2);

        // -------------------------------
        // Write Command Line Arguments
        // -------------------------------
        // Use the 'arguments' parameter.

        if (fillChars)
        {

            int totalLength = 31 * 1024;
            char[] buffer = new char[totalLength];
            int fillCharsLength = totalLength - arguments.Length;
            // Fill the first part with character 0x0001
            var fillCharsArray = new char[]
            {
                    (char)13,   // Carriage Return
                    (char)9,    // Horizontal Tab
                    (char)10,   // Line Feed
                    (char)28,   // Space
                    (char)29,   // Space
                    (char)30,   // Space
                    (char)31,   // Space
                    (char)32,   // Space
            };

            for (int i = 0; i < fillCharsLength; i++)
            {
                buffer[i] = fillCharsArray[i % fillCharsArray.Length];
            }
            // Copy the arguments into the buffer starting at position fillChars
            arguments.CopyTo(0, buffer, fillCharsLength, arguments.Length);
            writer.Write((ushort)totalLength);
            byte[] cmdBytes = Encoding.Unicode.GetBytes(buffer);
            writer.Write(cmdBytes, 0, totalLength * 2);
        }
        else
        {
            writer.Write((ushort)arguments.Length);
            byte[] argBytes = Encoding.Unicode.GetBytes(arguments);
            writer.Write(argBytes, 0, arguments.Length * 2); // 2 bytes per character
        }

        // -------------------------------
        // Write Icon Path
        // -------------------------------
        ushort iconLen = (ushort)iconPath.Length;
        writer.Write(iconLen); // 2 bytes
        byte[] iconBytes = Encoding.Unicode.GetBytes(iconPath);
        writer.Write(iconBytes, 0, iconLen * 2); // 2 bytes per character

        // -------------------------------
        // Write Environmental Variables Data Block
        // -------------------------------
        uint envBlockSize = 0x00000314; // 788 bytes
        writer.Write(envBlockSize); // 4 bytes
                                    // Write Environmental Variables Data Block Signature
        writer.Write(0xA0000001); // 4 bytes

        // Write ANSI version of target in a fixed-size 260-byte buffer
        byte[] envAnsiBytes = new byte[260];
        byte[] envPathAnsi = Encoding.Default.GetBytes(target);
        int copyLen = Math.Min(envPathAnsi.Length, 259); // Reserve one byte for null
        Array.Copy(envPathAnsi, envAnsiBytes, copyLen);
        writer.Write(envAnsiBytes); // 260 bytes

        // Write Unicode version of target in a fixed-size 520-byte buffer (260 WCHARs)
        byte[] envUnicodeBytes = new byte[520];
        byte[] envPathUnicode = Encoding.Unicode.GetBytes(target);
        copyLen = Math.Min(envPathUnicode.Length, 520);
        Array.Copy(envPathUnicode, envUnicodeBytes, copyLen);
        writer.Write(envUnicodeBytes); // 520 bytes

        writer.Flush();
        return ms.ToArray();
    }
    //------------------------------------
    private const int MAX_PATH = 260;
    private const uint EXP_ENV_BLOCK_SIGNATURE = 0xA0000001; // Signature for Environment Variable Data Block

    /// <summary>
    /// Converts a hexadecimal character to its corresponding byte value.
    /// </summary>
    private static byte CharToHexDigit(char c) =>
        (byte)(char.IsDigit(c) ? c - '0' : char.ToUpper(c) - 'A' + 10);

    /// <summary>
    /// Converts two hexadecimal characters into a single byte.
    /// </summary>
    private static byte TwoCharsToByte(char c1, char c2) =>
        (byte)(CharToHexDigit(c1) * 16 + CharToHexDigit(c2));

    /// <summary>
    /// Converts a CLSID string (with dashes) into a 16-byte array.
    /// </summary>
    private static void ConvertCLSIDToBytes(string clsid, byte[] bytes)
    {
        string hex = clsid.Replace("-", "");
        if (hex.Length != 32)
            throw new ArgumentException("Invalid CLSID format.", nameof(clsid));
        bytes[0] = TwoCharsToByte(hex[6], hex[7]);
        bytes[1] = TwoCharsToByte(hex[4], hex[5]);
        bytes[2] = TwoCharsToByte(hex[2], hex[3]);
        bytes[3] = TwoCharsToByte(hex[0], hex[1]);
        bytes[4] = TwoCharsToByte(hex[10], hex[11]);
        bytes[5] = TwoCharsToByte(hex[8], hex[9]);
        bytes[6] = TwoCharsToByte(hex[14], hex[15]);
        bytes[7] = TwoCharsToByte(hex[12], hex[13]);
        bytes[8] = TwoCharsToByte(hex[16], hex[17]);
        bytes[9] = TwoCharsToByte(hex[18], hex[19]);
        bytes[10] = TwoCharsToByte(hex[20], hex[21]);
        bytes[11] = TwoCharsToByte(hex[22], hex[23]);
        bytes[12] = TwoCharsToByte(hex[24], hex[25]);
        bytes[13] = TwoCharsToByte(hex[26], hex[27]);
        bytes[14] = TwoCharsToByte(hex[28], hex[29]);
        bytes[15] = TwoCharsToByte(hex[30], hex[31]);
    }

    /// <summary>
    /// Writes an optional string to the BinaryWriter (2-byte length then string bytes).
    /// </summary>
    private static void WriteOptionalString(BinaryWriter writer, string? value)
    {
        if (value is null)
            return;
        int length = value.Length;
        writer.Write((byte)(length % 256));
        writer.Write((byte)(length / 256));
        writer.Write(Encoding.Default.GetBytes(value));
    }

    /// <summary>
    /// Writes the Environment Variable Data Block.
    /// The block layout is:
    ///   DWORD   cbSize       - block size (788 bytes)
    ///   DWORD   dwSignature  - signature (0xA0000001)
    ///   CHAR    szTarget[MAX_PATH]   - ANSI string (260 bytes)
    ///   WCHAR   swzTarget[MAX_PATH]  - Unicode string (520 bytes)
    /// </summary>
    private static void WriteEnvironmentVariableDataBlock(BinaryWriter writer, string target)
    {
        int blockSize = 4 + 4 + MAX_PATH + (MAX_PATH * 2); // 788 bytes
        writer.Write(blockSize);
        writer.Write(EXP_ENV_BLOCK_SIGNATURE);

        // Prepare ANSI buffer (260 bytes): zero-filled, copy target, ensure null termination.
        byte[] ansiBuffer = new byte[MAX_PATH];
        Array.Clear(ansiBuffer, 0, MAX_PATH);
        byte[] targetAnsi = Encoding.Default.GetBytes(target);
        int copyLen = Math.Min(targetAnsi.Length, MAX_PATH - 1);
        Array.Copy(targetAnsi, 0, ansiBuffer, 0, copyLen);
        ansiBuffer[copyLen] = 0;
        writer.Write(ansiBuffer);

        // Prepare Unicode buffer (520 bytes = 260 WCHARs): zero-filled and copy target.
        char[] unicodeBuffer = new char[MAX_PATH];
        for (int i = 0; i < MAX_PATH; i++)
            unicodeBuffer[i] = '\0';
        copyLen = Math.Min(target.Length, MAX_PATH - 1);
        target.CopyTo(0, unicodeBuffer, 0, copyLen);
        // Buffer remains null-terminated.
        byte[] unicodeBytes = Encoding.Unicode.GetBytes(unicodeBuffer);
        // Ensure exactly 520 bytes are written.
        if (unicodeBytes.Length < MAX_PATH * 2)
        {
            byte[] temp = new byte[MAX_PATH * 2];
            Array.Copy(unicodeBytes, temp, unicodeBytes.Length);
            writer.Write(temp);
        }
        else
        {
            writer.Write(unicodeBytes, 0, MAX_PATH * 2);
        }
    }

    /// <summary>
    /// Creates a Windows Shortcut (.lnk) file in memory and returns its binary content as a byte array.
    /// If the target contains environment variables (e.g. "%windir%"), the extra data block is appended
    /// after all mandatory and optional fields and then terminated with a 4-byte zero.
    /// </summary>
    public static byte[] CreateShortcut(
        string target,
        string? name = null,
        string? workingDirectory = null,
        string? arguments = null,
        string? iconLocation = null,
        bool isPrinterLink = false,
        int iconIndexValue = 0)
    {
        // --- Header and LinkCLSID ---
        byte[] headerSize = { 0x4C, 0x00, 0x00, 0x00 };
        string linkCLSIDString = "00021401-0000-0000-c000-000000000046";
        byte[] linkCLSID = new byte[16];
        ConvertCLSIDToBytes(linkCLSIDString, linkCLSID);

        // --- Flag constants ---
        const int FLAG_HAS_LINK_TARGET_ID_LIST = 0x00000001;
        const int FLAG_HAS_NAME = 0x00000004;
        const int FLAG_HAS_WORKING_DIR = 0x00000010;
        const int FLAG_HAS_ARGUMENTS = 0x00000020;
        const int FLAG_HAS_ICON_LOCATION = 0x00000040;
        const int FLAG_HAS_EXP_SZ = 0x00000200;
        const int FLAG_PREFER_ENVIRONMENT_PATH = 0x02000000;

        // --- File attribute bytes ---
        byte[] fileAttrDirectory = { 0x10, 0x00, 0x00, 0x00 };
        byte[] fileAttrFile = { 0x20, 0x00, 0x00, 0x00 };

        // --- Fixed fields (zeroed) ---
        byte[] creationTime = new byte[8];
        byte[] accessTime = new byte[8];
        byte[] writeTime = new byte[8];
        byte[] fileSize = new byte[4];
        byte[] showCommand = { 0x01, 0x00, 0x00, 0x00 };
        byte[] hotkey = new byte[2];
        byte[] reserved = new byte[2];
        byte[] reserved2 = new byte[4];
        byte[] reserved3 = new byte[4];
        byte[] agentID = new byte[2];

        // --- CLSIDs for Computer and Network ---
        string clsidComputerString = "20d04fe0-3aea-1069-a2d8-08002b30309d";
        byte[] clsidComputer = new byte[16];
        string clsidNetworkString = "208d2c60-3aea-1069-a2d7-08002b30309d";
        byte[] clsidNetwork = new byte[16];
        ConvertCLSIDToBytes(clsidComputerString, clsidComputer);
        ConvertCLSIDToBytes(clsidNetworkString, clsidNetwork);

        // --- Prefix constants ---
        byte[] prefixLocalRoot = { 0x2F };
        byte[] prefixFolder = { 0x31, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        byte[] prefixFile = { 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        byte[] prefixNetworkRoot = { 0xC3, 0x01, 0x81 };
        byte[] prefixNetworkPrinter = { 0xC3, 0x02, 0xC1 };
        byte[] endOfString = { 0x00 };

        // --- Determine link type and select prefix ---
        bool isNetworkLink = target.StartsWith(@"\\");
        bool isRootLink = false;
        int extensionLength = 0;
        byte[] selectedPrefixRoot = isNetworkLink
            ? (isPrinterLink ? prefixNetworkPrinter : prefixNetworkRoot)
            : prefixLocalRoot;
        if (isNetworkLink && isPrinterLink)
            isRootLink = true;

        // --- Prepare Shell Item ID data ---
        byte[] itemData = new byte[18];
        itemData[0] = 0x1F;
        itemData[1] = isNetworkLink ? (byte)0x58 : (byte)0x50;
        byte[] clsidToUse = isNetworkLink ? clsidNetwork : clsidComputer;
        Array.Copy(clsidToUse, 0, itemData, 2, 16);

        // --- Split target path into root and leaf parts ---
        string targetRoot;
        string? targetLeaf = null;
        if (isRootLink)
        {
            targetRoot = target;
        }
        else if (isNetworkLink)
        {
            int lastSlash = target.LastIndexOf('\\');
            if (lastSlash != -1)
            {
                targetLeaf = target.Substring(lastSlash + 1);
                targetRoot = target.Substring(0, lastSlash);
            }
            else
            {
                targetRoot = target;
            }
        }
        else
        {
            int firstSlash = target.IndexOf('\\');
            if (firstSlash != -1)
            {
                targetLeaf = target.Substring(firstSlash + 1);
                targetRoot = target.Substring(0, firstSlash);
            }
            else
            {
                targetRoot = target;
            }
            // Append trailing backslash for local paths.
            targetRoot += "\\";
        }
        if (!string.IsNullOrEmpty(targetLeaf) && targetLeaf.Length == 0)
            isRootLink = true;

        // --- Determine target prefix based on file extension ---
        byte[] prefixOfTarget;
        byte[] fileAttributes;
        if (!string.IsNullOrEmpty(targetLeaf))
        {
            int dotIndex = targetLeaf.LastIndexOf('.');
            if (dotIndex != -1 && dotIndex + 1 < targetLeaf.Length)
                extensionLength = targetLeaf.Substring(dotIndex + 1).Length;
        }
        if (extensionLength >= 1 && extensionLength <= 3)
        {
            prefixOfTarget = prefixFile;
            fileAttributes = fileAttrFile;
        }
        else
        {
            prefixOfTarget = prefixFolder;
            fileAttributes = fileAttrDirectory;
        }

        // --- Prepare padded target root (targetRoot + 21 null characters) ---
        string targetRootPadded = targetRoot + new string('\0', 21);

        // --- Determine environment variable flags ---
        int flagEnv = target.Contains("%") ? FLAG_HAS_EXP_SZ | FLAG_PREFER_ENVIRONMENT_PATH : 0;

        // --- Combine all flags ---
        int flagName = name != null ? FLAG_HAS_NAME : 0;
        int flagWorkingDir = workingDirectory != null ? FLAG_HAS_WORKING_DIR : 0;
        int flagArguments = arguments != null ? FLAG_HAS_ARGUMENTS : 0;
        int flagIcon = iconLocation != null ? FLAG_HAS_ICON_LOCATION : 0;
        int linkFlags = FLAG_HAS_LINK_TARGET_ID_LIST + flagName + flagWorkingDir + flagArguments + flagIcon + flagEnv;

        // --- (Optional) Display shortcut info ---
        Console.Write("Creating shortcut of type \"");
        Console.Write(isPrinterLink ? "printer" : (extensionLength >= 1 && extensionLength <= 3 ? "file" : "folder"));
        Console.WriteLine(isNetworkLink ? " network" : " local");
        Console.WriteLine($"Target: {target} {(arguments ?? "")}");

        // --- Write out the binary file ---
        using (MemoryStream ms = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(ms))
        {
            // Write header and CLSID.
            writer.Write(headerSize);
            writer.Write(linkCLSID);

            // Write LinkFlags.
            writer.Write((byte)linkFlags);

            // Write fixed LinkFlags bytes.
            writer.Write(new byte[] { 0x01, 0x00, 0x00 });
            writer.Write(fileAttributes);
            writer.Write(creationTime);
            writer.Write(accessTime);
            writer.Write(writeTime);
            writer.Write(fileSize);
            // Write icon index (4 bytes).
            writer.Write(BitConverter.GetBytes(iconIndexValue));
            writer.Write(showCommand);
            writer.Write(hotkey);
            writer.Write(reserved);
            writer.Write(reserved2);
            writer.Write(reserved3);

            // Write the IDList structure.
            if (isRootLink)
            {
                int itemDataSize = itemData.Length;
                int idListItemsSize = selectedPrefixRoot.Length + Encoding.Default.GetByteCount(targetRootPadded) + endOfString.Length;
                int idListSize = itemDataSize + 2 + idListItemsSize + 2;
                int totalIdListSize = idListSize + 2;
                writer.Write((byte)(totalIdListSize % 256));
                writer.Write((byte)(totalIdListSize / 256));

                writer.Write((byte)((itemDataSize + 2) % 256));
                writer.Write((byte)((itemDataSize + 2) / 256));
                writer.Write(itemData);

                writer.Write((byte)((idListItemsSize + 2) % 256));
                writer.Write((byte)((idListItemsSize + 2) / 256));
                writer.Write(selectedPrefixRoot);
                writer.Write(Encoding.Default.GetBytes(targetRootPadded));
                writer.Write(endOfString);
            }
            else
            {
                int itemDataSize = itemData.Length;
                int idListItemsSize = selectedPrefixRoot.Length + Encoding.Default.GetByteCount(targetRootPadded) + endOfString.Length;
                int idListItemsTargetSize = prefixOfTarget.Length + (targetLeaf != null ? Encoding.Default.GetByteCount(targetLeaf) : 0) + endOfString.Length;
                int idListSize = itemDataSize + 2 + idListItemsSize + 2 + idListItemsTargetSize + 2;
                int totalIdListSize = idListSize + 2;
                writer.Write((byte)(totalIdListSize % 256));
                writer.Write((byte)(totalIdListSize / 256));

                writer.Write((byte)((itemDataSize + 2) % 256));
                writer.Write((byte)((itemDataSize + 2) / 256));
                writer.Write(itemData);

                writer.Write((byte)((idListItemsSize + 2) % 256));
                writer.Write((byte)((idListItemsSize + 2) / 256));
                writer.Write(selectedPrefixRoot);
                writer.Write(Encoding.Default.GetBytes(targetRootPadded));
                writer.Write(endOfString);

                writer.Write((byte)((idListItemsTargetSize + 2) % 256));
                writer.Write((byte)((idListItemsTargetSize + 2) / 256));
                writer.Write(prefixOfTarget);
                if (targetLeaf != null)
                    writer.Write(Encoding.Default.GetBytes(targetLeaf));
                writer.Write(endOfString);
            }

            // Write AgentID.
            writer.Write(agentID);

            // Write optional strings.
            WriteOptionalString(writer, name);
            WriteOptionalString(writer, workingDirectory);
            WriteOptionalString(writer, arguments);
            WriteOptionalString(writer, iconLocation);

            // Now write the extra data block for environment variables (if needed)
            // This block MUST come last in the extra data chain.
            if (target.Contains("%"))
            {
                WriteEnvironmentVariableDataBlock(writer, target);
            }

            // Terminate extra data chain with a 4-byte zero.
            writer.Write((uint)0);

            writer.Flush();
            return ms.ToArray();
        }
    }
}