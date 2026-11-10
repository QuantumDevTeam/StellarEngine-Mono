using Stellar.Kernel.EntryPoint;
using Stellar.Kernel.Failures;
using Stellar.Kernel.Lifecycle;

namespace Stellar.Runtime.Lifecycle;

/// <summary>
/// The state of the engine, and the way to stop it from anywhere.
/// </summary>
/// <remarks>Safe to use from any thread. The first stop request wins: its reason is the reason of the stop.</remarks>
public sealed class EngineControl
    : IEngineControl, IDisposable
{
    private readonly CancellationTokenSource _stop = new();
    private int _state = (int)EngineState.Created;
    private int _stopRequested;

    /// <inheritdoc/>
    public EngineState State => (EngineState)Volatile.Read(ref _state);

    /// <summary>
    /// Gets whether a stop was requested.
    /// </summary>
    public bool IsStopRequested => Volatile.Read(ref _stopRequested) != 0;

    /// <summary>
    /// Gets a token cancelled when a stop is requested.
    /// </summary>
    public CancellationToken StopToken => _stop.Token;

    /// <summary>
    /// Gets the reason of the stop, once requested.
    /// </summary>
    public StopReason StopReason { get; private set; } = StopReason.Unknown;

    /// <summary>
    /// Gets the failure that caused the stop, if any.
    /// </summary>
    public IFailure? StopFailure { get; private set; }

    /// <summary>
    /// Gets the exception that caused the stop, if the runtime caught one.
    /// </summary>
    public Exception? StopException { get; private set; }

    /// <inheritdoc/>
    public void RequestStop(StopReason reason, IFailure? failure = null)
    {
        Stop(reason, failure, null);
    }

    /// <summary>
    /// Requests a stop because of an exception the runtime caught.
    /// </summary>
    /// <param name="exception">The exception.</param>
    public void RequestStop(Exception exception)
    {
        Stop(StopReason.CriticalError, null, exception);
    }

    /// <summary>
    /// Moves the engine to a state.
    /// </summary>
    /// <param name="state">The state.</param>
    public void SetState(EngineState state)
    {
        Volatile.Write(ref _state, (int)state);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _stop.Dispose();
    }

    private void Stop(StopReason reason, IFailure? failure, Exception? exception)
    {
        if (Interlocked.Exchange(ref _stopRequested, 1) != 0) return;

        StopReason = reason;
        StopFailure = failure;
        StopException = exception;
        _stop.Cancel();
    }
}
