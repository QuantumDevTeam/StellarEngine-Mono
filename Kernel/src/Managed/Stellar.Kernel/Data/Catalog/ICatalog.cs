namespace Stellar.Kernel.Data.Catalog;

/// <summary>
/// Values found by a key, that can be changed: services, content types, drivers.
/// </summary>
/// <remarks>
/// A catalog may refuse a key or a value it cannot hold (an invalid extension, a service that does not fulfil its
/// contract) with an <see cref="System.ArgumentException"/>.
/// </remarks>
/// <typeparam name="TKey">The key.</typeparam>
/// <typeparam name="TValue">The value.</typeparam>
public interface ICatalog<TKey, TValue>
    : IReadOnlyCatalog<TKey, TValue>
    where TKey : notnull
{
    /// <summary>
    /// Sets the value of a key, replacing the one it had.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    void Set(TKey key, TValue value);

    /// <summary>
    /// Adds a value for a key that has none.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <returns><c>true</c> if it was added; <c>false</c> if the key already has a value, which stays.</returns>
    bool TryAdd(TKey key, TValue value);

    /// <summary>
    /// Removes the value of a key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns><c>true</c> if there was one.</returns>
    bool Remove(TKey key);
}
