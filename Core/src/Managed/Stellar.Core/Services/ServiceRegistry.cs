using System.Collections.Concurrent;
using Stellar.Kernel.Services;

namespace Stellar.Core.Services;

/// <inheritdoc/>
public sealed class ServiceRegistry
    : IServiceRegistry
{
    private readonly ConcurrentDictionary<Type, object> _services = new();

    /// <inheritdoc/>
    public void Set<TService>(TService service)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(service);
        _services[typeof(TService)] = service;
    }

    /// <inheritdoc/>
    public TService? Find<TService>()
        where TService : class
    {
        return _services.GetValueOrDefault(typeof(TService)) as TService;
    }

    /// <inheritdoc/>
    public TService Get<TService>()
        where TService : class
    {
        return Find<TService>() ??
               throw new InvalidOperationException($"Service {typeof(TService).Name} is not published");
    }

    /// <inheritdoc/>
    public bool Remove<TService>()
        where TService : class
    {
        return _services.TryRemove(typeof(TService), out _);
    }
}
