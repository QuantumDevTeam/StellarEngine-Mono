using System.Runtime.Loader;
using Stellar.Kernel.Configuration;
using Stellar.Kernel.EntryPoint;
using Stellar.Kernel.Lifecycle;
using Stellar.Kernel.Threading;
using Stellar.Runtime.Tests.Host;

namespace Stellar.Runtime.Tests.Application;

/// <summary>
/// Runs three frames and stops the engine. What it does is set by the test through the data of the process.
/// </summary>
public sealed class Game(RuntimeConfiguration configuration)
    : StellarEntryPoint("Tests/Game", configuration), IUpdatable, IRenderable
{
    public const string BehaviorKey = "Stellar.Runtime.Tests.Behavior";

    private IEngineControl? _engine;
    private int _frames;

    private static string Behavior => AppContext.GetData(BehaviorKey) as string ?? string.Empty;

    public override int Run<TContext>(ref readonly TContext context)
    {
        _engine = context.Services?.Find<IEngineControl>();

        Events.Add($"app:run context={AssemblyLoadContext.GetLoadContext(GetType().Assembly)?.Name} " +
                   $"host={AssemblyLoadContext.GetLoadContext(typeof(Events).Assembly)?.Name} " +
                   $"loop={context.Services?.Find<IMainLoop>() is not null} " +
                   $"main={context.Services?.Find<IMainThreadDispatcher>()?.IsMainThread} " +
                   $"engine={context.Engine?.Name} clock={context.Engine?.Time.Engine.Name}");

        return Behavior switch
        {
            "fail-run" => 3,
            "throw-run" => throw new InvalidOperationException("the game cannot start"),
            _ => 0
        };
    }

    public void Update<TContext>(ref readonly TContext context)
        where TContext : IFrameContext, allows ref struct
    {
        _frames++;
        Events.Add($"app:update {context.FrameIndex}");

        if (Behavior == "throw-update" && _frames == 1)
            throw new InvalidOperationException("a bad frame");

        if (_frames == 3)
            _engine?.RequestStop(StopReason.Regular);
    }

    public void Render<TContext>(ref readonly TContext context)
        where TContext : IFrameContext, allows ref struct
    {
        Events.Add($"app:render {context.FrameIndex}");
    }

    public override void RequestStop<TContext>(ref readonly TContext context)
    {
        Events.Add($"app:stop {context.Reason}");
    }

    public override void Dispose()
    {
        Events.Add("app:dispose");
    }
}
