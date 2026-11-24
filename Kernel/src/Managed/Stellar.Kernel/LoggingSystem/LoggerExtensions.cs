#if NET
using System;

namespace Stellar.Kernel.LoggingSystem;

/// <summary>
/// TODO: summary
/// </summary>
public static class LoggerExtensions
{
    /// <summary>
    /// TODO: summary
    /// </summary>
    public static void Info(this ILogger logger, string message)
    {
        logger.Log(LogLevel.Info, message);
    }

    /// <summary>
    /// TODO: summary
    /// </summary>
    public static void Debug(this ILogger logger, string message)
    {
        logger.Log(LogLevel.Debug, message);
    }

    /// <summary>
    /// TODO: summary
    /// </summary>
    public static void Success(this ILogger logger, string message)
    {
        logger.Log(LogLevel.Success, message);
    }

    /// <summary>
    /// TODO: summary
    /// </summary>
    public static void Warning(this ILogger logger, string message)
    {
        logger.Log(LogLevel.Warning, message);
    }

    /// <summary>
    /// TODO: summary
    /// </summary>
    public static void Error(this ILogger logger, string message)
    {
        logger.Log(LogLevel.Error, message);
    }

    /// <summary>
    /// TODO: summary
    /// </summary>
    public static void Exception(this ILogger logger, string message, Exception exception)
    {
        logger.Log(LogLevel.Exception,
            $"{message}\n" +
            $"{exception}\n" +
            $"{exception.GetType()}:  {exception.Message}");
    }

    /// <summary>
    /// TODO: summary
    /// </summary>
    public static void Separator(this ILogger logger)
    {
        logger.LogWithoutFormat("");
    }
}
#endif
