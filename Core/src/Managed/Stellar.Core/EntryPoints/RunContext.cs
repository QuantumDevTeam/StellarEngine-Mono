using Stellar.Kernel.EntryPoint;
using Stellar.Kernel.Failures;
using Stellar.Kernel.Lifecycle;
using Stellar.Kernel.LoggingSystem;
using Stellar.Kernel.Quantization;
using Stellar.Kernel.Services;

namespace Stellar.Core.EntryPoints;

/// <inheritdoc/>
/// <param name="sender">The quant that starts the module.</param>
/// <param name="logger">An already initialized logger.</param>
/// <param name="failureDispatcher">A failure dispatcher for failures during the run.</param>
/// <param name="services">The services of the engine.</param>
/// <param name="engine">The engine the module runs in.</param>
public readonly ref struct RunContext(
    IQuantumObject? sender,
    ILogger? logger = null,
    IFailureDispatcher? failureDispatcher = null,
    IServiceRegistry? services = null,
    IEngineInstance? engine = null
) : IRunContext
{
    /// <inheritdoc/>
    public IQuantumObject? Sender { get; } = sender;

    /// <inheritdoc/>
    public ILogger? Logger { get; } = logger;

    /// <inheritdoc/>
    public IFailureDispatcher? FailureDispatcher { get; } = failureDispatcher;

    /// <inheritdoc/>
    public IServiceRegistry? Services { get; } = services;

    /// <inheritdoc/>
    public IEngineInstance? Engine { get; } = engine;
}
