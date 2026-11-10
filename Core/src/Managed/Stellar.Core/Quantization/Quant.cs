using Stellar.Kernel;
using Stellar.Kernel.Quantization;

namespace Stellar.Core.Quantization;

/// <inheritdoc/>
/// <typeparam name="TMeta">Type of the primary meta.</typeparam>
public abstract class Quant<TMeta>
    : IQuant
    where TMeta : IMetaQuant
{
    // Replaced on change, never mutated: readers need no lock, and attaching is rare.
    private volatile IMetaQuant[] _metas;

    /// <summary>
    /// Creates a quant.
    /// </summary>
    /// <param name="meta">Its primary meta.</param>
    protected Quant(TMeta meta)
    {
        ArgumentNullException.ThrowIfNull(meta);

        MetaQuant = meta;
        _metas = [meta];
    }

    /// <summary>
    /// Primary meta quant typed as <typeparamref name="TMeta"/>.
    /// </summary>
    public TMeta MetaQuant { get; }

    /// <inheritdoc/>
    public IMetaQuant Meta => _metas[0];

    /// <inheritdoc/>
    public IReadOnlyList<IMetaQuant> Metas => _metas;

    /// <inheritdoc/>
    public IIdentifier UID => Meta.UID;

    /// <inheritdoc/>
    public TMeta2? FindMeta<TMeta2>()
        where TMeta2 : class, IMetaQuant
    {
        foreach (var meta in _metas)
            if (meta is TMeta2 typed)
                return typed;

        return null;
    }

    /// <summary>
    /// Attaches a meta quant. One meta quant may be attached to any number of quants.
    /// </summary>
    /// <param name="meta">The meta quant to attach.</param>
    /// <returns><c>false</c> if it was attached already.</returns>
    public bool Attach(IMetaQuant meta)
    {
        ArgumentNullException.ThrowIfNull(meta);

        lock (this)
        {
            if (_metas.Contains(meta)) return false;

            _metas = [.. _metas, meta];
            return true;
        }
    }

    /// <summary>
    /// Detaches a meta quant. The primary meta cannot be detached.
    /// </summary>
    /// <param name="meta">The meta quant to detach.</param>
    /// <returns><c>true</c> if it was attached and is now detached.</returns>
    public bool Detach(IMetaQuant meta)
    {
        ArgumentNullException.ThrowIfNull(meta);

        lock (this)
        {
            if (ReferenceEquals(meta, _metas[0]) || !_metas.Contains(meta)) return false;

            _metas = _metas.Where(m => !ReferenceEquals(m, meta)).ToArray();
            return true;
        }
    }
}
