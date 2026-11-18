namespace Stellar.Kernel.FileSystem.Resources;

/// <summary>
/// How the resource system holds the data of a resource while it is leased.
/// </summary>
/// <remarks>
/// Leases of one path share one entry, and the entry holds by the strongest policy of its leases:
/// <see cref="Keep"/> over <see cref="Cache"/> over <see cref="Stream"/>.
/// </remarks>
public enum ResourcePolicy : byte
{
    /// <summary>
    /// Nothing is held: each <see cref="IResource.Open"/> opens the file as it is now. For large data read once,
    /// such as music or video.
    /// </summary>
    Stream,

    /// <summary>
    /// The bytes are held in memory and shared by every lease. With no lease left they stay, until the cache
    /// needs the room (<see cref="IResourceSystem.CacheBudget"/>).
    /// </summary>
    Cache,

    /// <summary>
    /// The source is held open while a lease lives (a file handle, a connection), and nothing is read twice; with no
    /// lease left it is closed at once.
    /// </summary>
    Keep
}
