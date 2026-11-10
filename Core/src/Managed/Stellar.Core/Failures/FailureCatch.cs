using System.Runtime.ExceptionServices;
using Stellar.Kernel;
using Stellar.Kernel.Failures;
using Stellar.Kernel.Quantization;

namespace Stellar.Core.Failures;

/// <inheritdoc cref="IFailureCatch"/>
/// <param name="dispatcher">
/// Receives the exceptions no <see cref="Except{T}"/> block claimed. Without it they are rethrown.
/// </param>
/// <param name="level">The level of the failures reported to the dispatcher.</param>
/// <param name="type">The type of the failures reported to the dispatcher.</param>
/// <param name="sender">The quant reported as the sender of the failures.</param>
public sealed class FailureCatch(
    IFailureDispatcher? dispatcher = null,
    IFailureLevel? level = null,
    FailureType type = FailureType.Unknown,
    IQuantumObject? sender = null
) : IFailureCatch
{
    private readonly List<(Type Exception, Action<IFailureCatch, Exception> Handler)> _handlers = [];
    private Action<IFailureCatch>? _try;
    private Action<IFailureCatch>? _finally;

    /// <inheritdoc/>
    public IIdentifier UID { get; } = Identifier.Create();

    /// <inheritdoc/>
    public Exception? Caught { get; private set; }

    /// <inheritdoc/>
    public IFailureCatch Try(Action<IFailureCatch> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        _try = action;
        return this;
    }

    /// <inheritdoc/>
    public IFailureCatch Except<T>(Action<IFailureCatch, T> action)
        where T : Exception
    {
        ArgumentNullException.ThrowIfNull(action);

        _handlers.Add((typeof(T), (context, exception) => action(context, (T)exception)));
        return this;
    }

    /// <inheritdoc/>
    public IFailureCatch Finally(Action<IFailureCatch> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        _finally = action;
        return this;
    }

    /// <inheritdoc/>
    public IFailureCatch Handle()
    {
        Caught = null;

        try
        {
            _try?.Invoke(this);
        }
        catch (Exception exception)
        {
            Caught = exception;
            HandleException(exception);
        }
        finally
        {
            _finally?.Invoke(this);
        }

        return this;
    }

    private void HandleException(Exception exception)
    {
        foreach (var (exceptionType, handler) in _handlers)
        {
            if (!exceptionType.IsInstanceOfType(exception)) continue;

            handler(this, exception);
            return;
        }

        if (dispatcher is null || level is null)
            ExceptionDispatchInfo.Capture(exception).Throw();

        var failure = new Failure
        {
            Message = exception.Message,
            InnerException = exception,
            Type = type,
            Level = level,
            Source = exception.Source ?? string.Empty
        };

        var context = new FailureContext(sender, failure);
        dispatcher.Dispatch(in context);
    }
}
