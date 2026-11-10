using Stellar.Kernel.Quantization;

namespace Stellar.Kernel.Services;

/// <summary>
/// Scoped lookup of engine services by their contract.
/// </summary>
/// <remarks>
/// Modules publish implementations of Kernel contracts here during startup and other modules find them without a
/// build-time dependency on each other. This is what keeps level 2 modules independent.
/// </remarks>
public interface IServiceRegistry
    : IQuantumObject
{
    /// <summary>
    /// Publishes a service, replacing the previous implementation of the same contract.
    /// </summary>
    /// <typeparam name="TService">The contract the service is published under.</typeparam>
    /// <param name="service">The implementation.</param>
    void Set<TService>(TService service)
        where TService : class;

    /// <summary>
    /// Finds a service by its contract.
    /// </summary>
    /// <typeparam name="TService">The contract to look for.</typeparam>
    /// <returns>The implementation, or <c>null</c> if nothing is published.</returns>
    TService? Find<TService>()
        where TService : class;

    /// <summary>
    /// Gets a service by its contract.
    /// </summary>
    /// <typeparam name="TService">The contract to look for.</typeparam>
    /// <returns>The implementation.</returns>
    /// <exception cref="System.InvalidOperationException">Thrown if nothing is published for the contract.</exception>
    TService Get<TService>()
        where TService : class;

    /// <summary>
    /// Removes a published service.
    /// </summary>
    /// <typeparam name="TService">The contract to remove.</typeparam>
    /// <returns><c>true</c> if a service was removed; otherwise, <c>false</c>.</returns>
    bool Remove<TService>()
        where TService : class;
}
