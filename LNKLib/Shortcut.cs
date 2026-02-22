using System.Text;

namespace LNKLib;

public static class Shortcut
{
    private const int MaxPath = 260;
    private const uint EnvVarBlockSignature = 0xA0000001;
    private const uint IconEnvBlockSignature = 0xA0000007;
    private const uint TrackerBlockSignature = 0xA0000003;
    private const uint SpecialFolderBlockSignature = 0xA0000005;
    private const uint PropertyStoreBlockSignature = 0xA0000009;
    private const uint KnownFolderBlockSignature = 0xA000000B;

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
    /// Converts a nullable DateTime to an 8-byte FILETIME representation.
    /// Returns 8 zero bytes when null.
    /// </summary>
    private static byte[] ToFileTimeBytes(DateTime? dt)
    {
        if (dt is null) return new byte[8];
        long ft = dt.Value.ToFileTimeUtc();
        return BitConverter.GetBytes(ft);
    }

    /// <summary>
    /// Writes an optional string to the BinaryWriter (2-byte length then string bytes).
    /// When unicode is false, uses ANSI encoding per the .lnk spec (IS_UNICODE not set).
    /// When unicode is true, uses UTF-16LE encoding.
    /// </summary>
    private static void WriteStringData(BinaryWriter writer, string? value, bool unicode)
    {
        if (value is null)
            return;
        int length = value.Length;
        writer.Write((byte)(length % 256));
        writer.Write((byte)(length / 256));
        if (unicode)
            writer.Write(Encoding.Unicode.GetBytes(value));
        else
            writer.Write(Encoding.Default.GetBytes(value));
    }

