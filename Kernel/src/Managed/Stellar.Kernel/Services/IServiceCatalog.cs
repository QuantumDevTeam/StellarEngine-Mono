using System;
using Stellar.Kernel.Data.Catalog;

namespace Stellar.Kernel.Services;

/// <summary>
/// The services of an engine, by their contract.
/// </summary>
/// <remarks>
/// <para>Modules publish implementations of Kernel contracts here during startup and other modules find them without a
/// build-time dependency on each other. This is what keeps level 2 modules independent.</para>
/// <para>A catalog keyed by the contract: a service set under a contract fulfils it, and the catalog refuses one that
/// does not. <see cref="ServiceCatalogExtensions"/> are the same operations, typed: <c>Set&lt;T&gt;</c>,
/// <c>Find&lt;T&gt;</c>, <c>Get&lt;T&gt;</c>, <c>Remove&lt;T&gt;</c>.</para>
/// </remarks>
public interface IServiceCatalog
    : ICatalog<Type, object>;

/// <summary>
/// The operations of a service catalog, by the contract as a type parameter.
/// </summary>
public static class ServiceCatalogExtensions
{
    /// <summary>
    /// Publishes a service, replacing the previous implementation of the same contract.
    /// </summary>
    /// <typeparam name="TService">The contract the service is published under.</typeparam>
    /// <param name="services">The catalog.</param>
    /// <param name="service">The implementation.</param>
    public static void Set<TService>(this IServiceCatalog services, TService service)
        where TService : class
    {
        if (service is null) throw new ArgumentNullException(nameof(service));
        services.Set(typeof(TService), service);
    }

    /// <summary>
    /// Finds a service by its contract.
    /// </summary>
    /// <typeparam name="TService">The contract to look for.</typeparam>
    /// <param name="services">The catalog.</param>
    /// <returns>The implementation, or <c>null</c> if nothing is published.</returns>
    public static TService? Find<TService>(this IServiceCatalog services)
        where TService : class
    {
        return services.TryGet(typeof(TService), out var service) ? service as TService : null;
    }

    /// <summary>
    /// Gets a service by its contract.
    /// </summary>
    /// <typeparam name="TService">The contract to look for.</typeparam>
    /// <param name="services">The catalog.</param>
    /// <returns>The implementation.</returns>
    /// <exception cref="InvalidOperationException">Thrown if nothing is published for the contract.</exception>
    public static TService Get<TService>(this IServiceCatalog services)
        where TService : class
    {
        return services.Find<TService>() ??
               throw new InvalidOperationException($"Service {typeof(TService).Name} is not published");
    }

    /// <summary>
    /// Removes a published service.
    /// </summary>
    /// <typeparam name="TService">The contract to remove.</typeparam>
    /// <param name="services">The catalog.</param>
    /// <returns><c>true</c> if a service was removed.</returns>
    public static bool Remove<TService>(this IServiceCatalog services)
        where TService : class
    {
        return services.Remove(typeof(TService));
    }
}
