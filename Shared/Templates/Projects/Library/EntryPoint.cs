using Stellar.Kernel.Configuration;
using Stellar.Kernel.EntryPoint;

namespace {{ProjectName}};

[StellarEntryPoint]
public sealed class EntryPoint(RuntimeConfiguration configuration)
    : StellarEntryPoint("{{ProjectName}}", configuration)
{
    public override int Run<TContext>(ref readonly TContext context) => 0;

    public override void RequestStop<TContext>(ref readonly TContext context)
    {
    }

    public override void Dispose()
    {
    }
}
