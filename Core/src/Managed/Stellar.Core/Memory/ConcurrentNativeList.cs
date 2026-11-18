using Stellar.Core.Global;
using Stellar.Core.Native;

namespace Stellar.Core.Memory;

/// <summary>
/// A list of plain values in native memory, safe from any thread and from native code: the native list of the
/// engine, where everything runs on many threads.
/// </summary>
/// <remarks>
/// <para>The list has a lock in its header, which C# and C++ take the same way (<c>NativeListLock</c> of the native
/// Kernel): many readers or one writer, and a waiting writer goes before new readers.</para>
/// <para>Each call takes the lock for itself. For more than one step, or to work on the elements as a span, hold it
/// in a scope:</para>
/// <code>
/// using (var items = list.Read())
///     foreach (var particle in items.Items) Draw(particle);
///
/// using (var items = list.Write())
/// {
///     items.Add(spawned);
///     items.Items[0].Life -= delta;
/// }
/// </code>
/// <para>A span from a scope is valid while the scope is held. The lock is not reentrant: a scope must not call the
/// list it holds. Dispose the list when nobody else uses it: disposing takes the lock as a writer.</para>
/// </remarks>
/// <typeparam name="T">A plain value: no references.</typeparam>
public sealed unsafe class ConcurrentNativeList<T>
    : IDisposable
    where T : unmanaged
{
    // An address, so that disposing takes it away in one exchange
    private nint _header;

    /// <summary>
    /// Makes an empty list.
    /// </summary>
    /// <param name="capacity">How many elements to make room for now.</param>
    /// <param name="allocator">Where the memory comes from; the allocator of the current engine if <c>null</c>.</param>
    public ConcurrentNativeList(int capacity = 0, NativeAllocator? allocator = null)
    {
        Allocator = allocator ?? CoreEngine.Current.Native.Allocator;
        _header = (nint)NativeListStorage.Create(Allocator, sizeof(T), capacity);
    }

    /// <summary>
    /// Frees what a list nobody disposed holds.
    /// </summary>
    ~ConcurrentNativeList()
    {
        Free();
    }

    /// <summary>
    /// Gets the allocator of its memory.
    /// </summary>
    public NativeAllocator Allocator { get; }

    /// <summary>
    /// Gets how many elements it has now: by the time it is used, another thread may have changed it.
    /// </summary>
    public int Count => Volatile.Read(ref Header->Count);

    /// <summary>
    /// Gets whether it was disposed.
    /// </summary>
    public bool IsDisposed => Volatile.Read(ref _header) == 0;

    /// <summary>
    /// Gets the header, a <c>StellarNativeList*</c>: native code works on the same elements, under the same lock.
    /// </summary>
    public nint Pointer => (nint)Header;

    private NativeListHeader* Header =>
        Volatile.Read(ref _header) is var header and not 0 ? (NativeListHeader*)header : throw new ObjectDisposedException(GetType().Name);

    /// <summary>
    /// Holds the lock for reading: the elements do not change while the scope is held.
    /// </summary>
    /// <returns>The scope; dispose it.</returns>
    public ReadScope Read()
    {
        var header = Header;
        NativeListLock.EnterRead(header);
        return new ReadScope(header);
    }

    /// <summary>
    /// Holds the lock for writing: nobody else reads or changes the list while the scope is held.
    /// </summary>
    /// <returns>The scope; dispose it.</returns>
    public WriteScope Write()
    {
        var header = Header;
        NativeListLock.EnterWrite(header);
        return new WriteScope(this, header);
    }

    /// <summary>
    /// Adds an element at the end.
    /// </summary>
    /// <param name="item">The element.</param>
    public void Add(in T item)
    {
        using var scope = Write();
        scope.Add(in item);
    }

    /// <summary>
    /// Adds elements at the end, in one step.
    /// </summary>
    /// <param name="items">The elements, in order.</param>
    public void AddRange(ReadOnlySpan<T> items)
    {
        using var scope = Write();
        scope.AddRange(items);
    }

    /// <summary>
    /// Copies an element.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <param name="item">The element, or <c>default</c>.</param>
    /// <returns><c>true</c> if the list had that index.</returns>
    public bool TryGet(int index, out T item)
    {
        using var scope = Read();
        if ((uint)index < (uint)scope.Count)
        {
            item = scope.Items[index];
            return true;
        }

        item = default;
        return false;
    }

    /// <summary>
    /// Replaces an element.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <param name="item">The new element.</param>
    /// <returns><c>true</c> if the list had that index.</returns>
    public bool TrySet(int index, in T item)
    {
        using var scope = Write();
        if ((uint)index >= (uint)scope.Count) return false;

        scope.Items[index] = item;
        return true;
    }

    /// <summary>
    /// Removes an element by moving the last one into its place.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <param name="removed">The removed element, or <c>default</c>.</param>
    /// <returns><c>true</c> if the list had that index.</returns>
    public bool TryRemoveAtSwapBack(int index, out T removed)
    {
        using var scope = Write();
        if ((uint)index < (uint)scope.Count)
        {
            removed = scope.RemoveAtSwapBack(index);
            return true;
        }

        removed = default;
        return false;
    }

    /// <summary>
    /// Removes and gives back the last element.
    /// </summary>
    /// <param name="item">The element, or <c>default</c>.</param>
    /// <returns><c>true</c> if it had one.</returns>
    public bool TryPop(out T item)
    {
        using var scope = Write();
        if (scope.Count > 0)
        {
            item = scope.RemoveAtSwapBack(scope.Count - 1);
            return true;
        }

        item = default;
        return false;
    }

    /// <summary>
    /// Removes every element, keeping the memory.
    /// </summary>
    public void Clear()
    {
        using var scope = Write();
        scope.Clear();
    }

    /// <summary>
    /// Copies the elements into a managed array, as they are at one moment.
    /// </summary>
    /// <returns>A new array.</returns>
    public T[] ToArray()
    {
        using var scope = Read();
        return scope.Items.ToArray();
    }

    /// <summary>
    /// Frees the memory, once nobody holds the lock. The list cannot be used after, and native code must not hold
    /// its header any more.
    /// </summary>
    public void Dispose()
    {
        var header = Volatile.Read(ref _header);
        if (header == 0) return;

        NativeListLock.EnterWrite((NativeListHeader*)header);
        Free();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        var header = Volatile.Read(ref _header);
        return header == 0
            ? $"ConcurrentNativeList<{typeof(T).Name}> (disposed)"
            : $"ConcurrentNativeList<{typeof(T).Name}> ({Volatile.Read(ref ((NativeListHeader*)header)->Count)})";
    }

    private void Free()
    {
        var header = Interlocked.Exchange(ref _header, 0);
        if (header != 0) NativeListStorage.Destroy(Allocator, (NativeListHeader*)header);
    }

    /// <summary>
    /// The lock held for reading.
    /// </summary>
    public ref struct ReadScope
    {
        private NativeListHeader* _header;

        internal ReadScope(NativeListHeader* header)
        {
            _header = header;
        }

        /// <summary>
        /// Gets how many elements there are.
        /// </summary>
        public readonly int Count => _header->Count;

        /// <summary>
        /// Gets the elements: valid while the scope is held.
        /// </summary>
        public readonly ReadOnlySpan<T> Items => NativeListStorage.Items<T>(_header);

        /// <summary>
        /// Lets the lock go.
        /// </summary>
        public void Dispose()
        {
            if (_header == null) return;

            NativeListLock.ExitRead(_header);
            _header = null;
        }
    }

    /// <summary>
    /// The lock held for writing: the list as one owner has it.
    /// </summary>
    public ref struct WriteScope
    {
        private readonly ConcurrentNativeList<T> _list;
        private NativeListHeader* _header;

        internal WriteScope(ConcurrentNativeList<T> list, NativeListHeader* header)
        {
            _list = list;
            _header = header;
        }

        /// <summary>
        /// Gets how many elements there are.
        /// </summary>
        public readonly int Count => _header->Count;

        /// <summary>
        /// Gets the elements: valid while the scope is held and the list does not grow.
        /// </summary>
        public readonly Span<T> Items => NativeListStorage.Items<T>(_header);

        /// <summary>
        /// Adds an element at the end.
        /// </summary>
        public readonly void Add(in T item)
        {
            NativeListStorage.Add(_list.Allocator, _header, in item);
        }

        /// <summary>
        /// Adds elements at the end, growing once.
        /// </summary>
        public readonly void AddRange(ReadOnlySpan<T> items)
        {
            NativeListStorage.AddRange(_list.Allocator, _header, items);
        }

        /// <summary>
        /// Inserts an element, moving the following ones.
        /// </summary>
        public readonly void Insert(int index, in T item)
        {
            NativeListStorage.Insert(_list.Allocator, _header, index, in item);
        }

        /// <summary>
        /// Removes an element, moving the following ones: the order stays.
        /// </summary>
        public readonly T RemoveAt(int index)
        {
            return NativeListStorage.RemoveAt<T>(_header, index);
        }

        /// <summary>
        /// Removes an element by moving the last one into its place.
        /// </summary>
        public readonly T RemoveAtSwapBack(int index)
        {
            return NativeListStorage.RemoveAtSwapBack<T>(_header, index);
        }

        /// <summary>
        /// Removes every element, keeping the memory.
        /// </summary>
        public readonly void Clear()
        {
            NativeListStorage.Clear(_header);
        }

        /// <summary>
        /// Makes room for at least a number of elements.
        /// </summary>
        public readonly void EnsureCapacity(int capacity)
        {
            NativeListStorage.Reserve(_list.Allocator, _header, capacity);
        }

        /// <summary>
        /// Lets the lock go.
        /// </summary>
        public void Dispose()
        {
            if (_header == null) return;

            NativeListLock.ExitWrite(_header);
            _header = null;
        }
    }
}
