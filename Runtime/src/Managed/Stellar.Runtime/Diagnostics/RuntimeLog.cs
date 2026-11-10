using Stellar.Kernel.LoggingSystem;

namespace Stellar.Runtime.Diagnostics;

/// <summary>
/// What the runtime tells about itself. Before the engine has a logger it writes to the error output; once a module
/// publishes an <see cref="ILogger"/>, it goes there.
/// </summary>
public sealed class RuntimeLog
{
    private readonly TextWriter _fallback;

    /// <summary>
    /// Initializes a new instance of the <see cref="RuntimeLog"/> class.
    /// </summary>
    /// <param name="verbose">Whether the steps of the runtime are told, not only the problems.</param>
    /// <param name="fallback">Where to write without a logger; the error output by default.</param>
    public RuntimeLog(bool verbose = false, TextWriter? fallback = null)
    {
        Verbose = verbose;
        _fallback = fallback ?? Console.Error;
    }

    /// <summary>
    /// Gets or sets whether the steps of the runtime are told.
    /// </summary>
    public bool Verbose { get; set; }

    /// <summary>
    /// Gets or sets the logger of the engine.
    /// </summary>
    public ILogger? Logger { get; set; }

    /// <summary>
    /// Tells a step, if <see cref="Verbose"/>.
    /// </summary>
    /// <param name="message">The step.</param>
    public void Step(string message)
    {
        if (Verbose) Write(LogLevel.Debug, message);
    }

    /// <summary>
    /// Tells a problem.
    /// </summary>
    /// <param name="message">The problem.</param>
    public void Error(string message)
    {
        Write(LogLevel.Error, message);
    }

    private void Write(LogLevel level, string message)
    {
        if (Logger is { } logger)
        {
            logger.Log(level, message);
            return;
        }

        lock (_fallback)
        {
            _fallback.WriteLine($"[Stellar.Runtime] {message}");
        }
    }
}
