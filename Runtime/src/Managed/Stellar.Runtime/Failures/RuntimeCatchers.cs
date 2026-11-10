using Stellar.Kernel.Failures;
using Stellar.Runtime.Diagnostics;

namespace Stellar.Runtime.Failures;

/// <summary>
/// The catchers of every <see cref="CatchLevel"/>, sharing the log and, once there is one, the failure system.
/// </summary>
public sealed class RuntimeCatchers
{
    private readonly Dictionary<CatchLevel, RuntimeCatcher> _catchers;

    /// <summary>
    /// Initializes a new instance of the <see cref="RuntimeCatchers"/> class.
    /// </summary>
    /// <param name="log">The log of the runtime.</param>
    /// <param name="reporter">The reporter of the failure system, once there is one.</param>
    /// <param name="engineLogs">Whether the failure system has a logger.</param>
    public RuntimeCatchers(RuntimeLog log, Func<IFailureReporter?> reporter, Func<bool> engineLogs)
    {
        _catchers = Enum.GetValues<CatchLevel>()
            .ToDictionary(level => level, level => new RuntimeCatcher(level, log, reporter, engineLogs));
    }

    /// <summary>
    /// Gets the catcher of the runtime itself.
    /// </summary>
    public RuntimeCatcher Runtime => this[CatchLevel.Runtime];

    /// <summary>
    /// Gets the catcher of initializations.
    /// </summary>
    public RuntimeCatcher Initialization => this[CatchLevel.Initialization];

    /// <summary>
    /// Gets the catcher of the run of the engine.
    /// </summary>
    public RuntimeCatcher Engine => this[CatchLevel.Engine];

    /// <summary>
    /// Gets the catcher of the main loop.
    /// </summary>
    public RuntimeCatcher MainLoop => this[CatchLevel.MainLoop];

    /// <summary>
    /// Gets the catcher of a level.
    /// </summary>
    /// <param name="level">The level.</param>
    public RuntimeCatcher this[CatchLevel level] => _catchers[level];
}
