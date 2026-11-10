using Stellar.Kernel.Configuration;

namespace Stellar.Runtime.Configuration;

/// <summary>
/// Reads the runtime configuration of an assembly from one kind of place.
/// </summary>
public interface IRuntimeConfigurationSource
{
    /// <summary>
    /// Gets the kind of place this source reads.
    /// </summary>
    RuntimeConfigurationSource Kind { get; }

    /// <summary>
    /// Reads the configuration.
    /// </summary>
    /// <param name="assemblyPath">The assembly the configuration belongs to. It is not loaded.</param>
    /// <param name="name">The file or the resource, as the assembly names it.</param>
    /// <returns>The configuration.</returns>
    /// <exception cref="RuntimeConfigurationException">Thrown if the configuration is missing or unreadable.</exception>
    RuntimeConfiguration Read(string assemblyPath, string name);
}
