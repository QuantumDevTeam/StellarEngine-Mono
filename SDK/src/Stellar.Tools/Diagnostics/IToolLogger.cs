using System;

namespace Stellar.Tools.Diagnostics;

/// <summary>
/// Severity of a tool message.
/// </summary>
public enum ToolMessageLevel
{
    /// <summary>Details, only interesting while debugging a build.</summary>
    Verbose,

    /// <summary>Normal progress.</summary>
    Info,

    /// <summary>Something is off, but the operation goes on.</summary>
    Warning,

    /// <summary>The operation failed.</summary>
    Error
}

/// <summary>
/// Output of a tool operation.
/// </summary>
/// <remarks>
/// Tools know nothing about the host they run in. Every host adapts this contract to its own output:
/// the MSBuild log for SDK tasks, the console for the CLI, a panel for the Editor.
/// </remarks>
public interface IToolLogger
{
    /// <summary>
    /// Writes a message.
    /// </summary>
    /// <param name="level">The severity.</param>
    /// <param name="message">The text.</param>
    void Log(ToolMessageLevel level, string message);
}

/// <summary>
/// A logger that drops everything.
/// </summary>
public sealed class NullToolLogger
    : IToolLogger
{
    /// <summary>
    /// Gets the shared instance.
    /// </summary>
    public static NullToolLogger Instance { get; } = new();

    private NullToolLogger()
    {
    }

    /// <inheritdoc/>
    public void Log(ToolMessageLevel level, string message)
    {
    }
}

/// <summary>
/// Shorthands for <see cref="IToolLogger"/>.
/// </summary>
public static class ToolLoggerExtensions
{
    /// <summary>Writes a <see cref="ToolMessageLevel.Verbose"/> message.</summary>
    public static void Verbose(this IToolLogger logger, string message)
    {
        logger.Log(ToolMessageLevel.Verbose, message);
    }

    /// <summary>Writes an <see cref="ToolMessageLevel.Info"/> message.</summary>
    public static void Info(this IToolLogger logger, string message)
    {
        logger.Log(ToolMessageLevel.Info, message);
    }

    /// <summary>Writes a <see cref="ToolMessageLevel.Warning"/> message.</summary>
    public static void Warning(this IToolLogger logger, string message)
    {
        logger.Log(ToolMessageLevel.Warning, message);
    }

    /// <summary>Writes an <see cref="ToolMessageLevel.Error"/> message.</summary>
    public static void Error(this IToolLogger logger, string message)
    {
        logger.Log(ToolMessageLevel.Error, message);
    }
}

/// <summary>
/// A failure of a tool operation that is the user's to fix: bad input, a missing file, an invalid manifest.
/// </summary>
/// <remarks>Hosts show its message as is, with no stack trace.</remarks>
public sealed class ToolException
    : Exception
{
    /// <summary>
    /// Creates the exception.
    /// </summary>
    /// <param name="message">What went wrong, written for the user.</param>
    /// <param name="innerException">The underlying failure, if any.</param>
    public ToolException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
