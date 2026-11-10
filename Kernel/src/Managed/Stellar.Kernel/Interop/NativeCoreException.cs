using System;

namespace Stellar.Kernel.Interop;

/// <summary>
/// A failed call to the native core.
/// </summary>
public sealed class NativeCoreException : Exception
{
    /// <summary>
    /// Creates the exception.
    /// </summary>
    /// <param name="result">The result code.</param>
    /// <param name="message">The message of the native side.</param>
    public NativeCoreException(NativeResult result, string message)
        : base($"{result}: {message}")
    {
        Result = result;
    }

    /// <summary>
    /// Gets the result code.
    /// </summary>
    public NativeResult Result { get; }
}
