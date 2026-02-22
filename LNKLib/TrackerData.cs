namespace LNKLib;

/// <summary>
/// Data for TrackerDataBlock (signature 0xA0000003).
/// Used by the distributed link tracking service.
/// </summary>
public sealed class TrackerData
{
    /// <summary>NetBIOS machine name (max 15 characters, null-padded to 16 bytes).</summary>
    public required string MachineId { get; init; }

    /// <summary>Volume GUID (Droid[0]).</summary>
    public required Guid VolumeId { get; init; }

    /// <summary>Object GUID (Droid[1]).</summary>
    public required Guid ObjectId { get; init; }

    /// <summary>Birth volume GUID (DroidBirth[0]). Defaults to VolumeId if null.</summary>
    public Guid? BirthVolumeId { get; init; }

    /// <summary>Birth object GUID (DroidBirth[1]). Defaults to ObjectId if null.</summary>
    public Guid? BirthObjectId { get; init; }
}
