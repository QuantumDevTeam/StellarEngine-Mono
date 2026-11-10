using System.Collections.Concurrent;
using Stellar.Core.Data.Collections;
using Stellar.Core.Quantization;
using Stellar.Kernel;
using Stellar.Kernel.Failures;
using Stellar.Kernel.Failures.Handlers;

namespace Stellar.Core.Failures.Handlers;

/// <summary>
/// Which failures a handler is bound to.
/// </summary>
/// <param name="Type">
/// The flags a failure must have. <see cref="FailureType.Unknown"/> matches every failure.
/// </param>
/// <param name="Level">The level a failure must have. <c>null</c> matches every level.</param>
public readonly record struct FailureBinding(FailureType Type, IFailureLevel? Level)
{
    /// <summary>
    /// Checks whether a failure falls under this binding.
    /// </summary>
    public bool Matches(IFailure failure)
    {
        return failure.Type.HasFlag(Type) && (Level is null || ReferenceEquals(failure.Level, Level));
    }
}

/// <summary>
/// Meta of a handler provider: the handlers and what they are bound to.
/// </summary>
/// <param name="handlers">The container of handlers. A new one is created when omitted.</param>
public class HandlerProviderMeta(DataContainer<IFailureHandler>? handlers = null)
    : MetaQuant(Identifier.CreateAndRegister())
{
    /// <summary>
    /// Gets the handlers.
    /// </summary>
    public DataContainer<IFailureHandler> Handlers { get; } = handlers ?? new WritableTable<IFailureHandler>();

    /// <summary>
    /// Gets the binding of each handler.
    /// </summary>
    public ConcurrentDictionary<IIdentifier, FailureBinding> Bindings { get; } = new(IdentifierComparer.Instance);
}

/// <inheritdoc cref="IFailureHandlerProvider"/>
/// <param name="meta">The meta with handlers and bindings.</param>
public class FailureHandlerProvider(HandlerProviderMeta meta)
    : RegistrableQuant<FailureHandlerProvider, HandlerProviderMeta>(meta), IFailureHandlerProvider
{
    /// <summary>
    /// Binds a handler to a kind of failures.
    /// </summary>
    /// <param name="binding">The failures to handle.</param>
    /// <param name="handler">The handler.</param>
    public void RegisterHandler(FailureBinding binding, IFailureHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        MetaQuant.Handlers.Set(handler);
        MetaQuant.Bindings[handler.UID] = binding;
    }

    /// <summary>
    /// Binds a handler to every failure.
    /// </summary>
    /// <param name="handler">The handler.</param>
    public void RegisterDefaultHandler(IFailureHandler handler)
    {
        RegisterHandler(new FailureBinding(FailureType.Unknown, null), handler);
    }

    /// <inheritdoc/>
    public IEnumerable<IFailureHandler> GetHandlers(IFailure failure)
    {
        ArgumentNullException.ThrowIfNull(failure);

        return MetaQuant.Bindings
            .Where(binding => binding.Value.Matches(failure))
            .Select(binding => MetaQuant.Handlers[binding.Key])
            .ToArray();
    }
}
