using Stellar.Kernel;
using Stellar.Kernel.Quantization;

namespace Stellar.Core.Quantization;

/// <inheritdoc/>
/// <typeparam name="TMeta">Type of the primary meta.</typeparam>
public abstract class Quant<TMeta>
    : IQuant
    where TMeta : IMetaQuant
{
    private readonly IMetaQuant _meta;

    // Made on first need: most quants have only their primary meta, and pay no array for it. Replaced on change,
    // never mutated: readers need no lock, a writer swaps it whole
    private IMetaQuant[]? _metas;

    /// <summary>
    /// Creates a quant.
    /// </summary>
    /// <param name="meta">Its primary meta.</param>
    protected Quant(TMeta meta)
    {
        ArgumentNullException.ThrowIfNull(meta);

        MetaQuant = meta;
        _meta = meta;
    }

    /// <summary>
    /// Primary meta quant typed as <typeparamref name="TMeta"/>.
    /// </summary>
    public TMeta MetaQuant { get; }

    /// <inheritdoc/>
    public IMetaQuant Meta => _meta;

    /// <inheritdoc/>
    public IReadOnlyList<IMetaQuant> Metas => AllMetas();

    /// <inheritdoc/>
    public Identifier UID => _meta.UID;

    /// <inheritdoc/>
    public TMeta2? FindMeta<TMeta2>()
        where TMeta2 : class, IMetaQuant
    {
        if (_meta is TMeta2 primary) return primary;
        if (Volatile.Read(ref _metas) is not { } metas) return null;

        for (var i = 1; i < metas.Length; i++)
            if (metas[i] is TMeta2 typed)
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

        while (true)
        {
            var current = AllMetas();
            if (Array.IndexOf(current, meta) >= 0) return false;
            if (Interlocked.CompareExchange(ref _metas, [.. current, meta], current) == current) return true;
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

        if (ReferenceEquals(meta, _meta)) return false;

        while (true)
        {
            var current = AllMetas();
            var index = Array.IndexOf(current, meta);
            if (index < 0) return false;

            IMetaQuant[] next = [.. current.AsSpan(0, index), .. current.AsSpan(index + 1)];
            if (Interlocked.CompareExchange(ref _metas, next, current) == current) return true;
        }
    }

    private IMetaQuant[] AllMetas()
    {
        return Volatile.Read(ref _metas) ?? Interlocked.CompareExchange(ref _metas, [_meta], null) ?? _metas!;
    }
}
