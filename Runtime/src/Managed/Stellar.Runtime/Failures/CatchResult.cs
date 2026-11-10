namespace Stellar.Runtime.Failures;

/// <summary>
/// What came of running something under a <see cref="RuntimeCatcher"/>.
/// </summary>
/// <param name="Exception">The exception caught, or <c>null</c>.</param>
/// <param name="MustStop">Whether the engine must stop because of it.</param>
public readonly record struct CatchResult(Exception? Exception, bool MustStop)
{
    /// <summary>
    /// Nothing was caught.
    /// </summary>
    public static CatchResult Success => default;

    /// <summary>
    /// Gets whether something was caught.
    /// </summary>
    public bool Failed => Exception is not null;
}
