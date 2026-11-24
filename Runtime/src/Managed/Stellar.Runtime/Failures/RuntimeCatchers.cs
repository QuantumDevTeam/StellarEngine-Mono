using Stellar.Kernel.FailureSystem;
using Stellar.Runtime.Diagnostics;

namespace Stellar.Runtime.Failures;

/// <summary>
/// The catchers of the runtime, one per <see cref="CatchLevel"/>: plain <see cref="Catcher"/>s that report to the
/// failure system of the engine once there is one, and to the log of the runtime before.
/// </summary>
/// <remarks>
/// <para>The runtime calls them with <see cref="Catcher.Guard"/>: it never throws, and <see cref="MustStop"/> says
/// what the failure means at the level it was caught. Every level but <see cref="CatchLevel.MainLoop"/> is fatal: a
/// failure there stops the engine whatever the reaction. In the main loop, the reaction decides.</para>
/// <para>Without a failure system, a failure in the main loop is logged and the loop goes on.</para>
/// <para>A failure the engine does not log (no failure system yet, or no logger) is written to the log of the
/// runtime, so nothing is lost.</para>
/// </remarks>
public sealed class RuntimeCatchers
{
    // Failures that repeat on every frame must cost no memory: the catchers keep the last few
    private const int Kept = 16;

    private readonly Catcher[] _catchers;
    private readonly RuntimeLog _log;
    private readonly Func<IFailureSystem?> _system;
    private readonly Func<bool> _engineLogs;

    /// <summary>
    /// Makes the catchers of the runtime.
    /// </summary>
    /// <param name="log">The log of the runtime.</param>
    /// <param name="system">The failure system of the engine, once there is one.</param>
    /// <param name="engineLogs">Whether the engine has a logger.</param>
    public RuntimeCatchers(RuntimeLog log, Func<IFailureSystem?> system, Func<bool> engineLogs)
    {
        _log = log;
        _system = system;
        _engineLogs = engineLogs;
        var levels = Enum.GetValues<CatchLevel>();
        _catchers = new Catcher[levels.Max(level => (int)level) + 1];
        foreach (var level in levels)
            _catchers[(int)level] = new Catcher(Report, FailureKind.Engine,
                level == CatchLevel.MainLoop ? FailureLevel.Error : FailureLevel.CriticalError) { Keeps = Kept };
    }

    /// <summary>Gets the catcher of the runtime itself.</summary>
    public Catcher Runtime => this[CatchLevel.Runtime];

    /// <summary>Gets the catcher of initializations.</summary>
    public Catcher Initialization => this[CatchLevel.Initialization];

    /// <summary>Gets the catcher of the run of the engine.</summary>
    public Catcher Engine => this[CatchLevel.Engine];

    /// <summary>Gets the catcher of the main loop.</summary>
    public Catcher MainLoop => this[CatchLevel.MainLoop];

    /// <summary>
    /// Gets the catcher of a level.
    /// </summary>
    /// <param name="level">The level.</param>
    public Catcher this[CatchLevel level] => _catchers[(int)level];

    /// <summary>
    /// Gets whether the engine must stop because of a failure caught at a level.
    /// </summary>
    /// <param name="level">Where it was caught.</param>
    /// <param name="failure">The failure, or <c>null</c> if nothing failed.</param>
    /// <returns><c>true</c> at a fatal level, or in the main loop when the reaction stops the work.</returns>
    public static bool MustStop(CatchLevel level, Failure? failure)
    {
        if (failure is null) return false;
        return level != CatchLevel.MainLoop || failure.Reaction?.Stops() == true;
    }

    private FailureReaction Report(Failure failure)
    {
        // Without a failure system nothing decides for the main loop: its failures are logged and the loop goes on;
        // the fatal levels catch at CriticalError and stop
        var reaction = failure.Level == FailureLevel.CriticalError ? FailureReactions.Default(failure.Level) : FailureReaction.Log;
        var logged = false;

        if (_system() is { } system)
            try
            {
                reaction = system.Report(failure);
                logged = (reaction & FailureReaction.Log) != 0 && _engineLogs();
            }
            catch (Exception exception)
            {
                _log.Error($"The failure system failed on {failure}: {exception}");
                reaction |= FailureReaction.StopEngine;
            }

        if (!logged) _log.Error($"{failure}{(failure.Exception is { } cause ? $": {cause}" : "")}");
        return reaction;
    }
}
