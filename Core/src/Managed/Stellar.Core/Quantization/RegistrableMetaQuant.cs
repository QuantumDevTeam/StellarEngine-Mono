using Stellar.Core.Data.Registry;
using Stellar.Kernel;
using Stellar.Kernel.Data.Registry;
using Stellar.Kernel.Quantization;

namespace Stellar.Core.Quantization;

/// <inheritdoc cref="IRegistrableMetaQuant"/>
/// <remarks>
/// <para>A new meta quant asks the registries of the current engine to take it (implicit registration): it goes into
/// the registry of <typeparamref name="TMeta"/>, and of every type of its hierarchy marked with
/// <see cref="StellarRegistryAttribute"/>. A class that registers its meta quants itself turns this off with
/// <see cref="RegistersImplicitly"/>.</para>
/// <para>Disposing the meta quant removes it from every registry it is in, of whatever engine.</para>
/// </remarks>
/// <typeparam name="TMeta">The home type of the meta quant: the registry it goes into implicitly.</typeparam>
public abstract class RegistrableMetaQuant<TMeta>
    : MetaQuant, IRegistrableMetaQuant
    where TMeta : RegistrableMetaQuant<TMeta>
{
    /// <inheritdoc/>
    protected RegistrableMetaQuant(Identifier identifier)
        : base(identifier)
    {
        Registrations = new Registrations(this);

        // Called before the constructors of derived classes: an override returns a constant
        if (RegistersImplicitly) Global.CoreEngine.Current.Registry.RegisterImplicitly(this, typeof(TMeta));
    }

    /// <inheritdoc/>
    public IRegistrations Registrations { get; }

    /// <summary>
    /// Gets whether a new meta quant goes into the registries of the current engine by itself. <c>true</c> by default.
    /// </summary>
    protected virtual bool RegistersImplicitly => true;

    /// <summary>
    /// Removes the meta quant from every registry it is in.
    /// </summary>
    public virtual void Dispose()
    {
        Registrations.UnregisterAll();
        GC.SuppressFinalize(this);
    }
}
