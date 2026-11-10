namespace Stellar.Core.Failures.BaseLevels;

/// <summary>
/// A failure that is logged and does not stop execution.
/// </summary>
/// <param name="dispatcherMeta">The meta of the dispatcher the level belongs to.</param>
public class Warning(IFailureDispatcherMeta dispatcherMeta)
    : FailureLevel("S.C/Warning", dispatcherMeta)
{
    /// <inheritdoc/>
    public override bool IsEnabled { get; set; } = true;

    /// <inheritdoc/>
    public override bool IsLoggable { get; } = true;

    /// <inheritdoc/>
    public override bool IsStopExecute { get; } = false;

    /// <inheritdoc/>
    public override bool IsCritical { get; } = false;

    /// <inheritdoc/>
    public override bool ShouldTerminate { get; } = false;
}
