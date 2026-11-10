using Stellar.Core.Native;
using Stellar.Kernel;
using Stellar.Kernel.Data.Registry;

namespace Stellar.Core.Data.Registry;

/// <summary>
/// The registry of identifiers of a <see cref="CoreContext"/>. It holds no state: it is a view of the native registry.
/// </summary>
public sealed unsafe class IdentifierRegistry
    : IRegistry<Identifier>
{
    private readonly CoreContext _context;

    internal IdentifierRegistry(CoreContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public bool Exists(IIdentifier id)
    {
        return id is not null && Contains(id.UID);
    }

    /// <inheritdoc/>
    public bool Register(Identifier obj)
    {
        ArgumentNullException.ThrowIfNull(obj);

        var uid = obj.UID;
        int added;
        return Exports.Ask(Exports.stellar_identifier_register(_context.Handle, &uid, &added), added);
    }

    /// <inheritdoc/>
    public Identifier? Get(IIdentifier id)
    {
        return id is null ? null : Get(id.UID);
    }

    /// <summary>
    /// Gets a registered identifier by value.
    /// </summary>
    /// <param name="uid">The value.</param>
    /// <returns>The identifier, or <c>null</c> if it is not registered.</returns>
    public Identifier? Get(Guid uid)
    {
        return Contains(uid) ? new Identifier(uid) : null;
    }

    /// <inheritdoc/>
    public Identifier? Pop(IIdentifier id)
    {
        if (id is null) return null;

        var uid = id.UID;
        int removed;
        return Exports.Ask(Exports.stellar_identifier_unregister(_context.Handle, &uid, &removed), removed)
            ? new Identifier(uid)
            : null;
    }

    /// <inheritdoc/>
    public int Size
    {
        get
        {
            nuint count;
            Exports.Check(Exports.stellar_identifier_count(_context.Handle, &count));
            return (int)count;
        }
    }

    /// <inheritdoc/>
    public ICollection<IIdentifier> Keys => Values.Cast<IIdentifier>().ToArray();

    /// <inheritdoc/>
    public ICollection<Identifier> Values
    {
        get
        {
            var context = _context.Handle;
            return Exports.ReadIdentifiers((buffer, capacity, required) =>
                    Exports.stellar_identifier_snapshot(context, buffer, capacity, required))
                .Select(uid => new Identifier(uid))
                .ToArray();
        }
    }

    private bool Contains(Guid uid)
    {
        int contained;
        return Exports.Ask(Exports.stellar_identifier_contains(_context.Handle, &uid, &contained), contained);
    }
}
