using Stellar.Kernel;
using Stellar.Kernel.Quantization;

namespace Stellar.Core.Quantization;

/// <inheritdoc/>
public class MetaQuant
    : IMetaQuant
{
    /// <inheritdoc/>
    public Identifier UID { get; }

    /// <summary>
    /// Creates a meta quant with a new identifier.
    /// </summary>
    public MetaQuant()
        : this(Identifier.New())
    {
    }

    /// <summary>
    /// Creates a meta quant.
    /// </summary>
    /// <param name="identifier">The identifier, not empty.</param>
    /// <exception cref="ArgumentException">Thrown for <see cref="Identifier.Empty"/>.</exception>
    public MetaQuant(Identifier identifier)
    {
        if (identifier.IsEmpty) throw new ArgumentException("A meta quant needs an identifier", nameof(identifier));
        UID = identifier;
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return UID.GetHashCode();
    }
}
