namespace Stellar.Core.Failures.BaseLevels;

/// <summary>
/// A failure that is neither logged nor stops execution.
/// </summary>
/// <param name="dispatcherMeta">The meta of the dispatcher the level belongs to.</param>
public class NonCritical(IFailureDispatcherMeta dispatcherMeta)
    : FailureLevel("S.C/NonCritical", dispatcherMeta)
{
    /// <inheritdoc/>
    public override bool IsEnabled { get; set; } = true;

    /// <inheritdoc/>
    public override bool IsLoggable { get; } = false;

    /// <inheritdoc/>
    public override bool IsStopExecute { get; } = false;

    /// <inheritdoc/>
    public override bool IsCritical { get; } = false;

    /// <inheritdoc/>
    public override bool ShouldTerminate { get; } = false;
}
