using System;

namespace Stellar.Kernel.FileSystem;

/// <summary>
/// Whether an entry is a file or a directory.
/// </summary>
public enum FileEntryKind : byte
{
    /// <summary>A file.</summary>
    File,

    /// <summary>A directory.</summary>
    Directory
}

/// <summary>
/// What is known of a file or a directory at one moment: the answer of a domain, not a handle.
/// </summary>
/// <remarks>
/// An entry does not follow the file: read it again, or watch the file (<see cref="IDomain.Watch"/>), to see a change.
/// </remarks>
/// <param name="Path">The path of the entry.</param>
/// <param name="Kind">Whether it is a file or a directory.</param>
/// <param name="Length">The length of a file in bytes; 0 for a directory; -1 when the domain cannot tell.</param>
/// <param name="CreationTimeUtc">When the entry was created, if the domain knows.</param>
/// <param name="LastWriteTimeUtc">When the entry was last written, if the domain knows.</param>
/// <param name="Content">What the file holds; the unknown content for a directory.</param>
/// <param name="Revision">
/// Changes whenever the content changes, as far as the domain can tell: the time of the last write, a counter, a
/// hash. Equal revisions of one path mean the same bytes; 0 when the domain cannot tell, and then nothing can be
/// assumed. Caches compare it.
/// </param>
public readonly record struct FileEntry(
    FilePath Path,
    FileEntryKind Kind,
    long Length,
    DateTime? CreationTimeUtc,
    DateTime? LastWriteTimeUtc,
    ContentType Content,
    long Revision)
{
    /// <summary>
    /// Gets whether the entry is a file.
    /// </summary>
    public bool IsFile => Kind == FileEntryKind.File;

    /// <summary>
    /// Gets whether the entry is a directory.
    /// </summary>
    public bool IsDirectory => Kind == FileEntryKind.Directory;
}
