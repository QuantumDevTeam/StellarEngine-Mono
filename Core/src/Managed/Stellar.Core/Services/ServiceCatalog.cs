using Stellar.Core.Data.Catalog;
using Stellar.Kernel.Services;

namespace Stellar.Core.Services;

/// <inheritdoc cref="IServiceCatalog"/>
public sealed class ServiceCatalog
    : Catalog<Type, object>, IServiceCatalog
{
    /// <inheritdoc/>
    protected override void Validate(Type key, object value)
    {
        base.Validate(key, value);
        if (!key.IsInstanceOfType(value))
            throw new ArgumentException($"{value.GetType().Name} does not fulfil {key.Name}: it cannot be published as one", nameof(value));
    }

    /// <inheritdoc/>
    protected override Exception NotFound(Type key)
    {
        return new InvalidOperationException($"Service {key.Name} is not published");
    }
}
