using System.Collections.Concurrent;
using Stellar.Kernel.Threading;
using Stellar.Runtime.Failures;

namespace Stellar.Runtime.Threading;

/// <summary>
/// The queue of work for one thread: the one that created it, or the one it was bound to (<see cref="Bind"/>).
/// </summary>
public sealed class MainThreadDispatcher
    : IMainThreadDispatcher, IDisposable
{
    private readonly ConcurrentQueue<Action> _queue = new();
    private readonly AutoResetEvent _signal = new(false);
    private volatile int _threadId = Environment.CurrentManagedThreadId;

    /// <inheritdoc/>
    public bool IsMainThread => Environment.CurrentManagedThreadId == _threadId;

    /// <summary>
    /// Makes the calling thread the one this dispatcher runs work on: for a loop on a thread of its own, created
    /// before its thread.
    /// </summary>
    public void Bind()
    {
        _threadId = Environment.CurrentManagedThreadId;
    }

    /// <summary>
    /// Gets whether work is waiting.
    /// </summary>
    public bool HasWork => !_queue.IsEmpty;

    /// <inheritdoc/>
    public void Post(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        _queue.Enqueue(action);

        try
        {
            _signal.Set();
        }
        catch (ObjectDisposedException)
        {
            // The engine is gone: nobody will run the work, and a late poster must not crash the process
        }
    }

    /// <inheritdoc/>
    public Task InvokeAsync(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (IsMainThread)
            try
            {
                action();
                return Task.CompletedTask;
            }
            catch (Exception exception)
            {
                return Task.FromException(exception);
            }

        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        Post(() =>
        {
            try
            {
                action();
                completion.SetResult();
            }
            catch (Exception exception)
            {
                completion.SetException(exception);
            }
        });

        return completion.Task;
    }

    /// <summary>
    /// Runs the work that is waiting. Work posted meanwhile waits for the next call.
    /// </summary>
    /// <param name="catcher">Catches what the work throws.</param>
    /// <returns><c>true</c> if the engine must stop because of something the work threw.</returns>
    /// <exception cref="InvalidOperationException">Thrown if called from another thread.</exception>
    public bool Drain(RuntimeCatcher catcher)
    {
        if (!IsMainThread)
            throw new InvalidOperationException("The work of the main thread runs on the main thread only");

        var mustStop = false;

        for (var count = _queue.Count; count > 0 && _queue.TryDequeue(out var action); count--)
            mustStop |= catcher.Run(action, "work posted to the main thread").MustStop;

        return mustStop;
    }

    /// <summary>
    /// Waits until work is posted.
    /// </summary>
    /// <param name="timeout">The longest wait.</param>
    /// <returns><c>true</c> if work was posted.</returns>
    public bool WaitForWork(TimeSpan timeout)
    {
        return HasWork || _signal.WaitOne(timeout);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _signal.Dispose();
    }
}
