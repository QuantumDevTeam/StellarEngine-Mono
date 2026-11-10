using Stellar.Kernel.Failures;
using Stellar.Kernel.Quantization;
using Stellar.Runtime.Diagnostics;

namespace Stellar.Runtime.Failures;

/// <summary>
/// Catches at one <see cref="CatchLevel"/>.
/// </summary>
/// <remarks>
/// <para>Before Stellar.Core is up there is nothing but <c>try</c>/<c>catch</c> and the error output. Once Stellar.Core
/// publishes an <see cref="IFailureReporter"/>, what is caught goes through the failure system, which decides whether
/// the engine stops; a fatal level stops it whatever the failure system says.</para>
/// <para>Safe to use from any thread.</para>
/// </remarks>
public sealed class RuntimeCatcher
{
    private readonly RuntimeLog _log;
    private readonly Func<IFailureReporter?> _reporter;
    private readonly Func<bool> _engineLogs;

    /// <summary>
    /// Initializes a new instance of the <see cref="RuntimeCatcher"/> class.
    /// </summary>
    /// <param name="level">The level.</param>
    /// <param name="log">The log of the runtime.</param>
    /// <param name="reporter">The reporter of the failure system, once there is one.</param>
    /// <param name="engineLogs">Whether the failure system logs what it is given (it has a logger).</param>
    public RuntimeCatcher(CatchLevel level, RuntimeLog log, Func<IFailureReporter?>? reporter = null,
        Func<bool>? engineLogs = null)
    {
        Level = level;
        _log = log;
        _reporter = reporter ?? (() => null);
        _engineLogs = engineLogs ?? (() => false);
    }

    /// <summary>
    /// Gets the level.
    /// </summary>
    public CatchLevel Level { get; }

    /// <summary>
    /// Gets whether whatever is caught at this level stops the engine.
    /// </summary>
    public bool IsFatal => Level != CatchLevel.MainLoop;

    /// <summary>
    /// Runs an action.
    /// </summary>
    /// <param name="action">The action.</param>
    /// <param name="what">What the action is, for the message.</param>
    /// <param name="type">What fails if it throws.</param>
    /// <param name="sender">Who runs it.</param>
    /// <returns>What came of it.</returns>
    public CatchResult Run(Action action, string what, FailureType type = FailureType.Engine,
        IQuantumObject? sender = null)
    {
        try
        {
            action();
            return CatchResult.Success;
        }
        catch (Exception exception)
        {
            return Handle(exception, what, type, sender);
        }
    }

    /// <summary>
    /// Runs an asynchronous action.
    /// </summary>
    /// <param name="action">The action.</param>
    /// <param name="what">What the action is, for the message.</param>
    /// <param name="type">What fails if it throws.</param>
    /// <param name="sender">Who runs it.</param>
    /// <returns>What came of it.</returns>
    public async ValueTask<CatchResult> RunAsync(Func<ValueTask> action, string what,
        FailureType type = FailureType.Engine, IQuantumObject? sender = null)
    {
        try
        {
            await action();
            return CatchResult.Success;
        }
        catch (Exception exception)
        {
            return Handle(exception, what, type, sender);
        }
    }

    /// <summary>
    /// Handles an exception caught elsewhere, as this level does.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <param name="what">What failed, for the message.</param>
    /// <param name="type">What failed.</param>
    /// <param name="sender">Who caught it.</param>
    /// <returns>What came of it.</returns>
    public CatchResult Handle(Exception exception, string what, FailureType type = FailureType.Engine,
        IQuantumObject? sender = null)
    {
        var mustStop = IsFatal;
        var reported = false;

        if (_reporter() is { } reporter)
            try
            {
                mustStop |= reporter.Report(exception, type, IsFatal, sender);
                reported = _engineLogs();
            }
            catch (Exception reporterException)
            {
                _log.Error($"The failure system failed on a failure of {what}: {reporterException}");
                mustStop = true;
            }

        if (!reported)
            _log.Error($"{Level}: {what} failed: {exception}");

        return new CatchResult(exception, mustStop);
    }
}
