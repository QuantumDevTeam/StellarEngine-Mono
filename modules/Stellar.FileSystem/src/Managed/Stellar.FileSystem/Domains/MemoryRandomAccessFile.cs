using Stellar.Kernel.FileSystem;

namespace Stellar.FileSystem.Domains;

/// <summary>
/// Reads at offsets of bytes held in memory.
/// </summary>
/// <param name="path">The path of the file.</param>
/// <param name="data">Its bytes.</param>
public sealed class MemoryRandomAccessFile(FilePath path, ReadOnlyMemory<byte> data)
    : IRandomAccessFile
{
    /// <inheritdoc/>
    public FilePath Path { get; } = path;

    /// <inheritdoc/>
    public long Length => data.Length;

    /// <inheritdoc/>
    public int Read(long offset, Span<byte> buffer)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        if (offset >= data.Length) return 0;

        var count = (int)Math.Min(buffer.Length, data.Length - offset);
        data.Span.Slice((int)offset, count).CopyTo(buffer);
        return count;
    }

    /// <inheritdoc/>
    public ValueTask<int> ReadAsync(long offset, Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return new ValueTask<int>(Read(offset, buffer.Span));
    }

    /// <inheritdoc/>
    public void Dispose()
    {
    }
}
