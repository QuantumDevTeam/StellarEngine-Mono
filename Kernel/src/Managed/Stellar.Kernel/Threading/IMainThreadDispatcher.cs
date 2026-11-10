using System;
using System.Threading.Tasks;
using Stellar.Kernel.Quantization;

namespace Stellar.Kernel.Threading;

/// <summary>
/// Runs work on the main thread of the engine.
/// </summary>
/// <remarks>
/// The engine starts, runs its frames and stops on one thread. Work posted from other threads runs there between the
/// steps of the startup and at the start of every frame, and <c>await</c> on the main thread comes back to it.
/// </remarks>
public interface IMainThreadDispatcher
    : IQuantumObject
{
    /// <summary>
    /// Gets whether the calling thread is the main thread.
    /// </summary>
    bool IsMainThread { get; }

    /// <summary>
    /// Queues work for the main thread.
    /// </summary>
    /// <param name="action">The work.</param>
    void Post(Action action);

    /// <summary>
    /// Runs work on the main thread: at once if called from it, queued otherwise.
    /// </summary>
    /// <param name="action">The work.</param>
    /// <returns>A task that completes when the work has run.</returns>
    Task InvokeAsync(Action action);
}
