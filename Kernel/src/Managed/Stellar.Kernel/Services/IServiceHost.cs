using Stellar.Kernel.Quantization;

namespace Stellar.Kernel.Services;

/// <summary>
/// Something that owns the services of the engine.
/// </summary>
/// <remarks>
/// The entry point of Stellar.Core implements it: the runtime, which starts before Stellar.Core and does not
/// reference it, takes the registry from there and publishes its own services in it before Stellar.Core runs.
/// </remarks>
public interface IServiceHost
    : IQuantumObject
{
    /// <summary>
    /// Gets the services of the engine.
    /// </summary>
    IServiceRegistry Services { get; }
}
