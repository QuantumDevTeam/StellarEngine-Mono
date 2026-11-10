using System;
using Stellar.Kernel.Quantization;

namespace Stellar.Kernel.Failures;

/// <summary>
/// Turns an exception caught by someone who does not know the failure system into a failure.
/// </summary>
/// <remarks>
/// The runtime starts before Stellar.Core and catches with plain <c>try</c>/<c>catch</c>. Once Stellar.Core is up it
/// publishes an implementation as a service, and what the runtime catches goes through the failure levels and
/// handlers like any other failure.
/// </remarks>
public interface IFailureReporter
    : IQuantumObject
{
    /// <summary>
    /// Reports an exception.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <param name="type">What failed.</param>
    /// <param name="critical">Whether the engine cannot go on after it.</param>
    /// <param name="sender">Who caught it.</param>
    /// <returns><c>true</c> if the execution must stop.</returns>
    bool Report(Exception exception, FailureType type, bool critical, IQuantumObject? sender = null);
}
