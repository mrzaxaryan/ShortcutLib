namespace ShortcutLib;

public static class Shortcut
{
    private static byte[] ToFileTimeBytes(DateTime? dt)
    {
        if (dt is null) return new byte[8];
        long ft = dt.Value.ToFileTimeUtc();
        return BitConverter.GetBytes(ft);
    }

    /// <summary>
    /// Creates a Windows Shortcut (.lnk) file in memory using the specified options
    /// and returns its binary content as a byte array.
    /// </summary>
    public static byte[] Create(ShortcutOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrEmpty(options.Target, nameof(options.Target));

        var pathInfo = TargetPathInfo.Parse(options.Target, options.IsPrinterLink);
        int linkFlags = ComputeFlags(options);

        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        WriteHeader(writer, options, linkFlags, pathInfo);
        IdListWriter.Write(writer, pathInfo);
        writer.Write(new byte[2]); // TerminalID

        if (options.LinkInfo != null)
            ExtraDataBlockWriter.WriteLinkInfo(writer, options.LinkInfo);

        WriteStringData(writer, options);
        WriteExtraDataBlocks(writer, options);

        writer.Write(0u); // Terminate extra data chain
        writer.Flush();
        return ms.ToArray();
    }

    private static int ComputeFlags(ShortcutOptions options)
    {
        int flagEnv = options.Target.Contains("%")
            ? LinkFlags.HasExpSz | LinkFlags.PreferEnvironmentPath
            : 0;

        return LinkFlags.HasLinkTargetIdList
            | (options.LinkInfo != null ? LinkFlags.HasLinkInfo : 0)
            | (options.Description != null ? LinkFlags.HasName : 0)
            | (options.RelativePath != null ? LinkFlags.HasRelativePath : 0)
            | (options.WorkingDirectory != null ? LinkFlags.HasWorkingDir : 0)
            | (options.Arguments != null ? LinkFlags.HasArguments : 0)
            | (options.IconLocation != null ? LinkFlags.HasIconLocation : 0)
            | (options.UseUnicode ? LinkFlags.IsUnicode : 0)
            | (options.RunAsAdmin ? LinkFlags.RunAsUser : 0)
            | flagEnv;
    }

    private static void WriteHeader(BinaryWriter writer, ShortcutOptions options, int linkFlags, TargetPathInfo pathInfo)
    {
        writer.Write(new byte[] { 0x4C, 0x00, 0x00, 0x00 }); // HeaderSize
        writer.Write(LnkConstants.LinkClsid.ToByteArray());
        writer.Write((uint)linkFlags);
        writer.Write(pathInfo.FileAttributes);
        writer.Write(ToFileTimeBytes(options.CreationTime));
        writer.Write(ToFileTimeBytes(options.AccessTime));
        writer.Write(ToFileTimeBytes(options.WriteTime));
        writer.Write(BitConverter.GetBytes(options.FileSize));
        writer.Write(BitConverter.GetBytes(options.IconIndex));
        writer.Write(BitConverter.GetBytes((uint)options.WindowStyle));
        writer.Write(new byte[] { options.HotkeyKey, (byte)options.HotkeyModifiers });
        writer.Write(new byte[2]);  // Reserved
        writer.Write(new byte[4]);  // Reserved2
        writer.Write(new byte[4]);  // Reserved3
    }

    private static void WriteStringData(BinaryWriter writer, ShortcutOptions options)
    {
        bool unicode = options.UseUnicode;
        writer.WriteStringData(options.Description, unicode);
        writer.WriteStringData(options.RelativePath, unicode);
        writer.WriteStringData(options.WorkingDirectory, unicode);
        writer.WriteStringData(options.Arguments, unicode);
        writer.WriteStringData(options.IconLocation, unicode);
    }

    /// <summary>
    /// Parses a Windows Shortcut (.lnk) file from its binary content
    /// and returns a ShortcutOptions object representing its configuration.
    /// </summary>
    public static ShortcutOptions Open(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Length < 76)
            throw new ArgumentException("Data is too short to be a valid .lnk file.", nameof(data));
        return LnkParser.Parse(data);
    }

    /// <summary>
    /// Opens an existing Windows Shortcut (.lnk) file, applies modifications
    /// via the provided callback, and returns the modified file as a new byte array.
    /// </summary>
    public static byte[] Edit(byte[] data, Action<ShortcutOptions> modify)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(modify);

        var options = Open(data);
        modify(options);
        return Create(options);
    }

    private static void WriteExtraDataBlocks(BinaryWriter writer, ShortcutOptions options)
    {
        if (options.Target.Contains("%"))
            writer.WriteEnvironmentDataBlock(options.Target, LnkConstants.EnvVarBlockSignature);

        if (options.IconEnvironmentPath != null)
            writer.WriteEnvironmentDataBlock(options.IconEnvironmentPath, LnkConstants.IconEnvBlockSignature);

        if (options.KnownFolder != null)
            ExtraDataBlockWriter.WriteKnownFolderDataBlock(writer, options.KnownFolder);

        if (options.SpecialFolder != null)
            ExtraDataBlockWriter.WriteSpecialFolderDataBlock(writer, options.SpecialFolder);

        if (options.Tracker != null)
            ExtraDataBlockWriter.WriteTrackerDataBlock(writer, options.Tracker);

        if (options.PropertyStoreData != null)
            ExtraDataBlockWriter.WritePropertyStoreDataBlock(writer, options.PropertyStoreData);
    }
}
