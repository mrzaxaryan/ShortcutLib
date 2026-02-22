using System.Text;

namespace LNKLib;

public static class ShortcutCreator
{
    private const int MaxPath = 260;
    private const uint EnvVarBlockSignature = 0xA0000001;

    /// <summary>
    /// Converts a hexadecimal character to its corresponding byte value.
    /// </summary>
    private static byte ParseHexDigit(char c) =>
        (byte)(char.IsDigit(c) ? c - '0' : char.ToUpper(c) - 'A' + 10);

    /// <summary>
    /// Converts two hexadecimal characters into a single byte.
    /// </summary>
    private static byte ParseHexByte(char hi, char lo) =>
        (byte)(ParseHexDigit(hi) * 16 + ParseHexDigit(lo));

    /// <summary>
    /// Converts a CLSID string (with dashes) into a 16-byte array.
    /// </summary>
    private static void ParseClsid(string clsid, byte[] bytes)
    {
        string hex = clsid.Replace("-", "");
        if (hex.Length != 32)
            throw new ArgumentException("Invalid CLSID format.", nameof(clsid));
        bytes[0] = ParseHexByte(hex[6], hex[7]);
        bytes[1] = ParseHexByte(hex[4], hex[5]);
        bytes[2] = ParseHexByte(hex[2], hex[3]);
        bytes[3] = ParseHexByte(hex[0], hex[1]);
        bytes[4] = ParseHexByte(hex[10], hex[11]);
        bytes[5] = ParseHexByte(hex[8], hex[9]);
        bytes[6] = ParseHexByte(hex[14], hex[15]);
        bytes[7] = ParseHexByte(hex[12], hex[13]);
        bytes[8] = ParseHexByte(hex[16], hex[17]);
        bytes[9] = ParseHexByte(hex[18], hex[19]);
        bytes[10] = ParseHexByte(hex[20], hex[21]);
        bytes[11] = ParseHexByte(hex[22], hex[23]);
        bytes[12] = ParseHexByte(hex[24], hex[25]);
        bytes[13] = ParseHexByte(hex[26], hex[27]);
        bytes[14] = ParseHexByte(hex[28], hex[29]);
        bytes[15] = ParseHexByte(hex[30], hex[31]);
    }

    /// <summary>
    /// Writes an optional string to the BinaryWriter (2-byte length then string bytes).
    /// Per the .lnk spec, StringData uses ANSI encoding when IS_UNICODE is not set.
    /// </summary>
    private static void WriteStringData(BinaryWriter writer, string? value)
    {
        if (value is null)
            return;
        int length = value.Length;
        writer.Write((byte)(length % 256));
        writer.Write((byte)(length / 256));
        writer.Write(Encoding.Default.GetBytes(value));
    }

    /// <summary>
    /// Writes a padded arguments string. The actual arguments are placed at the end of a
    /// 31 KB buffer filled with whitespace characters, making them less visible in the
    /// shortcut properties UI.
    /// </summary>
    private static void WritePaddedArguments(BinaryWriter writer, string arguments)
    {
        int totalLength = 31 * 1024;
        char[] buffer = new char[totalLength];
        int fillLength = totalLength - arguments.Length;

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

        for (int i = 0; i < fillLength; i++)
        {
            buffer[i] = fillChars[i % fillChars.Length];
        }

        arguments.CopyTo(0, buffer, fillLength, arguments.Length);
        writer.Write((byte)(totalLength % 256));
        writer.Write((byte)(totalLength / 256));
        writer.Write(Encoding.Default.GetBytes(new string(buffer)));
    }

