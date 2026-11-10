namespace Stellar.Runtime.Hosting;

/// <summary>
/// The exit codes of the runtime. An entry point that fails with a code of its own exits with that code.
/// </summary>
public static class RuntimeExitCodes
{
    /// <summary>
    /// The engine stopped normally.
    /// </summary>
    public const int Success = 0;

    /// <summary>
    /// The engine stopped because of a failure.
    /// </summary>
    public const int Failure = 1;

    /// <summary>
    /// The engine could not start: the configuration or a module is missing or broken.
    /// </summary>
    public const int StartupFailure = 2;
}
