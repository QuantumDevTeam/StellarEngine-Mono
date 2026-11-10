namespace Stellar.Runtime.Threading;

/// <summary>
/// Brings <c>await</c> on the main thread back to the main thread, through its dispatcher.
/// </summary>
/// <param name="dispatcher">The dispatcher of the main thread.</param>
public sealed class MainThreadSynchronizationContext(MainThreadDispatcher dispatcher)
    : SynchronizationContext
{
    /// <inheritdoc/>
    public override void Post(SendOrPostCallback callback, object? state)
    {
        dispatcher.Post(() => callback(state));
    }

    /// <inheritdoc/>
    public override void Send(SendOrPostCallback callback, object? state)
    {
        if (dispatcher.IsMainThread)
            callback(state);
        else
            dispatcher.InvokeAsync(() => callback(state)).GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    public override SynchronizationContext CreateCopy()
    {
        return this;
    }
}
