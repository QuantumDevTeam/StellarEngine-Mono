using System;

namespace Stellar.Kernel.Configuration.Manifest;

/// <summary>
/// Content of <c>manifests/engine.manifest.json</c>: how an installation finds its modules, and what it changes
/// in them.
/// </summary>
/// <remarks>
/// The modules themselves are described by their <c>.stellar.module</c> files and found automatically
/// (<see cref="Discovery"/>). The manifest only holds what this installation does differently: modules that live
/// elsewhere, modules turned off, fields replaced.
/// </remarks>
public sealed class EngineManifest
{
    /// <summary>
    /// The manifest format version this build of the engine understands.
    /// </summary>
    public const int CurrentSchemaVersion = 2;

    /// <summary>
    /// Gets or sets the format version of this manifest.
    /// </summary>
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    /// <summary>
    /// Gets or sets the Stellar Orchester (Software Kit) version the manifest was made for.
    /// </summary>
    public string OrchesterVersion { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets how modules are found.
    /// </summary>
    public ModuleDiscoverySettings Discovery { get; set; } = new();

    /// <summary>
    /// Gets or sets the changes this installation makes to its modules, one entry per module.
    /// </summary>
    public ModuleOverride[] Modules { get; set; } = Array.Empty<ModuleOverride>();
}
