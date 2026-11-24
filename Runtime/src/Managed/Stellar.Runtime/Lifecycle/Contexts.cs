using Stellar.Kernel.EntryPoint;
using Stellar.Kernel.FailureSystem;
using Stellar.Kernel.Lifecycle;
using Stellar.Kernel.LoggingSystem;
using Stellar.Kernel.Quantization;
using Stellar.Kernel.Services;

namespace Stellar.Runtime.Lifecycle;

/// <summary>
/// What an entry point gets from the runtime when it starts.
/// </summary>
/// <param name="sender">The runtime.</param>
/// <param name="logger">The logger of the engine, if there is one yet.</param>
/// <param name="failures">The failure system, if there is one yet.</param>
/// <param name="services">The services of the engine, if there are any yet.</param>
/// <param name="engine">The engine.</param>
public readonly ref struct RuntimeRunContext(
    IQuantumObject? sender,
    ILogger? logger,
    IFailureSystem? failures,
    IServiceCatalog? services,
    IEngineInstance? engine
) : IRunContext
{
    /// <inheritdoc/>
    public IQuantumObject? Sender { get; } = sender;

    /// <inheritdoc/>
    public ILogger? Logger { get; } = logger;

    /// <inheritdoc/>
    public IFailureSystem? Failures { get; } = failures;

    /// <inheritdoc/>
    public IServiceCatalog? Services { get; } = services;

    /// <inheritdoc/>
    public IEngineInstance? Engine { get; } = engine;
}

/// <summary>
/// What an entry point gets from the runtime when it is stopped.
/// </summary>
/// <param name="sender">The runtime.</param>
/// <param name="reason">Why.</param>
public readonly ref struct RuntimeStopContext(IQuantumObject? sender, StopReason reason)
    : IStopContext
{
    /// <inheritdoc/>
    public IQuantumObject? Sender { get; } = sender;

    /// <inheritdoc/>
    public StopReason Reason { get; } = reason;
}
