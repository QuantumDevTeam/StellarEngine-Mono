using System.Reflection;
using System.Runtime.ExceptionServices;
using Stellar.Kernel.Configuration;
using Stellar.Kernel.EntryPoint;
using Stellar.Kernel.Failures;
using Stellar.Kernel.Lifecycle;
using Stellar.Kernel.LoggingSystem;
using Stellar.Kernel.Services;
using Stellar.Runtime.Modules;

namespace Stellar.Runtime.Lifecycle;

/// <summary>
/// Starts and stops the entry point of a module.
/// </summary>
/// <remarks>
/// <para>A started entry point that is <see cref="IUpdatable"/>, <see cref="IFixedUpdatable"/> or
/// <see cref="IRenderable"/> joins the primary loop by itself, and leaves it when it stops. The modules of the engine
/// run before the application in every phase (<see cref="EngineOrder"/>).</para>
/// <para>The first entry point that owns the services of the engine (<see cref="IServiceHost"/>, Stellar.Core) gets
/// the services of the runtime before it runs, and from then on every entry point gets the services, the logger and
/// the failure dispatcher.</para>
/// </remarks>
public static class ModuleStarter
{
    /// <summary>
    /// Starts a module.
    /// </summary>
    /// <param name="context">The runtime.</param>
    /// <param name="module">The module.</param>
    /// <exception cref="ModuleLoadException">Thrown if the entry point returns a code other than 0.</exception>
    public static void Start(RuntimeContext context, LoadedModule module)
    {
        context.Log.Step($"Starting {module}");
        context.MarkStarted(module);

        if (module.EntryPointType is null) return;

        var entryPoint = Create(module.EntryPointType, context.Configuration ?? new RuntimeConfiguration());
        module.EntryPoint = entryPoint;

        if (entryPoint is IServiceHost host && context.Services is null)
        {
            context.Services = host.Services;
            RuntimeServices.Publish(context, host.Services);
        }

        var services = context.Services;
        var runContext = new RuntimeRunContext(context, services?.Find<ILogger>(),
            services?.Find<IFailureDispatcher>(), services, context.Engine);

        var code = entryPoint.Run(in runContext);

        // The module that owns the services may have brought a logger
        context.Log.Logger ??= context.Services?.Find<ILogger>();

        if (code != 0)
        {
            context.RequestedExitCode ??= code;
            throw new ModuleLoadException($"The module {module.Id} did not start: its entry point returned {code}");
        }

        if (context.MainLoop is { } mainLoop && TakesPartInFrames(entryPoint))
            mainLoop.Add(entryPoint, order: module.IsApplication ? ApplicationOrder : EngineOrder);
    }

    /// <summary>
    /// Stops a module. What fails while stopping is caught and logged, the module is stopped anyway.
    /// </summary>
    /// <param name="context">The runtime.</param>
    /// <param name="module">The module.</param>
    public static void Stop(RuntimeContext context, LoadedModule module)
    {
        context.Log.Step($"Stopping {module}");

        if (module.EntryPoint is { } entryPoint)
        {
            context.MainLoop?.Remove(entryPoint);

            var reason = context.Control.IsStopRequested ? context.Control.StopReason : StopReason.Regular;

            context.Catchers.Engine.Run(() =>
                {
                    var stopContext = new RuntimeStopContext(context, reason);
                    entryPoint.RequestStop(in stopContext);
                }, $"the stop of {module.Id}", FailureType.Module, context);

            context.Catchers.Engine.Run(entryPoint.Dispose, $"the disposal of {module.Id}", FailureType.Module,
                context);

            if (entryPoint is IServiceHost host && ReferenceEquals(host.Services, context.Services))
            {
                RuntimeServices.Withdraw(host.Services);
                context.Services = null;
                context.Log.Logger = null;
            }

            module.EntryPoint = null;
        }

        context.MarkStopped(module);
    }

    /// <summary>
    /// The order of the entry points of engine modules in the primary loop: before the application.
    /// </summary>
    public const int EngineOrder = -1000;

    /// <summary>
    /// The order of the entry point of the application in the primary loop.
    /// </summary>
    public const int ApplicationOrder = 0;

    private static bool TakesPartInFrames(StellarEntryPoint entryPoint)
    {
        return entryPoint is IUpdatable or IFixedUpdatable or IRenderable;
    }

    private static StellarEntryPoint Create(Type type, RuntimeConfiguration configuration)
    {
        try
        {
            return (StellarEntryPoint)Activator.CreateInstance(type, configuration)!;
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
        catch (MissingMethodException exception)
        {
            throw new ModuleLoadException(
                $"The entry point {type.FullName} needs a public constructor that takes a {nameof(RuntimeConfiguration)}",
                exception);
        }
    }
}
