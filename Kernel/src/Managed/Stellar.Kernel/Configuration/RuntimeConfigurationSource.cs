namespace Stellar.Kernel.Configuration;

/// <summary>
/// Where the runtime configuration of an assembly is kept.
/// </summary>
public enum RuntimeConfigurationSource
{
    /// <summary>
    /// A file next to the assembly.
    /// </summary>
    External,

    /// <summary>
    /// A resource of the assembly itself.
    /// </summary>
    Embedded
}
