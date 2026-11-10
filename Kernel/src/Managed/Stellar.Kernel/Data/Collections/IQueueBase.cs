using Stellar.Kernel.Quantization;

namespace Stellar.Kernel.Data.Collections;

/// <summary>
/// Base contract of an engine queue that stores value-type items.
/// </summary>
/// <typeparam name="T">Item type. Items are structures so that a queue never allocates per element.</typeparam>
public interface IQueueBase<T>
    : IRegistrableQuant
    where T : struct
{
    /// <summary>
    /// Gets the number of items waiting in the queue.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Gets a value indicating whether the queue has no items.
    /// </summary>
    bool IsEmpty { get; }

    /// <summary>
    /// Adds an item to the queue.
    /// </summary>
    /// <param name="item">The item to add.</param>
    void Enqueue(T item);

    /// <summary>
    /// Attempts to take the next item from the queue.
    /// </summary>
    /// <param name="item">The taken item, or <c>default</c> when the queue is empty.</param>
    /// <returns><c>true</c> if an item was taken; otherwise, <c>false</c>.</returns>
    bool TryDequeue(out T item);

    /// <summary>
    /// Removes all items from the queue.
    /// </summary>
    void Clear();
}
