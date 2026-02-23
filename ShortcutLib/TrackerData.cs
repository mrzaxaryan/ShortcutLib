namespace ShortcutLib;

/// <summary>
/// Data for TrackerDataBlock (signature 0xA0000003).
/// Used by the distributed link tracking service.
/// </summary>
public sealed class TrackerData
{
    /// <summary>NetBIOS machine name (max 15 characters, null-padded to 16 bytes).</summary>
    public string MachineId { get; set; } = "";

    /// <summary>Volume GUID (Droid[0]).</summary>
    public Guid VolumeId { get; set; }

    /// <summary>Object GUID (Droid[1]).</summary>
    public Guid ObjectId { get; set; }

    /// <summary>Birth volume GUID (DroidBirth[0]). Defaults to VolumeId if null.</summary>
    public Guid? BirthVolumeId { get; set; }

    /// <summary>Birth object GUID (DroidBirth[1]). Defaults to ObjectId if null.</summary>
    public Guid? BirthObjectId { get; set; }
}
