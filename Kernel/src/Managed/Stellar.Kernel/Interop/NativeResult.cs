namespace Stellar.Kernel.Interop;

/// <summary>
/// Result code of a native call. Exceptions never cross the C ABI, errors travel as data.
/// </summary>
/// <remarks>The values match <c>StellarResult</c> of the native Kernel.</remarks>
public enum NativeResult
{
    /// <summary>The operation succeeded.</summary>
    Ok = 0,

    /// <summary>The operation failed for an unspecified reason.</summary>
    Unknown = 1,

    /// <summary>An argument was rejected.</summary>
    InvalidArgument = 2,

    /// <summary>The handle is stale or was never valid.</summary>
    InvalidHandle = 3,

    /// <summary>The requested object does not exist.</summary>
    NotFound = 4,

    /// <summary>An object with the same key already exists.</summary>
    AlreadyExists = 5,

    /// <summary>The caller's buffer is too small for the result.</summary>
    BufferTooSmall = 6,

    /// <summary>The contract versions of the two sides are incompatible.</summary>
    VersionMismatch = 7,

    /// <summary>The native side ran out of memory.</summary>
    OutOfMemory = 8,

    /// <summary>The native side caught an exception at the ABI boundary.</summary>
    NativeException = 9
}
