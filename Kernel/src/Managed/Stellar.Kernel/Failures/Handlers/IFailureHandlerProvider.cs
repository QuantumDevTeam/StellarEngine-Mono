using System.Collections.Generic;
using Stellar.Kernel.Quantization;

namespace Stellar.Kernel.Failures.Handlers;

/// <summary>
/// Provides a collection of appropriate failure handlers for a given failure.
/// </summary>
/// <remarks>
/// Implementations can filter handlers based on failure type, level, source, or any custom criteria.
/// </remarks>
public interface IFailureHandlerProvider
    : IRegistrableQuant
{
    /// <summary>
    /// Returns all handlers that are suitable for handling the specified failure.
    /// </summary>
    /// <param name="failure">The failure to handle.</param>
    /// <returns>The handlers, in the order the dispatcher must invoke them.</returns>
    IEnumerable<IFailureHandler> GetHandlers(IFailure failure);
}