    /// <summary>
    /// Writes a padded arguments string. The actual arguments are placed at the end of a
    /// 31 KB buffer filled with whitespace characters, making them less visible in the
    /// shortcut properties UI.
    /// </summary>
    private static void WritePaddedArguments(BinaryWriter writer, string arguments, bool unicode)
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
        if (unicode)
            writer.Write(Encoding.Unicode.GetBytes(new string(buffer)));
        else
            writer.Write(Encoding.Default.GetBytes(new string(buffer)));
    }

    /// <summary>
    /// Writes an environment-variable-style data block (used for both EnvironmentVariableDataBlock
    /// and IconEnvironmentDataBlock — they share the same layout, differing only in signature).
    /// </summary>
    private static void WriteEnvironmentDataBlock(BinaryWriter writer, string target, uint signature)
    {
        int blockSize = 4 + 4 + MaxPath + (MaxPath * 2); // 788 bytes
        writer.Write(blockSize);
        writer.Write(signature);

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
    /// Writes the LinkInfo structure per [MS-SHLLINK] 2.3.
    /// </summary>
    private static void WriteLinkInfo(BinaryWriter writer, LinkInfo info)
    {
        // Build LinkInfo into a temporary buffer to compute offsets.
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);

        bool hasLocal = info.Local is not null;
        bool hasNetwork = info.Network is not null;

        int flags = 0;
        if (hasLocal) flags |= 0x01; // VolumeIDAndLocalBasePath
        if (hasNetwork) flags |= 0x02; // CommonNetworkRelativeLinkAndPathSuffix

        // LinkInfoHeaderSize = 0x1C (28 bytes) for the basic header
        const int headerSize = 0x1C;

        // Pre-compute VolumeID structure if local
        byte[]? volumeIdBytes = null;
        byte[]? localBasePathBytes = null;
        if (hasLocal)
        {
            var local = info.Local!;
            byte[] volumeLabelAnsi = Encoding.Default.GetBytes(local.VolumeLabel + "\0");
            int volumeIdSize = 4 + 4 + 4 + 4 + volumeLabelAnsi.Length; // size + driveType + serialNum + labelOffset + label
            using var volMs = new MemoryStream();
            using var volW = new BinaryWriter(volMs);
            volW.Write(volumeIdSize);
            volW.Write(local.DriveType);
            volW.Write(local.DriveSerialNumber);
            volW.Write(0x10); // VolumeLabelOffset = 16 (offset within VolumeID structure)
            volW.Write(volumeLabelAnsi);
            volW.Flush();
            volumeIdBytes = volMs.ToArray();

            localBasePathBytes = Encoding.Default.GetBytes(local.BasePath + "\0");
        }

        // Pre-compute CommonNetworkRelativeLink if network
        byte[]? cnrlBytes = null;
        byte[]? commonPathSuffixBytes = null;
        if (hasNetwork)
        {
            var network = info.Network!;
            byte[] shareNameAnsi = Encoding.Default.GetBytes(network.ShareName + "\0");
            // CommonNetworkRelativeLink: size(4) + flags(4) + netNameOffset(4) + deviceNameOffset(4) + networkProviderType(4) + netName
            int cnrlSize = 4 + 4 + 4 + 4 + 4 + shareNameAnsi.Length;
            using var cnrlMs = new MemoryStream();
            using var cnrlW = new BinaryWriter(cnrlMs);
            cnrlW.Write(cnrlSize);
            cnrlW.Write(0); // CommonNetworkRelativeLinkFlags = 0
            cnrlW.Write(0x14); // NetNameOffset = 20 (offset within CNRL)
            cnrlW.Write(0); // DeviceNameOffset = 0
            cnrlW.Write(0x00020000); // NetworkProviderType = WNNC_NET_LANMAN
            cnrlW.Write(shareNameAnsi);
            cnrlW.Flush();
            cnrlBytes = cnrlMs.ToArray();

            commonPathSuffixBytes = Encoding.Default.GetBytes(network.CommonPathSuffix + "\0");
        }

        // Compute offsets from start of LinkInfo
        int volumeIdOffset = 0;
        int localBasePathOffset = 0;
        int cnrlOffset = 0;
        int commonPathSuffixOffset = 0;

        int currentOffset = headerSize;

        if (hasLocal)
        {
            volumeIdOffset = currentOffset;
            currentOffset += volumeIdBytes!.Length;
            localBasePathOffset = currentOffset;
            currentOffset += localBasePathBytes!.Length;
        }

        if (hasNetwork)
        {
            cnrlOffset = currentOffset;
            currentOffset += cnrlBytes!.Length;
            commonPathSuffixOffset = currentOffset;
            currentOffset += commonPathSuffixBytes!.Length;
        }
        else
        {
            // CommonPathSuffix is always present, even for local-only
            commonPathSuffixOffset = currentOffset;
            commonPathSuffixBytes = new byte[] { 0 }; // empty null-terminated string
            currentOffset += 1;
        }

        int linkInfoSize = currentOffset;

        // Write the complete LinkInfo structure
        writer.Write(linkInfoSize);
        writer.Write(headerSize);
        writer.Write(flags);
        writer.Write(volumeIdOffset);
        writer.Write(localBasePathOffset);
        writer.Write(cnrlOffset);
        writer.Write(commonPathSuffixOffset);

        if (hasLocal)
        {
            writer.Write(volumeIdBytes!);
            writer.Write(localBasePathBytes!);
        }

        if (hasNetwork)
        {
            writer.Write(cnrlBytes!);
        }

        writer.Write(commonPathSuffixBytes!);
    }

    /// <summary>
    /// Writes a KnownFolderDataBlock (signature 0xA000000B, size 28 bytes).
    /// </summary>
    private static void WriteKnownFolderDataBlock(BinaryWriter writer, KnownFolderData data)
    {
        writer.Write(28); // BlockSize
        writer.Write(KnownFolderBlockSignature);
        writer.Write(data.FolderId.ToByteArray()); // 16 bytes
        writer.Write(data.Offset);
    }

    /// <summary>
    /// Writes a TrackerDataBlock (signature 0xA0000003, size 96 bytes).
    /// </summary>
    private static void WriteTrackerDataBlock(BinaryWriter writer, TrackerData data)
    {
        writer.Write(96); // BlockSize
        writer.Write(TrackerBlockSignature);
        writer.Write(88); // Length
        writer.Write(0);  // Version

        // MachineID: 16 bytes, null-padded
        byte[] machineBytes = new byte[16];
        byte[] nameBytes = Encoding.ASCII.GetBytes(data.MachineId);
        int copyLen = Math.Min(nameBytes.Length, 15);
        Array.Copy(nameBytes, machineBytes, copyLen);
        writer.Write(machineBytes);

        // Droid[0], Droid[1], DroidBirth[0], DroidBirth[1]
        writer.Write(data.VolumeId.ToByteArray());
        writer.Write(data.ObjectId.ToByteArray());
        writer.Write((data.BirthVolumeId ?? data.VolumeId).ToByteArray());
        writer.Write((data.BirthObjectId ?? data.ObjectId).ToByteArray());
    }

    /// <summary>
    /// Writes a PropertyStoreDataBlock (signature 0xA0000009, variable size).
    /// </summary>
    private static void WritePropertyStoreDataBlock(BinaryWriter writer, byte[] data)
    {
        int blockSize = 8 + data.Length;
        writer.Write(blockSize);
        writer.Write(PropertyStoreBlockSignature);
        writer.Write(data);
    }

    /// <summary>
    /// Writes a SpecialFolderDataBlock (signature 0xA0000005, size 16 bytes).
    /// </summary>
    private static void WriteSpecialFolderDataBlock(BinaryWriter writer, SpecialFolderData data)
    {
        writer.Write(16); // BlockSize
        writer.Write(SpecialFolderBlockSignature);
        writer.Write(data.FolderId);
        writer.Write(data.Offset);
    }

    /// <summary>
    /// Creates a Windows Shortcut (.lnk) file in memory and returns its binary content as a byte array.
    /// </summary>
    public static byte[] Create(
        string target,
        string? arguments = null,
        bool padArguments = false,
        string? iconLocation = null,
        int iconIndex = 0,
        string? description = null,
        string? workingDirectory = null,
        bool isPrinterLink = false,
        ShortcutWindowStyle windowStyle = ShortcutWindowStyle.Normal,
        bool runAsAdmin = false,
        byte hotkeyKey = 0,
        HotkeyModifiers hotkeyModifiers = HotkeyModifiers.None) =>
        Create(new ShortcutOptions
        {
            Target = target,
            Arguments = arguments,
            PadArguments = padArguments,
            IconLocation = iconLocation,
            IconIndex = iconIndex,
            Description = description,
            WorkingDirectory = workingDirectory,
            IsPrinterLink = isPrinterLink,
            WindowStyle = windowStyle,
            RunAsAdmin = runAsAdmin,
            HotkeyKey = hotkeyKey,
            HotkeyModifiers = hotkeyModifiers
        });

    /// <summary>
    /// Creates a Windows Shortcut (.lnk) file in memory using the specified options
    /// and returns its binary content as a byte array.
    /// </summary>
    public static byte[] Create(ShortcutOptions options)
    {
        string target = options.Target;
        string? arguments = options.Arguments;
        bool padArguments = options.PadArguments;
        string? iconLocation = options.IconLocation;
        int iconIndex = options.IconIndex;
        string? description = options.Description;
        string? workingDirectory = options.WorkingDirectory;
        bool isPrinterLink = options.IsPrinterLink;
        ShortcutWindowStyle windowStyle = options.WindowStyle;
        bool runAsAdmin = options.RunAsAdmin;
        byte hotkeyKey = options.HotkeyKey;
        HotkeyModifiers hotkeyModifiers = options.HotkeyModifiers;
        bool unicode = options.UseUnicode;

        // --- Header and LinkCLSID ---
        byte[] headerSize = { 0x4C, 0x00, 0x00, 0x00 };
        byte[] linkClsid = new byte[16];
        ParseClsid("00021401-0000-0000-c000-000000000046", linkClsid);

        // --- Flag constants ---
        const int FLAG_HAS_LINK_TARGET_ID_LIST = 0x00000001;
        const int FLAG_HAS_LINK_INFO = 0x00000002;
        const int FLAG_HAS_NAME = 0x00000004;
        const int FLAG_HAS_RELATIVE_PATH = 0x00000008;
        const int FLAG_HAS_WORKING_DIR = 0x00000010;
        const int FLAG_HAS_ARGUMENTS = 0x00000020;
        const int FLAG_HAS_ICON_LOCATION = 0x00000040;
        const int FLAG_IS_UNICODE = 0x00000080;
        const int FLAG_HAS_EXP_SZ = 0x00000200;
        const int FLAG_RUN_AS_USER = 0x00002000;
        const int FLAG_PREFER_ENVIRONMENT_PATH = 0x02000000;

        // --- File attribute bytes ---
        byte[] fileAttrDirectory = { 0x10, 0x00, 0x00, 0x00 };
        byte[] fileAttrFile = { 0x20, 0x00, 0x00, 0x00 };

        // --- Fixed fields ---
        byte[] creationTime = ToFileTimeBytes(options.CreationTime);
        byte[] accessTime = ToFileTimeBytes(options.AccessTime);
        byte[] writeTime = ToFileTimeBytes(options.WriteTime);
        byte[] fileSize = BitConverter.GetBytes(options.FileSize);
        byte[] showCommand = BitConverter.GetBytes((uint)windowStyle);
        byte[] hotkey = { hotkeyKey, (byte)hotkeyModifiers };
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
            | (options.LinkInfo != null ? FLAG_HAS_LINK_INFO : 0)
            | (description != null ? FLAG_HAS_NAME : 0)
            | (options.RelativePath != null ? FLAG_HAS_RELATIVE_PATH : 0)
            | (workingDirectory != null ? FLAG_HAS_WORKING_DIR : 0)
            | (arguments != null ? FLAG_HAS_ARGUMENTS : 0)
            | (iconLocation != null ? FLAG_HAS_ICON_LOCATION : 0)
            | (unicode ? FLAG_IS_UNICODE : 0)
            | (runAsAdmin ? FLAG_RUN_AS_USER : 0)
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

            // Write LinkInfo structure (if provided).
            if (options.LinkInfo != null)
            {
                WriteLinkInfo(writer, options.LinkInfo);
            }

            // Write optional string data (order per spec: description, relative path, working dir, arguments, icon location).
            WriteStringData(writer, description, unicode);
            WriteStringData(writer, options.RelativePath, unicode);
            WriteStringData(writer, workingDirectory, unicode);
            if (padArguments && arguments != null)
                WritePaddedArguments(writer, arguments, unicode);
            else
                WriteStringData(writer, arguments, unicode);
            WriteStringData(writer, iconLocation, unicode);

            // --- Extra data blocks ---

            // EnvironmentVariableDataBlock (if target contains environment variables)
            if (target.Contains("%"))
            {
                WriteEnvironmentDataBlock(writer, target, EnvVarBlockSignature);
            }

            // IconEnvironmentDataBlock
            if (options.IconEnvironmentPath != null)
            {
                WriteEnvironmentDataBlock(writer, options.IconEnvironmentPath, IconEnvBlockSignature);
            }

            // KnownFolderDataBlock
            if (options.KnownFolder != null)
            {
                WriteKnownFolderDataBlock(writer, options.KnownFolder);
            }

            // SpecialFolderDataBlock
            if (options.SpecialFolder != null)
            {
                WriteSpecialFolderDataBlock(writer, options.SpecialFolder);
            }

            // TrackerDataBlock
            if (options.Tracker != null)
            {
                WriteTrackerDataBlock(writer, options.Tracker);
            }

            // PropertyStoreDataBlock
            if (options.PropertyStoreData != null)
            {
                WritePropertyStoreDataBlock(writer, options.PropertyStoreData);
            }

            // Terminate extra data chain with a 4-byte zero.
            writer.Write((uint)0);

            writer.Flush();
            return ms.ToArray();
        }
    }
}
