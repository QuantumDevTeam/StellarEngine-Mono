using System;
using System.Threading;
using Stellar.Kernel.Lifecycle;
using Stellar.Kernel.Quantization;

namespace Stellar.Kernel.Failures;

/// <summary>
/// A failure of the engine: what went wrong, where, how seriously, and what was done about it.
/// </summary>
/// <remarks>
/// <para>Code of the engine works with failures, whatever went wrong: an exception of the engine
/// (<see cref="FailureException"/>) carries its failure, and any other exception is wrapped into one
/// (<see cref="From"/>), so a handler, a log or a game sees one shape.</para>
/// <para>A failure is reported once: the first failure system or catcher that reports it sets
/// <see cref="Reaction"/>, and everyone after that sees the same decision.</para>
/// </remarks>
public sealed class Failure
{
    // 0 not reported, 1 being marked, 2 reported
    private int _state;
    private FailureReaction _reaction;

    /// <summary>Gets the identifier of the failure.</summary>
    public Identifier UID { get; } = Identifier.New();

    /// <summary>Gets where it comes from.</summary>
    public FailureKind Kind { get; }

    /// <summary>Gets how serious it is.</summary>
    public FailureLevel Level { get; }

    /// <summary>Gets what went wrong.</summary>
    public string Message { get; }

    /// <summary>Gets the exception behind it, if any.</summary>
    public Exception? Exception { get; }

    /// <summary>Gets the object it happened in, if known.</summary>
    public IQuantumObject? Sender { get; }

    /// <summary>Gets the phase of the frame it happened in, if any.</summary>
    public FramePhase? Phase { get; }

    /// <summary>Gets when it happened.</summary>
    public DateTime TimeUtc { get; } = DateTime.UtcNow;

    /// <summary>Gets whether it was reported.</summary>
    public bool IsReported => Volatile.Read(ref _state) == 2;

    /// <summary>Gets what was done about it; <c>null</c> until it is reported.</summary>
    public FailureReaction? Reaction => IsReported ? _reaction : null;

    /// <summary>
    /// Makes a failure.
    /// </summary>
    /// <param name="kind">Where it comes from.</param>
    /// <param name="level">How serious it is.</param>
    /// <param name="message">What went wrong.</param>
    /// <param name="exception">The exception behind it, if any.</param>
    /// <param name="sender">The object it happened in, if known.</param>
    /// <param name="phase">The phase of the frame it happened in, if any.</param>
    public Failure(FailureKind kind, FailureLevel level, string message, Exception? exception = null,
        IQuantumObject? sender = null, FramePhase? phase = null)
    {
        Kind = kind;
        Level = level;
        Message = string.IsNullOrEmpty(message) ? exception?.Message ?? "A failure" : message;
        Exception = exception;
        Sender = sender;
        Phase = phase;
    }

    /// <summary>
    /// Makes the failure of an exception: the failure of an exception of the engine, a new one for any other.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <param name="kind">Where it comes from, for an exception that is not of the engine.</param>
    /// <param name="level">How serious it is, for an exception that is not of the engine.</param>
    /// <param name="sender">The object it happened in, if known.</param>
    /// <param name="operation">What was being done, put before the message; <c>null</c> for nothing.</param>
    /// <param name="phase">The phase of the frame, if any.</param>
    /// <returns>The failure.</returns>
    public static Failure From(Exception exception, FailureKind kind, FailureLevel level, IQuantumObject? sender = null,
        string? operation = null, FramePhase? phase = null)
    {
        if (exception is null) throw new ArgumentNullException(nameof(exception));
        if (exception is FailureException engine) return engine.Failure;

        var message = string.IsNullOrEmpty(operation) ? exception.Message : $"{operation}: {exception.Message}";
        return new Failure(kind, level, message, exception, sender, phase);
    }

    /// <summary>
    /// Records what was done about the failure: called once, by whoever reports it first.
    /// </summary>
    /// <param name="reaction">The reaction.</param>
    /// <returns><c>true</c> if this call reported it; <c>false</c> if it was reported already.</returns>
    public bool MarkReported(FailureReaction reaction)
    {
        if (Interlocked.CompareExchange(ref _state, 1, 0) != 0)
        {
            // Another thread is marking it: its reaction is the one, and it is there in a moment
            var spin = new SpinWait();
            while (Volatile.Read(ref _state) != 2) spin.SpinOnce();
            return false;
        }

        _reaction = reaction;
        Volatile.Write(ref _state, 2);
        return true;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{Level} ({Kind}): {Message}";
    }
}
