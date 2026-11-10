using Stellar.Core.Data.Registry;
using Stellar.Kernel;
using Stellar.Kernel.Quantization;

namespace Stellar.Core.Quantization;

/// <inheritdoc cref="IRegistrableMetaQuant"/>
/// <typeparam name="TMeta">
/// The concrete type of the meta quant. It selects the <see cref="MetaQuantsRegistry{TMeta}"/> it is registered in.
/// </typeparam>
public abstract class RegistrableMetaQuant<TMeta>
    : MetaQuant, IRegistrableMetaQuant
    where TMeta : RegistrableMetaQuant<TMeta>
{
    /// <inheritdoc/>
    protected RegistrableMetaQuant(IIdentifier identifier)
        : base(identifier)
    {
        _home = Global.CoreEngine.Current.Registry.MetaQuants<TMeta>();
        Register(_home);
    }

    // The registry of the engine the meta quant was made in: it leaves that one, wherever it is disposed
    private readonly IQuantumObject _home;

    /// <inheritdoc/>
    public void Register(IQuantumObject registry)
    {
        RegistryBinding.Register(registry, (TMeta)this);
    }

    /// <inheritdoc/>
    public void Unregister(IQuantumObject registry)
    {
        RegistryBinding.Unregister<TMeta>(registry, UID);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Unregister(_home);
    }
}
