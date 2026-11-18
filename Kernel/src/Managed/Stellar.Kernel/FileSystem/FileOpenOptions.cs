using System;
using System.IO;

namespace Stellar.Kernel.FileSystem;

/// <summary>
/// How a file is opened, beyond its mode and access.
/// </summary>
[Flags]
public enum FileOpenFlags
{
    /// <summary>Nothing more.</summary>
    None = 0,

    /// <summary>
    /// The file is replaced as a whole when the stream is closed, or not at all if the process stops first: the
    /// domain writes beside the file and swaps it in. For saves and settings. Needs
    /// <see cref="DomainCapabilities.AtomicWrite"/>.
    /// </summary>
    Atomic = 1 << 0,

    /// <summary>A hint: the file is read from start to end once.</summary>
    Sequential = 1 << 1,

    /// <summary>A hint: the file is read at scattered offsets.</summary>
    RandomAccess = 1 << 2
}

/// <summary>
/// How to open a file: the mode and access of <see cref="System.IO"/> and the flags of the engine.
/// </summary>
/// <param name="Mode">Whether the file must exist, is created, truncated or appended to.</param>
/// <param name="Access">Read, write, or both.</param>
/// <param name="Flags">Atomic replacement and hints.</param>
public readonly record struct FileOpenOptions(
    FileMode Mode,
    FileAccess Access,
    FileOpenFlags Flags = FileOpenFlags.None)
{
    /// <summary>
    /// Gets whether these options change the file.
    /// </summary>
    public bool Writes =>
        (Access & FileAccess.Write) != 0 ||
        Mode is FileMode.Create or FileMode.CreateNew or FileMode.Truncate or FileMode.Append;

    #region Defaults

    /// <summary>Reads a file that exists.</summary>
    public static FileOpenOptions Read { get; } = new(FileMode.Open, FileAccess.Read);

    /// <summary>Creates a file, or truncates the one that exists, and writes it.</summary>
    public static FileOpenOptions Write { get; } = new(FileMode.Create, FileAccess.Write);

    /// <summary>Writes at the end of a file, creating it if needed.</summary>
    public static FileOpenOptions Append { get; } = new(FileMode.Append, FileAccess.Write);

    /// <summary>Replaces a file as a whole, or leaves it as it was (<see cref="FileOpenFlags.Atomic"/>).</summary>
    public static FileOpenOptions Replace { get; } = new(FileMode.Create, FileAccess.Write, FileOpenFlags.Atomic);

    #endregion
}
