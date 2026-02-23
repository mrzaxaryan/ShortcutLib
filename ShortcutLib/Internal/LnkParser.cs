using System.Text;

namespace ShortcutLib;

internal static class LnkParser
{
    internal static ShortcutOptions Parse(byte[] data)
    {
        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);

        var options = new ShortcutOptions();
        uint linkFlags = ReadHeader(reader, options);

        if ((linkFlags & (uint)LinkFlags.HasLinkTargetIdList) != 0)
            ReadIdList(reader, options);

        if ((linkFlags & (uint)LinkFlags.HasLinkInfo) != 0)
            ReadLinkInfo(reader, options);

        // Prefer LinkInfo paths over IDList-reconstructed target.
        // LinkInfo contains clean path strings, while the IDList may yield
        // garbled text from Windows-generated shell items with binary metadata
        // or 8.3 short filenames instead of long names.
        if (options.LinkInfo?.Local != null && !string.IsNullOrEmpty(options.LinkInfo.Local.BasePath))
        {
            options.Target = options.LinkInfo.Local.BasePath;
        }
        else if (options.LinkInfo?.Network != null && !string.IsNullOrEmpty(options.LinkInfo.Network.ShareName))
        {
            string target = options.LinkInfo.Network.ShareName;
            if (!string.IsNullOrEmpty(options.LinkInfo.Network.CommonPathSuffix))
                target += @"\" + options.LinkInfo.Network.CommonPathSuffix;
            options.Target = target;
        }

        ReadStringData(reader, options, linkFlags);
        ReadExtraDataBlocks(reader, options);

        // Read overlay data (anything after the terminal block)
        if (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            int remaining = (int)(reader.BaseStream.Length - reader.BaseStream.Position);
            if (remaining > 0)
                options.OverlayData = reader.ReadBytes(remaining);
        }

