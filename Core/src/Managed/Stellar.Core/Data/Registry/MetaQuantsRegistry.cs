using Stellar.Kernel;
using Stellar.Kernel.Quantization;

namespace Stellar.Core.Data.Registry;

/// <summary>
/// Global registry of the meta quants of one type.
/// </summary>
/// <typeparam name="TMeta">The type of stored meta quants.</typeparam>
public sealed class MetaQuantsRegistry<TMeta>
    : ConcurrentRegistry<TMeta>
    where TMeta : class, IRegistrableMetaQuant
{
    /// <summary>
    /// Creates an empty registry. Each engine has one (<see cref="Global.CoreEngine.Registry"/>).
    /// </summary>
    public MetaQuantsRegistry()
    {
    }

    /// <inheritdoc/>
    protected override IIdentifier IdentifierOf(TMeta obj)
    {
        return obj.UID;
    }
}
