using System;
using System.Threading;
using System.Threading.Tasks;

namespace Stellar.Kernel.FileSystem;

/// <summary>
/// A file read at any offset, with no position and no stream: packed archives, streaming of large data.
/// </summary>
/// <remarks>
/// Reads at different offsets may run at once, from any thread.
/// </remarks>
public interface IRandomAccessFile
    : IDisposable
{
    /// <summary>
    /// Gets the path of the file.
    /// </summary>
    FilePath Path { get; }

    /// <summary>
    /// Gets the length of the file in bytes.
    /// </summary>
    long Length { get; }

    /// <summary>
    /// Reads bytes at an offset.
    /// </summary>
    /// <param name="offset">Where to start, from the beginning of the file.</param>
    /// <param name="buffer">Where to put the bytes.</param>
    /// <returns>How many bytes were read; fewer than asked only at the end of the file.</returns>
    int Read(long offset, Span<byte> buffer);

    /// <summary>
    /// Reads bytes at an offset.
    /// </summary>
    /// <param name="offset">Where to start, from the beginning of the file.</param>
    /// <param name="buffer">Where to put the bytes.</param>
    /// <param name="cancellationToken">Stops the read.</param>
    /// <returns>How many bytes were read; fewer than asked only at the end of the file.</returns>
    ValueTask<int> ReadAsync(long offset, Memory<byte> buffer, CancellationToken cancellationToken = default);
}
