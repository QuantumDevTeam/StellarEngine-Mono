using Stellar.Kernel.Quantization;

namespace Stellar.Kernel.Configuration;

/// <summary>
/// Abstract base class for a piece of runtime configuration data.
/// </summary>
/// <remarks>
/// Configuration components are collected in <see cref="RuntimeConfiguration.Components"/>.
/// Each component defines how it should be built (engine component, default JSON, custom task, or script).
/// </remarks>
public abstract class ConfigurationComponent
    : IQuantumObject
{
    /// <summary>
    /// Gets the discriminator written to the runtime configuration file. Readers use it to pick the concrete type.
    /// </summary>
    public abstract string Kind { get; }

    /// <summary>
    /// Gets the build type that indicates how this component should be constructed by the S.R.I. builder.
    /// </summary>
    public abstract ConfigurationComponentBuildType ComponentBuildType { get; }
}