    /// <summary>
    /// Writes the Environment Variable Data Block.
    /// The block layout is:
    ///   DWORD   cbSize       - block size (788 bytes)
    ///   DWORD   dwSignature  - signature (0xA0000001)
    ///   CHAR    szTarget[MAX_PATH]   - ANSI string (260 bytes)
    ///   WCHAR   swzTarget[MAX_PATH]  - Unicode string (520 bytes)
    /// </summary>
    private static void WriteEnvVarDataBlock(BinaryWriter writer, string target)
    {
        int blockSize = 4 + 4 + MaxPath + (MaxPath * 2); // 788 bytes
        writer.Write(blockSize);
        writer.Write(EnvVarBlockSignature);

        // Prepare ANSI buffer (260 bytes): zero-filled, copy target, ensure null termination.
        byte[] ansiBuffer = new byte[MaxPath];
        Array.Clear(ansiBuffer, 0, MaxPath);
        byte[] targetAnsi = Encoding.Default.GetBytes(target);
        int copyLen = Math.Min(targetAnsi.Length, MaxPath - 1);
        Array.Copy(targetAnsi, 0, ansiBuffer, 0, copyLen);
        ansiBuffer[copyLen] = 0;
        writer.Write(ansiBuffer);

        // Prepare Unicode buffer (520 bytes = 260 WCHARs): zero-filled and copy target.
        char[] unicodeBuffer = new char[MaxPath];
        for (int i = 0; i < MaxPath; i++)
            unicodeBuffer[i] = '\0';
        copyLen = Math.Min(target.Length, MaxPath - 1);
        target.CopyTo(0, unicodeBuffer, 0, copyLen);
        // Buffer remains null-terminated.
        byte[] unicodeBytes = Encoding.Unicode.GetBytes(unicodeBuffer);
        // Ensure exactly 520 bytes are written.
        if (unicodeBytes.Length < MaxPath * 2)
        {
            byte[] temp = new byte[MaxPath * 2];
            Array.Copy(unicodeBytes, temp, unicodeBytes.Length);
            writer.Write(temp);
        }
        else
        {
            writer.Write(unicodeBytes, 0, MaxPath * 2);
        }
    }

