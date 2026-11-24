using Stellar.Kernel.Data.Context;
using Stellar.Kernel.FailureSystem;
using Stellar.Kernel.Lifecycle;
using Stellar.Kernel.LoggingSystem;
using Stellar.Kernel.Services;

namespace Stellar.Kernel.EntryPoint;

/// <summary>
/// Context passed to <see cref="StellarEntryPoint.Run{TContext}"/> when a module is started.
/// </summary>
/// <remarks>
/// <para>Provides the entry point with essential services: logging and failure handling.</para>
/// <para>All properties are nullable because the engine may not provide them in every environment.</para>
/// </remarks>
public interface IRunContext
    : IContext
{
    /// <summary>
    /// Gets the logger instance for the context, or <c>null</c> if logging is not available.
    /// </summary>
    ILogger? Logger { get; }

    /// <summary>
    /// Gets the failure system of the engine, or <c>null</c> before Stellar.Core publishes it.
    /// </summary>
    IFailureSystem? Failures { get; }

    /// <summary>
    /// Gets the services of the engine, or <c>null</c> if the engine has none yet.
    /// </summary>
    IServiceCatalog? Services { get; }

    /// <summary>
    /// Gets the engine the module runs in: its control, its clocks, its loops. <c>null</c> outside an engine (tests).
    /// </summary>
    IEngineInstance? Engine { get; }
}
