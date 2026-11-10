using Stellar.Runtime.Failures;

namespace Stellar.Runtime.Threading;

/// <summary>
/// Runs asynchronous work to its end on the main thread: the thread waits for it by running what is posted to it,
/// so every continuation of the work comes back there.
/// </summary>
public static class MainThreadPump
{
    private static readonly TimeSpan Idle = TimeSpan.FromMilliseconds(50);

    /// <summary>
    /// Runs the work.
    /// </summary>
    /// <param name="dispatcher">The dispatcher of the calling thread.</param>
    /// <param name="catcher">Catches what posted work throws.</param>
    /// <param name="work">The work.</param>
    /// <typeparam name="T">The result.</typeparam>
    /// <returns>The result of the work.</returns>
    public static T Run<T>(MainThreadDispatcher dispatcher, RuntimeCatcher catcher, Func<Task<T>> work)
    {
        var previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(new MainThreadSynchronizationContext(dispatcher));

        try
        {
            var task = work();

            while (!task.IsCompleted)
            {
                dispatcher.Drain(catcher);

                if (!task.IsCompleted)
                    dispatcher.WaitForWork(Idle);
            }

            dispatcher.Drain(catcher);
            return task.GetAwaiter().GetResult();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }
}
