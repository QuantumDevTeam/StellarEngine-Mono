using System;

namespace Stellar.Kernel.FileSystem;

/// <summary>
/// What a domain can do. A call it cannot do throws <see cref="NotSupportedException"/>, before any work.
/// </summary>
[Flags]
public enum DomainCapabilities
{
    /// <summary>Nothing.</summary>
    None = 0,

    /// <summary>Opens files for reading.</summary>
    Read = 1 << 0,

    /// <summary>Creates, writes, moves and deletes files and directories.</summary>
    Write = 1 << 1,

    /// <summary>Lists the files of a directory.</summary>
    Enumerate = 1 << 2,

    /// <summary>Reports the changes of its files (<see cref="IDomain.Watch"/>).</summary>
    Watch = 1 << 3,

    /// <summary>Reads at any offset without a stream (<see cref="IDomain.OpenRandomAccess"/>).</summary>
    RandomAccess = 1 << 4,

    /// <summary>Holds files in memory and hands them out without a copy (<see cref="IDomain.TryGetMemory"/>).</summary>
    Memory = 1 << 5,

    /// <summary>Replaces a file as a whole or not at all (<see cref="FileOpenFlags.Atomic"/>).</summary>
    AtomicWrite = 1 << 6
}
