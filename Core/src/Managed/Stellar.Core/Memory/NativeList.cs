using System.Runtime.CompilerServices;
using Stellar.Core.Global;
using Stellar.Core.Native;

namespace Stellar.Core.Memory;

/// <summary>
/// A list of plain values in native memory, with one owner: no garbage collector, no copy to give it to native code.
/// </summary>
/// <remarks>
/// <para>The elements live in a buffer of a native allocator, the one of the current engine by default. C# works on
/// them as a <see cref="Span{T}"/> (<see cref="AsSpan"/>, <see cref="this[int]"/> by reference), C++ as a
/// <c>std::span</c> over the same header (<see cref="Pointer"/>, <c>NativeListView</c> of the native Kernel): nothing
/// is copied either way.</para>
/// <para>One owner: no lock, so only one thread works on it at a time; share it between threads as a
/// <see cref="ConcurrentNativeList{T}"/>. A span or a reference into it is valid until the list grows; an enumeration
/// that sees the list grow or its elements move throws instead of reading freed memory.</para>
/// <para>Dispose it: the finalizer frees what a forgotten list holds, but late, and never while native code may still
/// hold the header.</para>
/// </remarks>
/// <typeparam name="T">A plain value: no references.</typeparam>
public sealed unsafe class NativeList<T>
    : IDisposable
    where T : unmanaged
{
    private NativeListHeader* _header;

    /// <summary>
    /// Makes an empty list.
    /// </summary>
    /// <param name="capacity">How many elements to make room for now.</param>
    /// <param name="allocator">Where the memory comes from; the allocator of the current engine if <c>null</c>.</param>
    public NativeList(int capacity = 0, NativeAllocator? allocator = null)
    {
        Allocator = allocator ?? CoreEngine.Current.Native.Allocator;
        _header = NativeListStorage.Create(Allocator, sizeof(T), capacity);
    }

    /// <summary>
    /// Makes a list of a copy of some elements.
    /// </summary>
    /// <param name="items">The elements, in order.</param>
    /// <param name="allocator">Where the memory comes from; the allocator of the current engine if <c>null</c>.</param>
    public NativeList(ReadOnlySpan<T> items, NativeAllocator? allocator = null)
        : this(items.Length, allocator)
    {
        NativeListStorage.AddRange(Allocator, _header, items);
    }

    /// <summary>
    /// Frees what a list nobody disposed holds.
    /// </summary>
    ~NativeList()
    {
        Free();
    }

    /// <summary>
    /// Gets the allocator of its memory.
    /// </summary>
    public NativeAllocator Allocator { get; }

    /// <summary>
    /// Gets how many elements it has.
    /// </summary>
    public int Count => Header->Count;

    /// <summary>
    /// Gets how many elements it holds before it grows.
    /// </summary>
    public int Capacity => Header->Capacity;

    /// <summary>
    /// Gets whether it has no elements.
    /// </summary>
    public bool IsEmpty => Header->Count == 0;

    /// <summary>
    /// Gets whether it was disposed.
    /// </summary>
    public bool IsDisposed => _header == null;

    /// <summary>
    /// Gets the header, a <c>StellarNativeList*</c>: for native code, which works on the same elements.
    /// </summary>
    public nint Pointer => (nint)Header;

    private NativeListHeader* Header
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _header != null ? _header : throw new ObjectDisposedException(GetType().Name);
    }

    /// <summary>
    /// Gets an element by reference: valid until the list grows.
    /// </summary>
    /// <param name="index">The index.</param>
    public ref T this[int index]
    {
        get
        {
            var header = Header;
            NativeListStorage.CheckIndex(header, index);
            return ref ((T*)header->Data)[index];
        }
    }

    /// <summary>
    /// Gets the elements as they are: valid until the list grows.
    /// </summary>
    /// <returns>The elements.</returns>
    public Span<T> AsSpan()
    {
        return NativeListStorage.Items<T>(Header);
    }

    /// <summary>
    /// Adds an element at the end.
    /// </summary>
    /// <param name="item">The element.</param>
    public void Add(in T item)
    {
        var header = Header;
        var count = header->Count;
        if (count < header->Capacity)
        {
            ((T*)header->Data)[count] = item;
            header->Count = count + 1;
            return;
        }

        NativeListStorage.Add(Allocator, header, in item);
    }

    /// <summary>
    /// Adds elements at the end, growing once.
    /// </summary>
    /// <param name="items">The elements, in order.</param>
    public void AddRange(ReadOnlySpan<T> items)
    {
        NativeListStorage.AddRange(Allocator, Header, items);
    }

    /// <summary>
    /// Inserts an element, moving the following ones.
    /// </summary>
    /// <param name="index">Where; <see cref="Count"/> adds at the end.</param>
    /// <param name="item">The element.</param>
    public void Insert(int index, in T item)
    {
        NativeListStorage.Insert(Allocator, Header, index, in item);
    }

    /// <summary>
    /// Removes an element, moving the following ones: the order stays.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <returns>The removed element.</returns>
    public T RemoveAt(int index)
    {
        return NativeListStorage.RemoveAt<T>(Header, index);
    }

    /// <summary>
    /// Removes an element by moving the last one into its place: no shift, the order changes.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <returns>The removed element.</returns>
    public T RemoveAtSwapBack(int index)
    {
        return NativeListStorage.RemoveAtSwapBack<T>(Header, index);
    }

    /// <summary>
    /// Removes and gives back the last element.
    /// </summary>
    /// <param name="item">The element, or <c>default</c>.</param>
    /// <returns><c>true</c> if it had one.</returns>
    public bool TryPop(out T item)
    {
        var header = Header;
        if (header->Count == 0)
        {
            item = default;
            return false;
        }

        item = NativeListStorage.RemoveAt<T>(header, header->Count - 1);
        return true;
    }

    /// <summary>
    /// Removes every element, keeping the memory.
    /// </summary>
    public void Clear()
    {
        NativeListStorage.Clear(Header);
    }

    /// <summary>
    /// Makes room for at least a number of elements.
    /// </summary>
    /// <param name="capacity">The number.</param>
    public void EnsureCapacity(int capacity)
    {
        NativeListStorage.Reserve(Allocator, Header, capacity);
    }

    /// <summary>
    /// Gives back the memory beyond the elements.
    /// </summary>
    public void TrimExcess()
    {
        NativeListStorage.TrimExcess(Allocator, Header);
    }

    /// <summary>
    /// Copies the elements into a managed array.
    /// </summary>
    /// <returns>A new array.</returns>
    public T[] ToArray()
    {
        return AsSpan().ToArray();
    }

    /// <summary>
    /// Enumerates the elements by reference: <c>foreach (ref var item in list)</c>.
    /// </summary>
    public Enumerator GetEnumerator()
    {
        return new Enumerator(Header);
    }

    /// <summary>
    /// Frees the memory. The list cannot be used after, and native code must not hold its header any more.
    /// </summary>
    public void Dispose()
    {
        Free();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return _header == null ? $"NativeList<{typeof(T).Name}> (disposed)" : $"NativeList<{typeof(T).Name}> ({_header->Count})";
    }

    private void Free()
    {
        var header = _header;
        if (header == null) return;

        _header = null;
        NativeListStorage.Destroy(Allocator, header);
    }

    /// <summary>
    /// Enumerates a native list by reference; throws if the list grows or its elements move meanwhile.
    /// </summary>
    public ref struct Enumerator
    {
        private readonly NativeListHeader* _header;
        private readonly uint _version;
        private int _index;

        internal Enumerator(NativeListHeader* header)
        {
            _header = header;
            _version = header->Version;
            _index = -1;
        }

        /// <summary>
        /// Gets the current element.
        /// </summary>
        public readonly ref T Current => ref ((T*)_header->Data)[_index];

        /// <summary>
        /// Moves to the next element.
        /// </summary>
        /// <returns><c>false</c> at the end.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the list grew or its elements moved.</exception>
        public bool MoveNext()
        {
            if (_header->Version != _version)
                throw new InvalidOperationException("The native list changed during the enumeration");

            return ++_index < _header->Count;
        }
    }
}
