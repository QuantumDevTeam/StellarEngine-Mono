using Stellar.Core.Data.Registry;
using Stellar.Kernel.Data.Registry;
using Stellar.Kernel.Quantization;

namespace Stellar.Core.Quantization;

/// <inheritdoc cref="IRegistrableQuant"/>
/// <remarks>
/// <para>A new quant asks the registries of the current engine to take it (implicit registration): it goes into the
/// registry of <typeparamref name="T"/>, and of every type of its hierarchy marked with
/// <see cref="StellarRegistryAttribute"/>. So <c>class Actor : RegistrableQuant&lt;Actor, ActorMeta&gt;</c> puts a
/// <c>Player : Actor</c> into the registry of actors. A class that registers its quants itself turns this off with
/// <see cref="RegistersImplicitly"/>.</para>
/// <para>Disposing the quant removes it from every registry it is in, of whatever engine.</para>
/// </remarks>
/// <typeparam name="T">The home type of the quant: the registry it goes into implicitly.</typeparam>
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
        Registrations = new Registrations(this);

        // Called before the constructors of derived classes: an override returns a constant
        if (RegistersImplicitly) Global.CoreEngine.Current.Registry.RegisterImplicitly(this, typeof(T));
    }

    /// <inheritdoc/>
    public IRegistrations Registrations { get; }

    /// <summary>
    /// Gets whether a new quant goes into the registries of the current engine by itself. <c>true</c> by default.
    /// </summary>
    protected virtual bool RegistersImplicitly => true;

    /// <summary>
    /// Removes the quant from every registry it is in.
    /// </summary>
    public virtual void Dispose()
    {
        Registrations.UnregisterAll();
        GC.SuppressFinalize(this);
    }
}
