namespace Stellar.Kernel.FileSystem;

/// <summary>
/// What happened to a watched file.
/// </summary>
public enum FileChangeKind : byte
{
    /// <summary>The file or directory appeared.</summary>
    Created,

    /// <summary>The content changed.</summary>
    Changed,

    /// <summary>The file or directory is gone.</summary>
    Deleted,

    /// <summary>It moved from <see cref="FileChange.OldPath"/> to <see cref="FileChange.Path"/>.</summary>
    Renamed,

    /// <summary>
    /// Changes were lost (the source dropped them, or the domain was remounted): anything under
    /// <see cref="FileChange.Path"/> may have changed. Read it again.
    /// </summary>
    Invalidated
}

/// <summary>
/// A change of a watched file or directory.
/// </summary>
/// <remarks>
/// Changes come as the source reports them: an editor saving one file may give several. Who reacts to them (the
/// resource system, a hot reload) coalesces them.
/// </remarks>
/// <param name="Kind">What happened.</param>
/// <param name="Path">The path the change is about; for <see cref="FileChangeKind.Renamed"/>, the new one.</param>
/// <param name="OldPath">The old path of a <see cref="FileChangeKind.Renamed"/>; the empty path otherwise.</param>
public readonly record struct FileChange(FileChangeKind Kind, FilePath Path, FilePath OldPath = default);