    /// <summary>
    /// Creates a Windows Shortcut (.lnk) file in memory and returns its binary content as a byte array.
    /// If the target contains environment variables (e.g. "%windir%"), the extra data block is appended
    /// after all mandatory and optional fields and then terminated with a 4-byte zero.
    /// </summary>
    public static byte[] CreateShortcut(
        string target,
        string? arguments = null,
        bool padArguments = false,
        string? iconLocation = null,
        int iconIndex = 0,
        string? name = null,
        string? workingDirectory = null,
        bool isPrinterLink = false)
    {
        // --- Header and LinkCLSID ---
        byte[] headerSize = { 0x4C, 0x00, 0x00, 0x00 };
        byte[] linkClsid = new byte[16];
        ParseClsid("00021401-0000-0000-c000-000000000046", linkClsid);

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
        byte[] terminalId = new byte[2];

        // --- CLSIDs for Computer and Network ---
        byte[] computerClsid = new byte[16];
        byte[] networkClsid = new byte[16];
        ParseClsid("20d04fe0-3aea-1069-a2d8-08002b30309d", computerClsid);
        ParseClsid("208d2c60-3aea-1069-a2d7-08002b30309d", networkClsid);

        // --- Prefix constants ---
        byte[] prefixLocalRoot = { 0x2F };
        byte[] prefixFolder = { 0x31, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        byte[] prefixFile = { 0x32, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        byte[] prefixNetworkRoot = { 0xC3, 0x01, 0x81 };
        byte[] prefixNetworkPrinter = { 0xC3, 0x02, 0xC1 };
        byte[] nullTerminator = { 0x00 };

        // --- Determine link type and select prefix ---
        bool isNetworkLink = target.StartsWith(@"\\");
        bool isRootLink = false;
        int extensionLength = 0;
        byte[] rootPrefix = isNetworkLink
            ? (isPrinterLink ? prefixNetworkPrinter : prefixNetworkRoot)
            : prefixLocalRoot;
        if (isNetworkLink && isPrinterLink)
            isRootLink = true;

        // --- Prepare Shell Item ID data ---
        byte[] rootShellItem = new byte[18];
        rootShellItem[0] = 0x1F;
        rootShellItem[1] = isNetworkLink ? (byte)0x58 : (byte)0x50;
        byte[] clsidToUse = isNetworkLink ? networkClsid : computerClsid;
        Array.Copy(clsidToUse, 0, rootShellItem, 2, 16);

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
        if (targetLeaf is null or { Length: 0 })
            isRootLink = true;

        // --- Determine target prefix based on file extension ---
        byte[] targetPrefix;
        byte[] fileAttributes;
        if (!string.IsNullOrEmpty(targetLeaf))
        {
            int dotIndex = targetLeaf.LastIndexOf('.');
            if (dotIndex != -1 && dotIndex + 1 < targetLeaf.Length)
                extensionLength = targetLeaf.Substring(dotIndex + 1).Length;
        }
        if (extensionLength >= 1 && extensionLength <= 3)
        {
            targetPrefix = prefixFile;
            fileAttributes = fileAttrFile;
        }
        else
        {
            targetPrefix = prefixFolder;
            fileAttributes = fileAttrDirectory;
        }

        // --- Prepare padded target root (targetRoot + 21 null characters) ---
        string paddedTargetRoot = targetRoot + new string('\0', 21);

        // --- Determine environment variable flags ---
        int flagEnv = target.Contains("%") ? FLAG_HAS_EXP_SZ | FLAG_PREFER_ENVIRONMENT_PATH : 0;

        // --- Combine all flags ---
        int linkFlags = FLAG_HAS_LINK_TARGET_ID_LIST
            | (name != null ? FLAG_HAS_NAME : 0)
            | (workingDirectory != null ? FLAG_HAS_WORKING_DIR : 0)
            | (arguments != null ? FLAG_HAS_ARGUMENTS : 0)
            | (iconLocation != null ? FLAG_HAS_ICON_LOCATION : 0)
            | flagEnv;

        // --- Write out the binary file ---
        using (MemoryStream ms = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(ms))
        {
            // Write header and CLSID.
            writer.Write(headerSize);
            writer.Write(linkClsid);

            // Write LinkFlags (4 bytes).
            writer.Write((uint)linkFlags);
            writer.Write(fileAttributes);
            writer.Write(creationTime);
            writer.Write(accessTime);
            writer.Write(writeTime);
            writer.Write(fileSize);
            // Write icon index (4 bytes).
            writer.Write(BitConverter.GetBytes(iconIndex));
            writer.Write(showCommand);
            writer.Write(hotkey);
            writer.Write(reserved);
            writer.Write(reserved2);
            writer.Write(reserved3);

            // Write the IDList structure.
            if (isRootLink)
            {
                int rootShellItemSize = rootShellItem.Length;
                int rootItemSize = rootPrefix.Length + Encoding.Default.GetByteCount(paddedTargetRoot) + nullTerminator.Length;
                int idListSize = rootShellItemSize + 2 + rootItemSize + 2;
                int totalIdListSize = idListSize + 2;
                writer.Write((byte)(totalIdListSize % 256));
                writer.Write((byte)(totalIdListSize / 256));

                writer.Write((byte)((rootShellItemSize + 2) % 256));
                writer.Write((byte)((rootShellItemSize + 2) / 256));
                writer.Write(rootShellItem);

                writer.Write((byte)((rootItemSize + 2) % 256));
                writer.Write((byte)((rootItemSize + 2) / 256));
                writer.Write(rootPrefix);
                writer.Write(Encoding.Default.GetBytes(paddedTargetRoot));
                writer.Write(nullTerminator);
            }
            else
            {
                int rootShellItemSize = rootShellItem.Length;
                int rootItemSize = rootPrefix.Length + Encoding.Default.GetByteCount(paddedTargetRoot) + nullTerminator.Length;
                int targetItemSize = targetPrefix.Length + (targetLeaf != null ? Encoding.Default.GetByteCount(targetLeaf) : 0) + nullTerminator.Length;
                int idListSize = rootShellItemSize + 2 + rootItemSize + 2 + targetItemSize + 2;
                int totalIdListSize = idListSize + 2;
                writer.Write((byte)(totalIdListSize % 256));
                writer.Write((byte)(totalIdListSize / 256));

                writer.Write((byte)((rootShellItemSize + 2) % 256));
                writer.Write((byte)((rootShellItemSize + 2) / 256));
                writer.Write(rootShellItem);

                writer.Write((byte)((rootItemSize + 2) % 256));
                writer.Write((byte)((rootItemSize + 2) / 256));
                writer.Write(rootPrefix);
                writer.Write(Encoding.Default.GetBytes(paddedTargetRoot));
                writer.Write(nullTerminator);

                writer.Write((byte)((targetItemSize + 2) % 256));
                writer.Write((byte)((targetItemSize + 2) / 256));
                writer.Write(targetPrefix);
                if (targetLeaf != null)
                    writer.Write(Encoding.Default.GetBytes(targetLeaf));
                writer.Write(nullTerminator);
            }

            // Write TerminalID.
            writer.Write(terminalId);

            // Write optional strings.
            WriteStringData(writer, name);
            WriteStringData(writer, workingDirectory);
            if (padArguments && arguments != null)
                WritePaddedArguments(writer, arguments);
            else
                WriteStringData(writer, arguments);
            WriteStringData(writer, iconLocation);

            // Now write the extra data block for environment variables (if needed)
            // This block MUST come last in the extra data chain.
            if (target.Contains("%"))
            {
                WriteEnvVarDataBlock(writer, target);
            }

            // Terminate extra data chain with a 4-byte zero.
            writer.Write((uint)0);

            writer.Flush();
            return ms.ToArray();
        }
    }
}
