using Stellar.Kernel.Failures;

namespace Stellar.Kernel.FileSystem;

/// <summary>
/// Thrown when a path names a domain that is not mounted: an error of the engine, of the file system.
/// </summary>
/// <param name="domain">The name of the domain.</param>
public sealed class DomainNotFoundException(string domain)
    : FailureException(FailureKind.FileSystem, FailureLevel.Error, $"No domain '{domain}' is mounted")
{
    /// <summary>
    /// Gets the name of the domain.
    /// </summary>
    public string Domain { get; } = domain;
}
