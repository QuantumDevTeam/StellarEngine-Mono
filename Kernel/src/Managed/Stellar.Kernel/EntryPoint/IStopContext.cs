using Stellar.Kernel.Data.Context;

namespace Stellar.Kernel.EntryPoint;

/// <summary>
/// Context passed to <see cref="StellarEntryPoint.RequestStop{TContext}"/>.
/// </summary>
public interface IStopContext
    : IContext
{
    /// <summary>
    /// Gets the reason why the stop was requested.
    /// </summary>
    StopReason Reason { get; }
}
