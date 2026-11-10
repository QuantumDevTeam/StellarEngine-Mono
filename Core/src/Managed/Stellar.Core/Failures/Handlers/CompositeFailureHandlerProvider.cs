using Stellar.Core.Quantization;
using Stellar.Kernel.Failures;
using Stellar.Kernel.Failures.Handlers;

namespace Stellar.Core.Failures.Handlers;

/// <summary>
/// Joins several providers into one.
/// </summary>
public class CompositeFailureHandlerProvider()
    : RegistrableQuant<CompositeFailureHandlerProvider, HandlerProviderMeta>(new HandlerProviderMeta()),
        IFailureHandlerProvider
{
    private volatile IFailureHandlerProvider[] _providers = [];
    private readonly Lock _lock = new();

    /// <summary>
    /// Adds a provider.
    /// </summary>
    public void AddProvider(IFailureHandlerProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        lock (_lock)
        {
            _providers = [.. _providers, provider];
        }
    }

    /// <summary>
    /// Removes a provider.
    /// </summary>
    public void RemoveProvider(IFailureHandlerProvider provider)
    {
        lock (_lock)
        {
            _providers = _providers.Where(p => !ReferenceEquals(p, provider)).ToArray();
        }
    }

    /// <inheritdoc/>
    public IEnumerable<IFailureHandler> GetHandlers(IFailure failure)
    {
        return _providers.SelectMany(provider => provider.GetHandlers(failure));
    }
}
