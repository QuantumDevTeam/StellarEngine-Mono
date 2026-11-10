using Stellar.Kernel;
using Stellar.Kernel.Quantization;

namespace Stellar.Core.Data.Registry;

/// <summary>
/// Global registry of the quants of one type.
/// </summary>
/// <typeparam name="T">The type of stored quants.</typeparam>
public sealed class QuantsRegistry<T>
    : ConcurrentRegistry<T>
    where T : class, IRegistrableQuant
{
    /// <summary>
    /// Creates an empty registry. Each engine has one (<see cref="Global.CoreEngine.Registry"/>).
    /// </summary>
    public QuantsRegistry()
    {
    }

    /// <inheritdoc/>
    protected override IIdentifier IdentifierOf(T obj)
    {
        return obj.UID;
    }
}
