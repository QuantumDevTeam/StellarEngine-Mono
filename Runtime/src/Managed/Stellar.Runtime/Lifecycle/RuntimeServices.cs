using Stellar.Kernel.Lifecycle;
using Stellar.Kernel.Services;
using Stellar.Kernel.Threading;
using Stellar.Kernel.Timing;

namespace Stellar.Runtime.Lifecycle;

/// <summary>
/// The services the runtime itself provides to the engine: the engine instance, its control, its main thread, its
/// clocks, its loops and the primary one. Published in the registry of the module that owns the services, before that module runs.
/// </summary>
public static class RuntimeServices
{
    /// <summary>
    /// Publishes the services of the runtime.
    /// </summary>
    /// <param name="context">The runtime.</param>
    /// <param name="services">The registry.</param>
    public static void Publish(RuntimeContext context, IServiceRegistry services)
    {
        services.Set<IEngineControl>(context.Control);
        services.Set<IMainThreadDispatcher>(context.Dispatcher);
        services.Set<ITimeSystem>(context.Time);

        if (context.Engine is { } engine) services.Set<IEngineInstance>(engine);
        if (context.Loops is { } loops) services.Set<IMainLoops>(loops);
        if (context.MainLoop is { } mainLoop) services.Set<IMainLoop>(mainLoop);
    }

    /// <summary>
    /// Withdraws the services of the runtime.
    /// </summary>
    /// <param name="services">The registry.</param>
    public static void Withdraw(IServiceRegistry services)
    {
        services.Remove<IMainLoop>();
        services.Remove<IMainLoops>();
        services.Remove<IEngineInstance>();
        services.Remove<ITimeSystem>();
        services.Remove<IMainThreadDispatcher>();
        services.Remove<IEngineControl>();
    }
}
