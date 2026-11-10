using Stellar.Core.Data.Registry;
using Stellar.Kernel.Quantization;

namespace Stellar.Core.Quantization;

/// <inheritdoc cref="IRegistrableQuant"/>
/// <typeparam name="T">The concrete type of the quant. It selects the <see cref="QuantsRegistry{T}"/> it is registered in.</typeparam>
/// <typeparam name="TMeta">Type of the primary meta.</typeparam>
public abstract class RegistrableQuant<T, TMeta>
    : Quant<TMeta>, IRegistrableQuant
    where TMeta : IMetaQuant
    where T : RegistrableQuant<T, TMeta>
{
    /// <inheritdoc/>
    protected RegistrableQuant(TMeta meta)
        : base(meta)
    {
        _home = Global.CoreEngine.Current.Registry.Quants<T>();
        Register(_home);
    }

    // The registry of the engine the quant was made in: it leaves that one, wherever it is disposed
    private readonly IQuantumObject _home;

    /// <inheritdoc/>
    public void Register(IQuantumObject registry)
    {
        RegistryBinding.Register(registry, (T)this);
    }

    /// <inheritdoc/>
    public void Unregister(IQuantumObject registry)
    {
        RegistryBinding.Unregister<T>(registry, UID);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Unregister(_home);
    }
}
