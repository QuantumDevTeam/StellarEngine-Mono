using System;

namespace Stellar.Kernel.Failures;

/// <summary>
/// An exception of the engine: it carries its <see cref="Failure"/>.
/// </summary>
/// <remarks>
/// Errors of the engine derive from it and say where they come from and how serious they are. A catcher throws it
/// on when the reaction to a failure stops the work: a foreign exception travels inside, as
/// <see cref="Exception.InnerException"/>.
/// </remarks>
public class FailureException
    : Exception
{
    /// <summary>
    /// Gets the failure.
    /// </summary>
    public Failure Failure { get; }

    /// <summary>
    /// Makes an exception of the engine, with a new failure.
    /// </summary>
    /// <param name="kind">Where it comes from.</param>
    /// <param name="level">How serious it is.</param>
    /// <param name="message">What went wrong.</param>
    /// <param name="innerException">The exception behind it, if any.</param>
    public FailureException(FailureKind kind, FailureLevel level, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Failure = new Failure(kind, level, message, this);
    }

    /// <summary>
    /// Makes the exception of a failure, to throw it on.
    /// </summary>
    /// <param name="failure">The failure.</param>
    public FailureException(Failure failure)
        : base((failure ?? throw new ArgumentNullException(nameof(failure))).Message, failure.Exception)
    {
        Failure = failure;
    }
}
