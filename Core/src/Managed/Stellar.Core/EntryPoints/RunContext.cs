using Stellar.Kernel.EntryPoint;
using Stellar.Kernel.FailureSystem;
using Stellar.Kernel.Lifecycle;
using Stellar.Kernel.LoggingSystem;
using Stellar.Kernel.Quantization;
using Stellar.Kernel.Services;

namespace Stellar.Core.EntryPoints;

/// <inheritdoc/>
/// <param name="sender">The quant that starts the module.</param>
/// <param name="logger">An already initialized logger.</param>
/// <param name="failures">The failure system of the engine.</param>
/// <param name="services">The services of the engine.</param>
/// <param name="engine">The engine the module runs in.</param>
public readonly ref struct RunContext(
    IQuantumObject? sender,
    ILogger? logger = null,
    IFailureSystem? failures = null,
    IServiceCatalog? services = null,
    IEngineInstance? engine = null
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
