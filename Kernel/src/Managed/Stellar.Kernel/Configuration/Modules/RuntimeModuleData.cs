using System;
using Stellar.Kernel.Modules;

namespace Stellar.Kernel.Configuration.Modules;

/// <summary>
/// A module the runtime loads, as the build resolved it.
/// </summary>
public sealed class RuntimeModuleData
{
    /// <summary>
    /// Gets or sets the identifier of the module.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the version the application was built against.
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Gets or sets the level of the module.
    /// </summary>
    public ModuleLevel Level { get; set; } = ModuleLevel.HighLevel;

    /// <summary>
    /// Gets or sets the simple name of the assembly of the module. Empty means the identifier.
    /// </summary>
    public string? Assembly { get; set; }

    /// <summary>
    /// Gets or sets the modules this one depends on directly.
    /// </summary>
    public string[] Dependencies { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets the simple name of the assembly of the module.
    /// </summary>
    public string AssemblyName => string.IsNullOrEmpty(Assembly) ? Id : Assembly!;

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{Id} {Version} ({Level})";
    }
}
