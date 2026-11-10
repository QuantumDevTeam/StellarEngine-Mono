using Stellar.Kernel.Data.Collections;
using Stellar.Kernel.Data.Registry;
using Stellar.Kernel.Failures;
using Stellar.Kernel.LoggingSystem;
using Stellar.Kernel.Quantization;
using Stellar.Kernel.Services;

namespace Stellar.Core.Global;

/// <summary>
/// The core of the engine of the running code: a static view of <see cref="CoreEngine.Current"/>.
/// </summary>
/// <remarks>
/// <para>Nothing here is global to the process: every engine instance has its own services, failure levels and
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
    public static IServiceRegistry Services => Current.Services;

    /// <summary>
    /// Gets the failure dispatcher, or <c>null</c> if the Core module has not started yet.
    /// </summary>
    public static IFailureDispatcher? FailureDispatcher => Current.FailureDispatcher;

    /// <summary>
    /// Gets the logger, or <c>null</c> if no logging module has started.
    /// </summary>
    public static ILogger? Logger => Current.Logger;

    /// <summary>
    /// The base failure levels. They exist while the Core module is running.
    /// </summary>
    public static class FailureLevels
    {
        /// <summary>Gets the level that is neither logged nor stops execution.</summary>
        public static IFailureLevel? NonCritical => Current.FailureLevels.NonCritical;

        /// <summary>Gets the level that is logged and does not stop execution.</summary>
        public static IFailureLevel? Warning => Current.FailureLevels.Warning;

        /// <summary>Gets the level that stops the current execution.</summary>
        public static IFailureLevel? Error => Current.FailureLevels.Error;

        /// <summary>Gets the level that terminates the engine.</summary>
        public static IFailureLevel? CriticalError => Current.FailureLevels.CriticalError;
    }

    /// <summary>
    /// The registries.
    /// </summary>
    public static class Registry
    {
        /// <summary>Gets the registry of identifiers.</summary>
        public static IRegistry<Identifier> Identifiers => Current.Registry.Identifiers;

        /// <summary>Gets the registry of labels.</summary>
        public static IRegistry<Label> Labels => Current.Registry.Labels;

        /// <summary>Gets the registry of data containers.</summary>
        public static IRegistry<IDataContainer> DataContainers => Current.Registry.DataContainers;

        /// <summary>Gets the registry of meta quants of one type.</summary>
        public static IRegistry<TMeta> MetaQuants<TMeta>()
            where TMeta : class, IRegistrableMetaQuant
        {
            return Current.Registry.MetaQuants<TMeta>();
        }

        /// <summary>Gets the registry of quants of one type.</summary>
        public static IRegistry<T> Quants<T>()
            where T : class, IRegistrableQuant
        {
            return Current.Registry.Quants<T>();
        }
    }
}
