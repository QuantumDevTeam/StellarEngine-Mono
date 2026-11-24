using Stellar.Kernel.Quantization;

namespace Stellar.Kernel.LoggingSystem;

/// <summary>
/// Engine logger
/// </summary>
public interface ILogger
    : IQuant
{
    /// <summary>
    /// Create log with formatting
    /// </summary>
    /// <param name="level">Log level</param>
    /// <param name="message">Log message</param>
    void Log(LogLevel level, string message);

    /// <summary>
    /// Create log without formatting
    /// </summary>
    /// <param name="message">just message to write</param>
    void LogWithoutFormat(string message);

    /// <summary>
    /// TODO: summary
    /// </summary>
    void Start();

    /// <summary>
    /// TODO: summary
    /// </summary>
    void Stop();
}
