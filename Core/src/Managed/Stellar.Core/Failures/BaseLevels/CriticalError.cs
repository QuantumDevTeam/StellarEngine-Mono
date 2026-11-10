namespace Stellar.Core.Failures.BaseLevels;

/// <summary>
/// A failure that terminates the engine.
/// </summary>
/// <param name="dispatcherMeta">The meta of the dispatcher the level belongs to.</param>
public class CriticalError(IFailureDispatcherMeta dispatcherMeta)
    : FailureLevel("S.C/CriticalError", dispatcherMeta)
{
    /// <inheritdoc/>
    public override bool IsEnabled { get; set; } = true;

    /// <inheritdoc/>
    public override bool IsLoggable { get; } = true;

    /// <inheritdoc/>
    public override bool IsStopExecute { get; } = true;

    /// <inheritdoc/>
    public override bool IsCritical { get; } = true;

    /// <inheritdoc/>
    public override bool ShouldTerminate { get; } = true;
}
