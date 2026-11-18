using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Stellar.Core.Native;

namespace Stellar.Core.Memory;

/// <summary>
/// The header of a native list, in native memory. Mirrors <c>StellarNativeList</c> of the native Kernel.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct NativeListHeader
{
    public nint Data;
    public int Count;
    public int Capacity;
    public int ElementSize;
    public int Lock;
    public uint Version;
    public uint Alignment;
    public nint Allocator;
}

/// <summary>
/// The lock of a concurrent native list, taken as <c>NativeListLock</c> of the native Kernel takes it: the two sides
/// share it.
/// </summary>
/// <remarks>
/// Many readers or one writer. A writer that waits stops new readers, so a stream of readers never starves it. Not
/// reentrant: a holder that takes it again waits for itself.
/// </remarks>
internal static unsafe class NativeListLock
{
    private const int Writer = 0x40000000;
    private const int Waiting = 0x20000000;

    public static void EnterRead(NativeListHeader* list)
    {
        ref var state = ref list->Lock;
        var spins = 0;
        while (true)
        {
            var current = Volatile.Read(ref state);
            if ((current & (Writer | Waiting)) == 0 && Interlocked.CompareExchange(ref state, current + 1, current) == current)
                return;

            Backoff(ref spins);
        }
    }

    public static void ExitRead(NativeListHeader* list)
    {
        Interlocked.Decrement(ref list->Lock);
    }

    public static void EnterWrite(NativeListHeader* list)
    {
        ref var state = ref list->Lock;
        var spins = 0;
        while (true)
        {
            var current = Volatile.Read(ref state);
            if ((current & ~Waiting) == 0)
            {
                if (Interlocked.CompareExchange(ref state, Writer, current) == current) return;
            }
            else if ((current & Waiting) == 0)
            {
                Interlocked.CompareExchange(ref state, current | Waiting, current);
            }

            Backoff(ref spins);
        }
    }

    public static void ExitWrite(NativeListHeader* list)
    {
        Interlocked.And(ref list->Lock, ~Writer);
    }

    // Held for nanoseconds, so spin first, longer each time, so that waiters stop hammering the line of the lock;
    // then give the processor away
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Backoff(ref int spins)
    {
        if (spins < 10) Thread.SpinWait(1 << spins);
        else Thread.Yield();

        spins++;
    }
}

/// <summary>
/// What every native list does to its header and its buffer: shared by <see cref="NativeList{T}"/> and
/// <see cref="ConcurrentNativeList{T}"/>, which add the locking.
/// </summary>
internal static unsafe class NativeListStorage
{
    /// <summary>
    /// The alignment of every buffer: enough for SIMD types.
    /// </summary>
    public const uint Alignment = 16;

    public static NativeListHeader* Create(NativeAllocator allocator, int elementSize, int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);

        // The header is a block of the allocator: it holds a reference, so the allocator outlives its engine as long
        // as the list does
        var header = (NativeListHeader*)allocator.Allocate((nuint)sizeof(NativeListHeader), (nuint)IntPtr.Size);
        *header = default;
        header->ElementSize = elementSize;
        header->Alignment = Alignment;
        header->Allocator = allocator.Handle;

        try
        {
            Reserve(allocator, header, capacity);
        }
        catch
        {
            allocator.Free((nint)header, (nuint)sizeof(NativeListHeader));
            throw;
        }

        return header;
    }

    public static void Destroy(NativeAllocator allocator, NativeListHeader* header)
    {
        allocator.Free(header->Data, (nuint)header->ElementSize * (nuint)header->Capacity);
        allocator.Free((nint)header, (nuint)sizeof(NativeListHeader));
    }

    public static void Reserve(NativeAllocator allocator, NativeListHeader* header, int capacity)
    {
        if (capacity <= header->Capacity) return;

        var size = (nuint)header->ElementSize;
        header->Data = allocator.Reallocate(header->Data, size * (nuint)header->Capacity, size * (nuint)capacity, header->Alignment);
        header->Capacity = capacity;
        header->Version++;
    }

    public static void TrimExcess(NativeAllocator allocator, NativeListHeader* header)
    {
        if (header->Count == header->Capacity) return;

        var size = (nuint)header->ElementSize;
        header->Data = allocator.Reallocate(header->Data, size * (nuint)header->Capacity, size * (nuint)header->Count, header->Alignment);
        header->Capacity = header->Count;
        header->Version++;
    }

    // Doubles, as List<T> does, and at least what is needed
    public static void Grow(NativeAllocator allocator, NativeListHeader* header, int needed)
    {
        if (needed < 0) throw new OutOfMemoryException("A native list holds at most int.MaxValue elements");

        var doubled = header->Capacity < 4 ? 4 : (int)Math.Min((long)header->Capacity * 2, Array.MaxLength);
        Reserve(allocator, header, Math.Max(needed, doubled));
    }

    public static Span<T> Items<T>(NativeListHeader* header)
        where T : unmanaged
    {
        return new Span<T>((void*)header->Data, header->Count);
    }

    public static void Add<T>(NativeAllocator allocator, NativeListHeader* header, in T item)
        where T : unmanaged
    {
        if (header->Count == header->Capacity) Grow(allocator, header, header->Count + 1);

        ((T*)header->Data)[header->Count] = item;
        header->Count++;
    }

    public static void AddRange<T>(NativeAllocator allocator, NativeListHeader* header, ReadOnlySpan<T> items)
        where T : unmanaged
    {
        if (items.IsEmpty) return;

        var needed = header->Count + items.Length;
        if (needed > header->Capacity) Grow(allocator, header, needed);

        items.CopyTo(new Span<T>((T*)header->Data + header->Count, items.Length));
        header->Count = needed;
    }

    public static void Insert<T>(NativeAllocator allocator, NativeListHeader* header, int index, in T item)
        where T : unmanaged
    {
        if ((uint)index > (uint)header->Count) throw new ArgumentOutOfRangeException(nameof(index));
        if (header->Count == header->Capacity) Grow(allocator, header, header->Count + 1);

        var items = (T*)header->Data;
        new Span<T>(items + index, header->Count - index).CopyTo(new Span<T>(items + index + 1, header->Count - index));
        items[index] = item;
        header->Count++;
        header->Version++;
    }

    public static T RemoveAt<T>(NativeListHeader* header, int index)
        where T : unmanaged
    {
        CheckIndex(header, index);

        var items = (T*)header->Data;
        var removed = items[index];
        var after = header->Count - index - 1;
        new Span<T>(items + index + 1, after).CopyTo(new Span<T>(items + index, after));
        header->Count--;
        header->Version++;
        return removed;
    }

    public static T RemoveAtSwapBack<T>(NativeListHeader* header, int index)
        where T : unmanaged
    {
        CheckIndex(header, index);

        var items = (T*)header->Data;
        var removed = items[index];
        items[index] = items[header->Count - 1];
        header->Count--;
        header->Version++;
        return removed;
    }

    public static void Clear(NativeListHeader* header)
    {
        header->Count = 0;
        header->Version++;
    }

    public static void CheckIndex(NativeListHeader* header, int index)
    {
        if ((uint)index >= (uint)header->Count)
            throw new ArgumentOutOfRangeException(nameof(index), index, $"The list has {header->Count} elements");
    }
}
