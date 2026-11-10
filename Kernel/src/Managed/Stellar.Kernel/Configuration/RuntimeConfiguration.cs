using System;
using System.Collections.Generic;
using Stellar.Kernel.Quantization;

namespace Stellar.Kernel.Configuration;

/// <summary>
/// Runtime configuration data passed to an entry point.
/// </summary>
/// <remarks>
/// <para>This configuration contains metadata about the project (name, company, version) as well as
/// a collection of <see cref="ConfigurationComponent"/> instances that provide module-specific settings.</para>
/// <para>It is generated at build time by the Stellar build pipeline and read back by the runtime.</para>
/// </remarks>
public class RuntimeConfiguration
    : IQuantumObject
{
    /// <summary>
    /// The configuration format version this build of the engine understands.
    /// </summary>
    public const int CurrentSchemaVersion = 2;

    /// <summary>
    /// Gets or sets the format version of this configuration.
    /// </summary>
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    /// <summary>
    /// Gets or sets the name of the C# project.
    /// </summary>
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the company name that created the project.
    /// </summary>
    public string CompanyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the project version string.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the version of Stellar Orchester (the Stellar Software Kit).
    /// </summary>
    public string StellarOrchesterVersion { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the version of Stellar Engine.
    /// </summary>
    public string StellarEngineVersion { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the fully qualified name of the entry point class.
    /// </summary>
    public string EntryPoint { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the build date as a string (ISO 8601).
    /// </summary>
    public string BuildDate { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the free-form values of the <c>Runtime</c> section of the <c>.stellar.project</c> file.
    /// </summary>
    /// <remarks>Plain strings are stored as is, any other JSON value is stored as its JSON text.</remarks>
    public Dictionary<string, string> Settings { get; set; } = new();

    /// <summary>
    /// Gets or sets an array of configuration components that provide module-specific settings.
    /// </summary>
    public ConfigurationComponent[] Components { get; set; } = Array.Empty<ConfigurationComponent>();
}
