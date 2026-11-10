using Stellar.Kernel;
using Stellar.Kernel.Quantization;

namespace Stellar.Core.Quantization;

/// <inheritdoc/>
public class MetaQuant
    : IMetaQuant
{
    /// <inheritdoc/>
    public IIdentifier UID { get; }

    /// <summary>
    /// Creates a meta quant with a new registered identifier.
    /// </summary>
    public MetaQuant()
        : this(Identifier.CreateAndRegister())
    {
    }

    /// <summary>
    /// Creates a meta quant.
    /// </summary>
    /// <param name="identifier">Unique ID.</param>
    public MetaQuant(IIdentifier identifier)
    {
        ArgumentNullException.ThrowIfNull(identifier);
        UID = identifier;
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return UID.GetHashCode();
    }
}
