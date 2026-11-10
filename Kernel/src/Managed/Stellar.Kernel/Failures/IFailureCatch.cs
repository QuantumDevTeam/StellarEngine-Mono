using System;
using Stellar.Kernel.Quantization;

namespace Stellar.Kernel.Failures;

/// <summary>
/// Provides a fluent API for catching, handling, and recovering from failures.
/// </summary>
/// <remarks>
/// <para>This interface mimics the familiar try/catch/finally pattern but integrated with the Stellar failure
/// system: an exception no <see cref="Except{T}"/> block claimed goes to the <see cref="IFailureDispatcher"/>.</para>
/// <para>Nothing runs until <see cref="Handle"/> is called. The methods return the same instance to enable
/// chaining.</para>
/// </remarks>
/// <example>
/// Typical usage:
/// <code>
/// failureCatch
///     .Try(c => SomeRiskyOperation())
///     .Except&lt;IOException&gt;((c, ex) => Console.WriteLine("IO error"))
///     .Except&lt;TimeoutException&gt;((c, ex) => Console.WriteLine("Timeout"))
///     .Finally(c => Cleanup())
///     .Handle();
/// </code>
/// Usage in try-catch:
/// <code>
/// try (Catch catch = new())
/// {
///    c => SomeRiskyOperation();
/// }
/// except (IOException e) // TODO: доделать пример
/// </code>
/// </example>
public interface IFailureCatch
    : IIdentifiableQuantumObject
{
    /// <summary>
    /// Gets the exception that the last <see cref="Handle"/> call caught, or <c>null</c> if there was none.
    /// </summary>
    Exception? Caught { get; }

    /// <summary>
    /// Sets the action to attempt.
    /// </summary>
    /// <param name="action">The action to attempt, receiving this catch context.</param>
    /// <returns>The same catch context for chaining.</returns>
    IFailureCatch Try(Action<IFailureCatch> action);

    /// <summary>
    /// Adds an exception handler for a specific exception type. Handlers are tested in the order they were added.
    /// </summary>
    /// <typeparam name="T">The type of exception to catch.</typeparam>
    /// <param name="action">The action to execute, receiving the context and the exception.</param>
    /// <returns>The same catch context for chaining.</returns>
    IFailureCatch Except<T>(Action<IFailureCatch, T> action)
        where T : Exception;

    /// <summary>
    /// Adds a finally block that executes regardless of whether an exception occurred.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <returns>The same catch context for chaining.</returns>
    IFailureCatch Finally(Action<IFailureCatch> action);

    /// <summary>
    /// Runs the registered try/except/finally blocks.
    /// </summary>
    /// <returns>The same catch context after handling.</returns>
    IFailureCatch Handle();
}
