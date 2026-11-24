using System;

namespace Stellar.Kernel.LoggingSystem;

/// <summary>
/// Full logger formats alias
/// </summary>
/// <param name="Info">Info log format</param>
/// <param name="Debug">Debug log format</param>
/// <param name="Success">Success log format</param>
/// <param name="Warning">Warning log format</param>
/// <param name="Error">Error log format</param>
/// <param name="Exception">Exception log format</param>
public readonly record struct LoggerFormat(
    LogFormat Info,
    LogFormat Debug,
    LogFormat Success,
    LogFormat Warning,
    LogFormat Error,
    LogFormat Exception)
{
    /// <summary>
    /// Default Logger format
    /// </summary>
    public static readonly LoggerFormat Default = new(
        new()
        {
            Format = "{0}:{1} | {2}:{3} | {4}      | {5}:{6}:{7} : {8}.{9} - {10}",
            ColorizedFormat = "\e[32m{0}\e[0m" +
                              "\e[31m:\e[0m" +
                              "\e[2;32m{1}\e[0m " +
                              "\e[31m|\e[0m " +
                              "\e[32m{2}\e[0m" +
                              "\e[31m:\e[0m" +
                              "\e[2;32m{3}\e[0m " +
                              "\e[31m|\e[0m " +
                              "\e[1;38m{4}\e[0m      " +
                              "\e[31m|\e[0m " +
                              "\e[36m{5}\e[0m" +
                              "\e[31m:\e[0m" +
                              "\e[36m{6}\e[0m " +
                              "\e[31m:\e[0m" +
                              "\e[36m{7}\e[0m " +
                              "\e[31m:\e[0m " +
                              "\e[36m{8}\e[0m " +
                              "\e[31m.\e[0m" +
                              "\e[36m{9}\e[0m " +
                              "\e[31m-\e[0m " +
                              "\e[1;38m{10}\e[0m"
        },
        new()
        {
            Format = "{0}:{1} | {2}:{3} | {4}     | {5}:{6}:{7} : {8}.{9} - {10}",
            ColorizedFormat = "\e[32m{0}\e[0m" +
                              "\e[31m:\e[0m" +
                              "\e[2;32m{1}\e[0m " +
                              "\e[31m|\e[0m " +
                              "\e[32m{2}\e[0m" +
                              "\e[31m:\e[0m" +
                              "\e[2;32m{3}\e[0m " +
                              "\e[31m|\e[0m " +
                              "\e[1;33m{4}\e[0m     " +
                              "\e[31m|\e[0m " +
                              "\e[36m{5}\e[0m" +
                              "\e[31m:\e[0m" +
                              "\e[36m{6}\e[0m " +
                              "\e[31m:\e[0m" +
                              "\e[36m{7}\e[0m " +
                              "\e[31m:\e[0m " +
                              "\e[36m{8}\e[0m " +
                              "\e[31m.\e[0m" +
                              "\e[36m{9}\e[0m " +
                              "\e[31m-\e[0m " +
                              "\e[1;33m{10}\e[0m"
        },
        new()
        {
            Format = "{0}:{1} | {2}:{3} | {4}   | {5}:{6}:{7} : {8}.{9} - {10}",
            ColorizedFormat = "\e[32m{0}\e[0m" +
                              "\e[31m:\e[0m" +
                              "\e[2;32m{1}\e[0m " +
                              "\e[31m|\e[0m " +
                              "\e[32m{2}\e[0m" +
                              "\e[31m:\e[0m" +
                              "\e[2;32m{3}\e[0m " +
                              "\e[31m|\e[0m " +
                              "\e[1;32m{4}\e[0m   " +
                              "\e[31m|\e[0m " +
                              "\e[36m{5}\e[0m" +
                              "\e[31m:\e[0m" +
                              "\e[36m{6}\e[0m " +
                              "\e[31m:\e[0m" +
                              "\e[36m{7}\e[0m " +
                              "\e[31m:\e[0m " +
                              "\e[36m{8}\e[0m " +
                              "\e[31m.\e[0m" +
                              "\e[36m{9}\e[0m " +
                              "\e[31m-\e[0m " +
                              "\e[1;32m{10}\e[0m"
        },
        new()
        {
            Format = "{0}:{1} | {2}:{3} | {4}   | {5}:{6}:{7} : {8}.{9} - {10}",
            ColorizedFormat = "\e[32m{0}\e[0m" +
                              "\e[31m:\e[0m" +
                              "\e[2;32m{1}\e[0m " +
                              "\e[31m|\e[0m " +
                              "\e[32m{2}\e[0m" +
                              "\e[31m:\e[0m" +
                              "\e[2;32m{3}\e[0m " +
                              "\e[31m|\e[0m " +
                              "\e[1;34m{4}\e[0m   " +
                              "\e[31m|\e[0m " +
                              "\e[36m{5}\e[0m" +
                              "\e[31m:\e[0m" +
                              "\e[36m{6}\e[0m " +
                              "\e[31m:\e[0m" +
                              "\e[36m{7}\e[0m " +
                              "\e[31m:\e[0m " +
                              "\e[36m{8}\e[0m " +
                              "\e[31m.\e[0m" +
                              "\e[36m{9}\e[0m " +
                              "\e[31m-\e[0m " +
                              "\e[1;34m{10}\e[0m"
        },
        new()
        {
            Format = "{0}:{1} | {2}:{3} | {4}     | {5}:{6}:{7} : {8}.{9} - {10}",
            ColorizedFormat = "\e[32m{0}\e[0m" +
                              "\e[31m:\e[0m" +
                              "\e[2;32m{1}\e[0m " +
                              "\e[31m|\e[0m " +
                              "\e[32m{2}\e[0m" +
                              "\e[31m:\e[0m" +
                              "\e[2;32m{3}\e[0m " +
                              "\e[31m|\e[0m " +
                              "\e[1;31m{4}\e[0m     " +
                              "\e[31m|\e[0m " +
                              "\e[36m{5}\e[0m" +
                              "\e[31m:\e[0m" +
                              "\e[36m{6}\e[0m" +
                              "\e[31m:\e[0m" +
                              "\e[36m{7}\e[0m " +
                              "\e[31m:\e[0m " +
                              "\e[36m{8}\e[0m " +
                              "\e[31m.\e[0m" +
                              "\e[36m{9}\e[0m " +
                              "\e[31m-\e[0m " +
                              "\e[1;38m{10}\e[0m"
        },
        new()
        {
            Format = "{0}:{1} | {2}:{3} | {4} | {5}:{6}:{7} : {8}.{9} - {10}",
            ColorizedFormat = "\e[32m{0}\e[0m" +
                              "\e[31m:\e[0m" +
                              "\e[2;32m{1}\e[0m " +
                              "\e[31m|\e[0m " +
                              "\e[32m{2}\e[0m" +
                              "\e[31m:\e[0m" +
                              "\e[2;32m{3}\e[0m " +
                              "\e[31m|\e[0m " +
                              "\e[1;31m{4}\e[0m " +
                              "\e[31m|\e[0m " +
                              "\e[36m{5}\e[0m" +
                              "\e[31m:\e[0m" +
                              "\e[36m{6}\e[0m " +
                              "\e[31m:\e[0m" +
                              "\e[36m{7}\e[0m " +
                              "\e[31m:\e[0m " +
                              "\e[36m{8}\e[0m " +
                              "\e[31m.\e[0m" +
                              "\e[36m{9}\e[0m " +
                              "\e[31m-\e[0m " +
                              "\e[1;31m{10}\e[0m"
        }
    );

    /// <summary>
    /// Gets LogFormat for a LogLevel
    /// </summary>
    /// <param name="level">Log level</param>
    /// <returns>a LogFormat for given LogLevel</returns>
    /// <exception cref="ArgumentException">if LogLevel is out of LogLevel enum</exception>
    public LogFormat ForLevel(LogLevel level) => level switch
    {
        LogLevel.Info => Info,
        LogLevel.Debug => Debug,
        LogLevel.Success => Success,
        LogLevel.Warning => Warning,
        LogLevel.Error => Error,
        LogLevel.Exception => Exception,
        _ => throw new ArgumentException("LoggerFormat.ForLevel operate only levels writen in LogLevel enum",
            nameof(level), null)
    };
}
