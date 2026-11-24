using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Stellar.Core.Data.Registry;
using Stellar.Core.Native;
using Stellar.Core.Services;
using Stellar.Kernel.Data.Collections;
using Stellar.Kernel.Data.Registry;
using Stellar.Kernel.FailureSystem;
using Stellar.Kernel.Lifecycle;
using Stellar.Kernel.LoggingSystem;
using Stellar.Kernel.Quantization;
using Stellar.Kernel.Services;

namespace Stellar.Core.Global;

/// <summary>
/// What Stellar.Core keeps for one engine: its services, its failure system, its registries, managed and native.
/// </summary>
/// <remarks>
/// <para>Every engine instance has its own (<see cref="For"/>): two engines in one process (an editor and the game it
/// runs) share nothing of it. <see cref="Current"/> is the one of the engine of the running code
/// (<see cref="EngineScope"/>), or <see cref="Default"/> outside any engine (tools, tests).
/// <see cref="Engine"/> is a static view of <see cref="Current"/>.</para>
/// <para>Objects that register themselves when they are made (quants, data containers) remember the registry of the
/// engine they were made in, and leave that one when they are disposed, wherever that happens.</para>
/// </remarks>
public sealed class CoreEngine
    : IDisposable
{
    private static readonly ConditionalWeakTable<IEngineInstance, CoreEngine> Engines = new();

    private CoreEngine(string name, CoreContext native, IEngineInstance? instance)
    {
        Name = name;
        Native = native;
        Instance = instance;
        Registry = new CoreRegistries();
    }

    /// <summary>
    /// Gets the core of the code that runs outside any engine.
    /// </summary>
    public static CoreEngine Default { get; } = new("Default", CoreContext.Default, null);

    /// <summary>
    /// Gets the core of the engine of the running code, or <see cref="Default"/>.
    /// </summary>
    public static CoreEngine Current => EngineScope.Resolve() is { } engine ? For(engine) : Default;

    /// <summary>
    /// Gets the name: the one of the engine, or <c>Default</c>.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the engine this core belongs to, or <c>null</c> for <see cref="Default"/>.
    /// </summary>
    public IEngineInstance? Instance { get; }

    /// <summary>
    /// Gets the registries of the native core of this engine.
    /// </summary>
    public CoreContext Native { get; }

    /// <summary>
    /// Gets the services published by the modules of this engine.
    /// </summary>
    public IServiceCatalog Services { get; } = new ServiceCatalog();

    /// <summary>
    /// Gets the failure system, or <c>null</c> if the Core module has not started yet.
    /// </summary>
    public IFailureSystem? Failures => Services.Find<IFailureSystem>();

    /// <summary>
    /// Gets the logger, or <c>null</c> if no logging module has started.
    /// </summary>
    public ILogger? Logger => Services.Find<ILogger>();

    /// <summary>
    /// Gets the registries.
    /// </summary>
    public CoreRegistries Registry { get; }

    /// <summary>
    /// Gets whether this core was disposed: its engine is gone.
    /// </summary>
    public bool IsDisposed { get; private set; }

    /// <summary>
    /// Gets the core of an engine, created the first time.
    /// </summary>
    /// <param name="engine">The engine.</param>
    /// <returns>Its core.</returns>
    public static CoreEngine For(IEngineInstance engine)
    {
        ArgumentNullException.ThrowIfNull(engine);
        return Engines.GetValue(engine,
            static instance => new CoreEngine(instance.Name, CoreContext.For(instance), instance));
    }

    /// <summary>
    /// Disposes the core of an engine, if it has one: its native registries go with it.
    /// </summary>
    /// <param name="engine">The engine.</param>
    /// <returns>Whether it had one.</returns>
    public static bool Release(IEngineInstance engine)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (engine is null || !Engines.TryGetValue(engine, out var core)) return false;

        Engines.Remove(engine);
        core.Dispose();
        return true;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (IsDisposed || Instance is null) return;

        IsDisposed = true;
        CoreContext.Release(Instance);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"Core of {Name}";
    }

    /// <summary>
    /// The registries of one engine: one per type, made the first time it is asked for.
    /// </summary>
    /// <remarks>
    /// <para><see cref="Of{T}"/> is the registry of a type; a <c>Player</c> derived from <c>Actor</c> is found in the
    /// registry of actors. <see cref="RegisterImplicitly"/> is what the base classes of Stellar.Core call for a new
    /// object: it goes into the registry of its home type and of every type of its hierarchy marked with
    /// <see cref="StellarRegistryAttribute"/>.</para>
    /// </remarks>
    public sealed class CoreRegistries
    {
        private readonly ConcurrentDictionary<Type, IRegistry> _registries = new();

        // The registries an object of a type goes to, worked out once per type: registries are never taken away
        private readonly ConcurrentDictionary<(Type Item, Type? Home), IUntypedRegistry[]> _routes = new();

        internal CoreRegistries()
        {
        }

        /// <summary>Gets the registry of data containers.</summary>
        public IRegistry<IDataContainer> DataContainers => Of<IDataContainer>();

        /// <summary>Gets the registries made so far.</summary>
        public IReadOnlyCollection<IRegistry> All => _registries.Values.ToArray();

        /// <summary>Gets the registry of a type.</summary>
        /// <typeparam name="T">The type: a base class or a contract.</typeparam>
        public IRegistry<T> Of<T>()
            where T : class, IRegistrableQuantumObject
        {
            return (IRegistry<T>)_registries.GetOrAdd(typeof(T), static _ => new Registry<T>());
        }

        /// <summary>Gets the registry of a type known at run time.</summary>
        /// <param name="type">The type: a base class or a contract of registrable objects.</param>
        public IRegistry Of(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);
            if (_registries.TryGetValue(type, out var registry)) return registry;

            RegistryTypes.Validate(type);
            return _registries.GetOrAdd(type, static type =>
                (IRegistry)Activator.CreateInstance(typeof(Registry<>).MakeGenericType(type))!);
        }

        /// <summary>
        /// Registers a new object where it belongs: in the registry of its home type, and of every type of its
        /// hierarchy marked with <see cref="StellarRegistryAttribute"/>.
        /// </summary>
        /// <param name="item">The object.</param>
        /// <param name="home">The type it is registered as, whatever its hierarchy; <c>null</c> for none.</param>
        public void RegisterImplicitly(IRegistrableQuantumObject item, Type? home)
        {
            ArgumentNullException.ThrowIfNull(item);

            var route = _routes.GetOrAdd((item.GetType(), home),
                static (key, registries) => registries.Route(key.Item, key.Home), this);
            foreach (var registry in route) registry.Register(item);
        }

        private IUntypedRegistry[] Route(Type item, Type? home)
        {
            var route = new List<IUntypedRegistry>();
            if (home is not null) route.Add((IUntypedRegistry)Of(home));
            foreach (var type in RegistryTypes.MarkedIn(item))
                if (type != home)
                    route.Add((IUntypedRegistry)Of(type));

            return [.. route];
        }
    }
}
