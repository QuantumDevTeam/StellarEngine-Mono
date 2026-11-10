using Stellar.Kernel;

namespace Stellar.Core.Data.Collections;

/// <summary>
/// Compares identifiers by value: two <see cref="IIdentifier"/> instances with the same UID are equal,
/// whatever their implementation is.
/// </summary>
public sealed class IdentifierComparer
    : IEqualityComparer<IIdentifier>
{
    /// <summary>
    /// Gets the shared instance.
    /// </summary>
    public static IdentifierComparer Instance { get; } = new();

    private IdentifierComparer()
    {
    }

    /// <inheritdoc/>
    public bool Equals(IIdentifier? x, IIdentifier? y)
    {
        return ReferenceEquals(x, y) || (x is not null && y is not null && x.UID == y.UID);
    }

    /// <inheritdoc/>
    public int GetHashCode(IIdentifier obj)
    {
        return obj.UID.GetHashCode();
    }
}
