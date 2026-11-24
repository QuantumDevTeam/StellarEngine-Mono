using Stellar.Kernel.Data.Collections;
using Stellar.Kernel.Data.Registry;
using Stellar.Kernel.FailureSystem;
using Stellar.Kernel.LoggingSystem;
using Stellar.Kernel.Quantization;
using Stellar.Kernel.Services;

namespace Stellar.Core.Global;

/// <summary>
/// The core of the engine of the running code: a static view of <see cref="CoreEngine.Current"/>.
/// </summary>
/// <remarks>
/// <para>Nothing here is global to the process: every engine instance has its own services, failure system and
/// registries, and this class shows those of the engine the running code belongs to
/// (<see cref="Kernel.Lifecycle.EngineScope"/>). Outside any engine it shows <see cref="CoreEngine.Default"/>.</para>
/// <para>Services are published by the modules that implement them. Nothing is assumed to exist: lower levels must
/// use the nullable accessors and survive when a service is missing.</para>
/// </remarks>
public static class Engine
{
    /// <summary>
    /// Gets the core of the engine of the running code.
    /// </summary>
    public static CoreEngine Current => CoreEngine.Current;

    /// <summary>
    /// Gets the services published by the modules.
    /// </summary>
    public static IServiceCatalog Services => Current.Services;

    /// <summary>
    /// Gets the failure system of the current engine, or <c>null</c> if its Core module has not started yet.
    /// </summary>
    public static IFailureSystem? Failures => Current.Failures;

    /// <summary>
    /// Gets the logger, or <c>null</c> if no logging module has started.
    /// </summary>
    public static ILogger? Logger => Current.Logger;

    /// <summary>
    /// The registries of the current engine.
    /// </summary>
    public static class Registry
    {
        /// <summary>Gets the registry of data containers.</summary>
        public static IRegistry<IDataContainer> DataContainers => Current.Registry.DataContainers;

        /// <summary>Gets the registry of a type.</summary>
        /// <typeparam name="T">The type: a base class or a contract.</typeparam>
        public static IRegistry<T> Of<T>()
            where T : class, IRegistrableQuantumObject
        {
            return Current.Registry.Of<T>();
        }
    }
}
