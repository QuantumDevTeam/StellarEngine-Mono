using System.Numerics;
using Stellar.Kernel.Interop;

namespace Stellar.Core.Native;

/// <summary>
/// An allocator of the native core: gives native memory, takes it back and counts it.
/// </summary>
/// <remarks>
/// <para>Every engine has one (<see cref="CoreContext.Allocator"/>); anything may make one of its own to count its
/// memory apart. The native allocator lives while it is referenced: by this object until it is collected, and by
/// each block not freed yet, so a block freed after its engine is gone still finds it.</para>
/// <para>The functions are called through the table of the native core, not through P/Invoke stubs. Safe from any
/// thread.</para>
/// </remarks>
public sealed unsafe class NativeAllocator
{
    private readonly nint _handle;

    /// <summary>
    /// Makes an allocator of its own.
    /// </summary>
    public NativeAllocator()
        : this(MemoryFunctions.Api->Create(), adopt: true)
    {
    }

    internal NativeAllocator(nint handle, bool adopt)
    {
        if (handle == 0) throw new OutOfMemoryException("The native core could not make an allocator");

        _handle = handle;
        if (!adopt) MemoryFunctions.Api->Acquire(handle);
    }

    /// <summary>
    /// Drops the reference of this object. Not disposable, on purpose: one allocator object is shared by everything
    /// that allocates from it, so nobody may dispose it.
    /// </summary>
    ~NativeAllocator()
    {
        MemoryFunctions.Api->Release(_handle);
    }

    /// <summary>
    /// Gets the allocator of the default context: for code outside any engine.
    /// </summary>
    public static NativeAllocator Default => CoreContext.Default.Allocator;

    /// <summary>
    /// Gets the native allocator, a <c>StellarAllocator*</c>: for a native list, or to give a native module.
    /// </summary>
    public nint Handle => _handle;

    /// <summary>
    /// Gets what it holds now and held so far.
    /// </summary>
    public NativeMemoryStats Stats
    {
        get
        {
            NativeMemoryStats stats;
            MemoryFunctions.Api->GetStats(_handle, &stats);
            return stats;
        }
    }

    /// <summary>
    /// Gives a block.
    /// </summary>
    /// <param name="size">Its size in bytes; 0 gives no block.</param>
    /// <param name="alignment">Its alignment, a power of two.</param>
    /// <returns>The block; 0 for a size of 0.</returns>
    /// <exception cref="ArgumentException">Thrown for an alignment that is not a power of two.</exception>
    /// <exception cref="OutOfMemoryException">Thrown when out of native memory.</exception>
    public nint Allocate(nuint size, nuint alignment)
    {
        CheckAlignment(alignment);
        var memory = MemoryFunctions.Api->Allocate(_handle, size, alignment);
        return memory != 0 || size == 0 ? memory : throw new OutOfMemoryException($"No native block of {size} bytes");
    }

    /// <summary>
    /// Grows or shrinks a block, keeping its first bytes.
    /// </summary>
    /// <param name="memory">The block; 0 allocates one.</param>
    /// <param name="oldSize">Its size now.</param>
    /// <param name="newSize">Its new size; 0 frees it.</param>
    /// <param name="alignment">Its alignment, a power of two.</param>
    /// <returns>The block, maybe moved; 0 for a new size of 0.</returns>
    /// <exception cref="OutOfMemoryException">Thrown when out of native memory: the old block is untouched.</exception>
    public nint Reallocate(nint memory, nuint oldSize, nuint newSize, nuint alignment)
    {
        CheckAlignment(alignment);
        var moved = MemoryFunctions.Api->Reallocate(_handle, memory, oldSize, newSize, alignment);
        return moved != 0 || newSize == 0 ? moved : throw new OutOfMemoryException($"No native block of {newSize} bytes");
    }

    /// <summary>
    /// Frees a block this allocator gave, with the size it has.
    /// </summary>
    /// <param name="memory">The block; 0 does nothing.</param>
    /// <param name="size">Its size.</param>
    public void Free(nint memory, nuint size)
    {
        MemoryFunctions.Api->Free(_handle, memory, size);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        var stats = Stats;
        return $"Native allocator ({stats.AllocatedBytes} bytes in {stats.LiveAllocations} blocks)";
    }

    private static void CheckAlignment(nuint alignment)
    {
        if (!BitOperations.IsPow2(alignment))
            throw new ArgumentException($"An alignment is a power of two, not {alignment}", nameof(alignment));
    }
}
