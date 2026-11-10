using Stellar.Kernel.Configuration;
using Stellar.Kernel.EntryPoint;
using Stellar.Kernel.Lifecycle;

namespace {{ProjectName}};

/// <summary>
/// The game. The runtime starts it once the engine is up, then runs the main loop: as an <see cref="IUpdatable"/>
/// it takes part in every frame. Ctrl+C, or <see cref="IEngineControl.RequestStop"/>, stops the engine.
/// </summary>
[StellarEntryPoint]
public sealed class EntryPoint(RuntimeConfiguration configuration)
    : StellarEntryPoint("{{ProjectName}}", configuration), IUpdatable
{
    public override int Run<TContext>(ref readonly TContext context)
    {
        Console.WriteLine($"{RuntimeConfiguration.ProjectName} is running on StellarEngine {RuntimeConfiguration.StellarEngineVersion}");
        return 0;
    }

    public void Update<TContext>(ref readonly TContext context)
        where TContext : IFrameContext, allows ref struct
    {
    }

    public override void RequestStop<TContext>(ref readonly TContext context)
    {
    }

    public override void Dispose()
    {
    }
}