        return options;
    }

    private static uint ReadHeader(BinaryReader reader, ShortcutOptions options)
    {
        uint headerSize = reader.ReadUInt32();
        if (headerSize != 0x4C)
            throw new FormatException("Invalid .lnk file: header size mismatch.");

        byte[] clsidBytes = reader.ReadBytes(16);
        Guid clsid = new(clsidBytes);
        if (clsid != LnkConstants.LinkClsid)
            throw new FormatException("Invalid .lnk file: CLSID mismatch.");

        uint linkFlags = reader.ReadUInt32();
        uint fileAttributes = reader.ReadUInt32();
        options.FileAttributes = (FileAttributes)fileAttributes;

        // Timestamps
        long creationTime = reader.ReadInt64();
        long accessTime = reader.ReadInt64();
        long writeTime = reader.ReadInt64();
        options.CreationTime = FileTimeToDateTime(creationTime);
        options.AccessTime = FileTimeToDateTime(accessTime);
        options.WriteTime = FileTimeToDateTime(writeTime);

        options.FileSize = reader.ReadUInt32();
        options.IconIndex = reader.ReadInt32();

        uint windowStyle = reader.ReadUInt32();
        options.WindowStyle = (ShortcutWindowStyle)windowStyle;

        options.HotkeyKey = reader.ReadByte();
        options.HotkeyModifiers = (HotkeyModifiers)reader.ReadByte();

        reader.ReadBytes(10); // Reserved (2 + 4 + 4)

        // Flags → boolean properties
        options.UseUnicode = (linkFlags & (uint)LinkFlags.IsUnicode) != 0;
        options.RunAsAdmin = (linkFlags & (uint)LinkFlags.RunAsUser) != 0;
        options.ForceNoLinkInfo = (linkFlags & (uint)LinkFlags.ForceNoLinkInfo) != 0;
        options.RunInSeparateProcess = (linkFlags & (uint)LinkFlags.RunInSeparateProcess) != 0;
        options.NoPidlAlias = (linkFlags & (uint)LinkFlags.NoPidlAlias) != 0;
        options.ForceNoLinkTrack = (linkFlags & (uint)LinkFlags.ForceNoLinkTrack) != 0;
        options.EnableTargetMetadata = (linkFlags & (uint)LinkFlags.EnableTargetMetadata) != 0;
        options.DisableLinkPathTracking = (linkFlags & (uint)LinkFlags.DisableLinkPathTracking) != 0;
        options.DisableKnownFolderTracking = (linkFlags & (uint)LinkFlags.DisableKnownFolderTracking) != 0;
        options.DisableKnownFolderAlias = (linkFlags & (uint)LinkFlags.DisableKnownFolderAlias) != 0;
        options.AllowLinkToLink = (linkFlags & (uint)LinkFlags.AllowLinkToLink) != 0;
        options.UnaliasOnSave = (linkFlags & (uint)LinkFlags.UnaliasOnSave) != 0;
        options.KeepLocalIDListForUNCTarget = (linkFlags & (uint)LinkFlags.KeepLocalIDListForUNCTarget) != 0;

        return linkFlags;
    }

    private static void ReadIdList(BinaryReader reader, ShortcutOptions options)
    {
        ushort totalIdListSize = reader.ReadUInt16();
        long endPosition = reader.BaseStream.Position + totalIdListSize;

        bool isNetworkLink = false;
        string targetRoot = "";
        var leafParts = new List<string>();
        bool isPrinterLink = false;

        int itemIndex = 0;
        while (reader.BaseStream.Position < endPosition)
        {
            ushort itemSize = reader.ReadUInt16();
            if (itemSize == 0) break; // Terminal ID

            byte[] itemData = reader.ReadBytes(itemSize - 2);

            if (itemIndex == 0) // Root shell item
            {
                // itemData[0] = 0x1F (sort index)
                // itemData[1] = 0x50 (local/computer) or 0x58 (network)
                if (itemData.Length >= 2)
                    isNetworkLink = itemData[1] == 0x58;
            }
            else if (itemIndex == 1) // Root path item
            {
                (targetRoot, isPrinterLink) = ParseRootPathItem(itemData, isNetworkLink);
            }
            else // Leaf items (index >= 2, may be multiple for deep paths)
            {
                string part = ParseLeafItem(itemData);
                if (!string.IsNullOrEmpty(part))
                    leafParts.Add(part);
            }

            itemIndex++;
        }

        // Ensure stream is at end of IDList
        reader.BaseStream.Position = endPosition;

        // Reconstruct target path
        string? targetLeaf = leafParts.Count > 0 ? string.Join(@"\", leafParts) : null;

        if (isNetworkLink)
        {
            if (isPrinterLink || targetLeaf == null)
            {
                options.Target = targetRoot;
                options.IsPrinterLink = isPrinterLink;
            }
            else
            {
                options.Target = targetRoot + @"\" + targetLeaf;
            }
        }
        else
        {
            if (targetLeaf == null)
            {
                // Root-only local path (e.g., "C:\") → strip trailing backslash
                options.Target = targetRoot.TrimEnd('\\');
            }
            else
            {
                // targetRoot already has trailing backslash for local paths
                options.Target = targetRoot + targetLeaf;
            }
        }
    }

    private static (string root, bool isPrinter) ParseRootPathItem(byte[] data, bool isNetworkLink)
    {
        int prefixLength;
        bool isPrinter = false;

        if (isNetworkLink)
        {
            prefixLength = 3;
            // Check for printer prefix: 0xC3, 0x02, 0xC1
            if (data.Length >= 3 && data[0] == 0xC3 && data[1] == 0x02 && data[2] == 0xC1)
                isPrinter = true;
        }
        else
        {
            prefixLength = 1; // 0x2F
        }

        // Read only until the first null byte to avoid consuming binary metadata
        // that follows the null-terminated path string in Windows-generated shell items
        int end = Array.IndexOf(data, (byte)0, prefixLength);
        if (end < 0) end = data.Length;
        int length = end - prefixLength;
        if (length <= 0) return ("", isPrinter);

        string root = Encoding.Default.GetString(data, prefixLength, length);
        return (root, isPrinter);
    }

    private static string ParseLeafItem(byte[] data)
    {
        // Leaf items always have a 12-byte prefix (PrefixFile or PrefixFolder)
        const int prefixLength = 12;
        if (data.Length <= prefixLength) return "";

        // Read only until the first null byte to avoid consuming binary metadata
        // (timestamps, extension blocks, Unicode names) that follows the null-terminated
        // short filename in Windows-generated shell items
        int end = Array.IndexOf(data, (byte)0, prefixLength);
        if (end < 0) end = data.Length;
        int length = end - prefixLength;
        if (length <= 0) return "";

        return Encoding.Default.GetString(data, prefixLength, length);
    }

    private static void ReadLinkInfo(BinaryReader reader, ShortcutOptions options)
    {
        long linkInfoStart = reader.BaseStream.Position;

        uint linkInfoSize = reader.ReadUInt32();
        uint linkInfoHeaderSize = reader.ReadUInt32();
        uint flags = reader.ReadUInt32();
        uint volumeIdOffset = reader.ReadUInt32();
        uint localBasePathOffset = reader.ReadUInt32();
        uint cnrlOffset = reader.ReadUInt32();
        uint commonPathSuffixOffset = reader.ReadUInt32();

        var linkInfo = new LinkInfo();
        bool hasLocal = (flags & 0x01) != 0;
        bool hasNetwork = (flags & 0x02) != 0;

        if (hasLocal)
        {
            // Read VolumeID
            reader.BaseStream.Position = linkInfoStart + volumeIdOffset;
            uint volumeIdSize = reader.ReadUInt32();
            uint driveType = reader.ReadUInt32();
            uint driveSerialNumber = reader.ReadUInt32();
            uint volumeLabelOffset = reader.ReadUInt32();

            int labelLength = (int)(volumeIdSize - volumeLabelOffset);
            reader.BaseStream.Position = linkInfoStart + volumeIdOffset + volumeLabelOffset;
            string volumeLabel = reader.ReadFixedAnsiString(labelLength).TrimEnd('\0');

            // Read LocalBasePath
            reader.BaseStream.Position = linkInfoStart + localBasePathOffset;
            string basePath = reader.ReadNullTerminatedAnsiString();

            linkInfo.Local = new LocalPathInfo
            {
                BasePath = basePath,
                DriveType = driveType,
                DriveSerialNumber = driveSerialNumber,
                VolumeLabel = volumeLabel
            };
        }

        if (hasNetwork)
        {
            // Read CommonNetworkRelativeLink
            reader.BaseStream.Position = linkInfoStart + cnrlOffset;
            uint cnrlSize = reader.ReadUInt32();
            uint cnrlFlags = reader.ReadUInt32();
            uint netNameOffset = reader.ReadUInt32();
            uint deviceNameOffset = reader.ReadUInt32();
            uint networkProviderType = reader.ReadUInt32();

            // Read ShareName at netNameOffset within the CNRL
            reader.BaseStream.Position = linkInfoStart + cnrlOffset + netNameOffset;
            string shareName = reader.ReadNullTerminatedAnsiString();

            // Read DeviceName if ValidDevice flag is set
            string? deviceName = null;
            if ((cnrlFlags & 0x01) != 0 && deviceNameOffset > 0)
            {
                reader.BaseStream.Position = linkInfoStart + cnrlOffset + deviceNameOffset;
                deviceName = reader.ReadNullTerminatedAnsiString();
            }

            // Read NetworkProviderType if ValidNetType flag is set
            uint? providerType = null;
            if ((cnrlFlags & 0x02) != 0)
                providerType = networkProviderType;

            // Read CommonPathSuffix
            reader.BaseStream.Position = linkInfoStart + commonPathSuffixOffset;
            string commonPathSuffix = reader.ReadNullTerminatedAnsiString();

            linkInfo.Network = new NetworkPathInfo
            {
                ShareName = shareName,
                CommonPathSuffix = commonPathSuffix,
                DeviceName = deviceName,
                NetworkProviderType = providerType
            };
        }

        if (hasLocal || hasNetwork)
            options.LinkInfo = linkInfo;

        // Ensure stream is past LinkInfo
        reader.BaseStream.Position = linkInfoStart + linkInfoSize;
    }

    private static void ReadStringData(BinaryReader reader, ShortcutOptions options, uint linkFlags)
    {
        bool unicode = (linkFlags & (uint)LinkFlags.IsUnicode) != 0;

        if ((linkFlags & (uint)LinkFlags.HasName) != 0)
            options.Description = ReadStringEntry(reader, unicode);

        if ((linkFlags & (uint)LinkFlags.HasRelativePath) != 0)
            options.RelativePath = ReadStringEntry(reader, unicode);

        if ((linkFlags & (uint)LinkFlags.HasWorkingDir) != 0)
            options.WorkingDirectory = ReadStringEntry(reader, unicode);

        if ((linkFlags & (uint)LinkFlags.HasArguments) != 0)
            options.Arguments = ReadStringEntry(reader, unicode);

        if ((linkFlags & (uint)LinkFlags.HasIconLocation) != 0)
            options.IconLocation = ReadStringEntry(reader, unicode);
    }

    private static string ReadStringEntry(BinaryReader reader, bool unicode)
    {
        ushort charCount = reader.ReadUInt16();
        if (unicode)
        {
            byte[] bytes = reader.ReadBytes(charCount * 2);
            return Encoding.Unicode.GetString(bytes);
        }
        else
        {
            byte[] bytes = reader.ReadBytes(charCount);
            return Encoding.Default.GetString(bytes);
        }
    }

    private static void ReadExtraDataBlocks(BinaryReader reader, ShortcutOptions options)
    {
        while (reader.BaseStream.Position + 4 <= reader.BaseStream.Length)
        {
            uint blockSize = reader.ReadUInt32();
            if (blockSize < 4) break; // Terminal block (0x00000000)

            if (reader.BaseStream.Position + 4 > reader.BaseStream.Length) break;
            uint signature = reader.ReadUInt32();
            int dataLength = (int)blockSize - 8;

            switch (signature)
            {
                case LnkConstants.EnvVarBlockSignature:
                    ReadEnvironmentVariableDataBlock(reader, options);
                    break;
                case LnkConstants.IconEnvBlockSignature:
                    ReadIconEnvironmentDataBlock(reader, options);
                    break;
                case LnkConstants.KnownFolderBlockSignature:
                    ReadKnownFolderDataBlock(reader, options);
                    break;
                case LnkConstants.SpecialFolderBlockSignature:
                    ReadSpecialFolderDataBlock(reader, options);
                    break;
                case LnkConstants.TrackerBlockSignature:
                    ReadTrackerDataBlock(reader, options);
                    break;
                case LnkConstants.PropertyStoreBlockSignature:
                    ReadPropertyStoreDataBlock(reader, options, dataLength);
                    break;
                case LnkConstants.ConsoleBlockSignature:
                    ReadConsoleDataBlock(reader, options);
                    break;
                case LnkConstants.ConsoleFEBlockSignature:
                    ReadConsoleFEDataBlock(reader, options);
                    break;
                case LnkConstants.DarwinBlockSignature:
                    ReadDarwinDataBlock(reader, options);
                    break;
                case LnkConstants.ShimBlockSignature:
                    ReadShimDataBlock(reader, options, dataLength);
                    break;
                case LnkConstants.VistaIdListBlockSignature:
                    ReadVistaIdListDataBlock(reader, options, dataLength);
                    break;
                default:
                    // Skip unknown blocks
                    if (dataLength > 0)
                        reader.ReadBytes(dataLength);
                    break;
            }
        }
    }

    /// <summary>
    /// Reads the 260-byte ANSI + 520-byte Unicode buffer layout shared by
    /// EnvironmentVariable, IconEnvironment, and Darwin data blocks.
    /// Returns the Unicode value if present, otherwise the ANSI value.
    /// </summary>
    private static string? ReadEnvironmentStyleBuffer(BinaryReader reader)
    {
        byte[] ansiBuffer = reader.ReadBytes(LnkConstants.MaxPath);
        byte[] unicodeBuffer = reader.ReadBytes(LnkConstants.MaxPath * 2);

        string unicodePath = Encoding.Unicode.GetString(unicodeBuffer).TrimEnd('\0');
        if (!string.IsNullOrEmpty(unicodePath))
            return unicodePath;

        string ansiPath = Encoding.Default.GetString(ansiBuffer).TrimEnd('\0');
        return !string.IsNullOrEmpty(ansiPath) ? ansiPath : null;
    }

    private static void ReadEnvironmentVariableDataBlock(BinaryReader reader, ShortcutOptions options)
    {
        var value = ReadEnvironmentStyleBuffer(reader);
        if (value != null)
            options.Target = value;
    }

    private static void ReadIconEnvironmentDataBlock(BinaryReader reader, ShortcutOptions options)
    {
        var value = ReadEnvironmentStyleBuffer(reader);
        if (value != null)
            options.IconEnvironmentPath = value;
    }

    private static void ReadKnownFolderDataBlock(BinaryReader reader, ShortcutOptions options)
    {
        byte[] guidBytes = reader.ReadBytes(16);
        uint offset = reader.ReadUInt32();
        options.KnownFolder = new KnownFolderData
        {
            FolderId = new Guid(guidBytes),
            Offset = offset
        };
    }

    private static void ReadSpecialFolderDataBlock(BinaryReader reader, ShortcutOptions options)
    {
        uint folderId = reader.ReadUInt32();
        uint offset = reader.ReadUInt32();
        options.SpecialFolder = new SpecialFolderData
        {
            FolderId = folderId,
            Offset = offset
        };
    }

    private static void ReadTrackerDataBlock(BinaryReader reader, ShortcutOptions options)
    {
        uint length = reader.ReadUInt32();  // 88
        uint version = reader.ReadUInt32(); // 0

        byte[] machineBytes = reader.ReadBytes(16);
        string machineId = Encoding.ASCII.GetString(machineBytes).TrimEnd('\0');

        Guid volumeId = new(reader.ReadBytes(16));
        Guid objectId = new(reader.ReadBytes(16));
        Guid birthVolumeId = new(reader.ReadBytes(16));
        Guid birthObjectId = new(reader.ReadBytes(16));

        options.Tracker = new TrackerData
        {
            MachineId = machineId,
            VolumeId = volumeId,
            ObjectId = objectId,
            BirthVolumeId = birthVolumeId == volumeId ? null : birthVolumeId,
            BirthObjectId = birthObjectId == objectId ? null : birthObjectId
        };
    }

    private static void ReadPropertyStoreDataBlock(BinaryReader reader, ShortcutOptions options, int dataLength)
    {
        options.PropertyStoreData = reader.ReadBytes(dataLength);
    }

    private static void ReadConsoleDataBlock(BinaryReader reader, ShortcutOptions options)
    {
        var data = new ConsoleData();
        data.FillAttributes = reader.ReadUInt16();
        data.PopupFillAttributes = reader.ReadUInt16();
        data.ScreenBufferSizeX = reader.ReadInt16();
        data.ScreenBufferSizeY = reader.ReadInt16();
        data.WindowSizeX = reader.ReadInt16();
        data.WindowSizeY = reader.ReadInt16();
        data.WindowOriginX = reader.ReadInt16();
        data.WindowOriginY = reader.ReadInt16();
        reader.ReadInt32(); // Unused1
        reader.ReadInt32(); // Unused2
        data.FontSize = reader.ReadUInt32();
        data.FontFamily = reader.ReadUInt32();
        data.FontWeight = reader.ReadUInt32();
        data.FaceName = reader.ReadFixedUnicodeString(64);
        data.CursorSize = reader.ReadUInt32();
        data.FullScreen = reader.ReadUInt32() != 0;
        data.QuickEdit = reader.ReadUInt32() != 0;
        data.InsertMode = reader.ReadUInt32() != 0;
        data.AutoPosition = reader.ReadUInt32() != 0;
        data.HistoryBufferSize = reader.ReadUInt32();
        data.NumberOfHistoryBuffers = reader.ReadUInt32();
        data.HistoryNoDup = reader.ReadUInt32() != 0;
        data.ColorTable = new uint[16];
        for (int i = 0; i < 16; i++)
            data.ColorTable[i] = reader.ReadUInt32();
        options.Console = data;
    }

    private static void ReadConsoleFEDataBlock(BinaryReader reader, ShortcutOptions options)
    {
        options.ConsoleCodePage = reader.ReadUInt32();
    }

    private static void ReadDarwinDataBlock(BinaryReader reader, ShortcutOptions options)
    {
        var value = ReadEnvironmentStyleBuffer(reader);
        if (value != null)
            options.DarwinData = value;
    }

    private static void ReadShimDataBlock(BinaryReader reader, ShortcutOptions options, int dataLength)
    {
        if (dataLength <= 0) return;
        byte[] bytes = reader.ReadBytes(dataLength);
        options.ShimLayerName = Encoding.Unicode.GetString(bytes).TrimEnd('\0');
    }

    private static void ReadVistaIdListDataBlock(BinaryReader reader, ShortcutOptions options, int dataLength)
    {
        if (dataLength <= 0) return;
        options.VistaIdListData = reader.ReadBytes(dataLength);
    }

    private static DateTime? FileTimeToDateTime(long fileTime)
    {
        if (fileTime == 0) return null;
        return DateTime.FromFileTimeUtc(fileTime);
    }
}
