using Stellar.Core.Data.Catalog;
using Stellar.Core.Data.Collections;
using Stellar.Core.Quantization;
using Stellar.Kernel.Data.Catalog;
using Stellar.Kernel.EntryPoint;
using Stellar.Kernel.Failures;
using Stellar.Kernel.Lifecycle;
using Stellar.Kernel.LoggingSystem;

namespace Stellar.Core.Failures;

/// <summary>
/// The failure system of one engine: reactions in catalogs, handlers that hear of failures, a log, a stop.
/// </summary>
/// <remarks>
/// Stellar.Core makes one per engine and publishes it in the services of the engine. The logger and the control of
/// the engine are looked up when a failure needs them, so they can come after the system.
/// </remarks>
public sealed class FailureSystem
    : Quant<MetaQuant>, IFailureSystem
{
    private sealed class Subscription(FailureSystem system, IFailureHandler handler, FailureFilter filter)
        : IDisposable
    {
        public IFailureHandler Handler { get; } = handler;

        public FailureFilter Filter { get; } = filter;

        public void Dispose()
        {
            system._handlers.Remove(this);
        }
    }

    private readonly Func<ILogger?> _logger;
    private readonly Func<IEngineControl?> _control;
    private readonly ConcurrentList<Subscription> _handlers = new();
    private readonly Catalog<FailureLevel, FailureReaction> _reactions = new();
    private readonly Catalog<(FailureKind Kind, FailureLevel Level), FailureReaction> _kindReactions = new();

    /// <inheritdoc/>
    public ICatalog<FailureLevel, FailureReaction> Reactions => _reactions;

    /// <inheritdoc/>
    public ICatalog<(FailureKind Kind, FailureLevel Level), FailureReaction> KindReactions => _kindReactions;

    /// <summary>
    /// Makes a failure system.
    /// </summary>
    /// <param name="logger">Gives the logger, when there is one.</param>
    /// <param name="control">Gives the control of the engine, when there is one.</param>
    public FailureSystem(Func<ILogger?>? logger = null, Func<IEngineControl?>? control = null)
        : base(new MetaQuant())
    {
        _logger = logger ?? (() => null);
        _control = control ?? (() => null);

        foreach (var level in Enum.GetValues<FailureLevel>())
            _reactions.Set(level, FailureReactions.Default(level));
    }

    private static LogLevel LogLevelOf(FailureLevel level)
    {
        return level switch
        {
            FailureLevel.NonCritical => LogLevel.Info,
            FailureLevel.Warning => LogLevel.Warning,
            FailureLevel.Error => LogLevel.Error,
            _ => LogLevel.Exception
        };
    }

    private static string Describe(Failure failure)
    {
        return failure.Exception is { } exception and not FailureException { InnerException: null }
            ? $"{failure}{Environment.NewLine}{exception}"
            : failure.ToString();
    }

    /// <inheritdoc/>
    public FailureReaction ReactionTo(Failure failure)
    {
        ArgumentNullException.ThrowIfNull(failure);

        if (_kindReactions.TryGet((failure.Kind, failure.Level), out var byKind)) return byKind;
        return _reactions.TryGet(failure.Level, out var byLevel)
            ? byLevel
            : FailureReactions.Default(failure.Level);
    }

    /// <inheritdoc/>
    public FailureReaction Report(Failure failure)
    {
        ArgumentNullException.ThrowIfNull(failure);

        var reaction = ReactionTo(failure);
        if (!failure.MarkReported(reaction)) return failure.Reaction!.Value;

        if ((reaction & FailureReaction.Log) != 0) _logger()?.Log(LogLevelOf(failure.Level), Describe(failure));

        foreach (var subscription in _handlers.AsSpan())
        {
            if (!subscription.Filter.Matches(failure)) continue;

            try
            {
                subscription.Handler.Handle(failure);
            }
            catch (Exception exception)
            {
                // A handler that fails is not a failure of the engine: it is told, and the others still hear
                _logger()?.Log(LogLevel.Error,
                    $"The failure handler {subscription.Handler.GetType().Name} failed: {exception}");
            }
        }

        if ((reaction & FailureReaction.StopEngine) != 0) _control()?.RequestStop(StopReason.CriticalError, failure);

        return reaction;
    }

    /// <inheritdoc/>
    public IDisposable AddHandler(IFailureHandler handler, FailureFilter filter = default)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var subscription = new Subscription(this, handler, filter);
        _handlers.Add(subscription);
        return subscription;
    }
}
