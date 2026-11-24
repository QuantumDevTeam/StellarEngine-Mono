namespace Stellar.Kernel.LoggingSystem;

/// <summary>
/// Level of log
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// Debug message or information
    /// </summary>
    Debug = 0,

    /// <summary>
    /// Any Information
    /// </summary>
    Info = 1,

    /// <summary>
    /// Operation succeed
    /// </summary>
    Success = 2,

    /// <summary>
    /// A Warning
    /// </summary>
    Warning = 3,

    /// <summary>
    /// An Error
    /// </summary>
    Error = 4,

    /// <summary>
    /// An Exception
    /// </summary>
    Exception = 5
}
