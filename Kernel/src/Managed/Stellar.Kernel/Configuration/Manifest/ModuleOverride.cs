using Stellar.Kernel.Modules;

namespace Stellar.Kernel.Configuration.Manifest;

/// <summary>
/// What an installation changes in one module. Every field but <see cref="Id"/> is optional: what is set replaces
/// the value of the <c>.stellar.module</c> file, what is not keeps it.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>A module that lives outside the discovered folders, e.g. a repository being worked on: <see cref="Path"/>.</item>
/// <item>A module turned off without removing it: <see cref="Enabled"/> = <c>false</c>.</item>
/// <item>A module that exists only as a package: no <see cref="Path"/>, no discovered folder, and <see cref="Version"/>.</item>
/// </list>
/// </remarks>
public sealed class ModuleOverride
{
    /// <summary>
    /// Gets or sets the identifier of the module.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the module is part of the installation. <c>true</c> when omitted.
    /// </summary>
    public bool? Enabled { get; set; }

    /// <summary>
    /// Gets or sets the folder of the module, absolute or relative to the root of the installation. It wins over a
    /// discovered folder with the same module.
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// Gets or sets how projects reference the module.
    /// </summary>
    public ModuleLink? Link { get; set; }

    /// <summary>Gets or sets a replacement for <see cref="ModuleDescriptor.Version"/>.</summary>
    public string? Version { get; set; }

    /// <summary>Gets or sets a replacement for <see cref="ModuleDescriptor.Level"/>.</summary>
    public ModuleLevel? Level { get; set; }

    /// <summary>Gets or sets a replacement for <see cref="ModuleDescriptor.Description"/>.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets a replacement for <see cref="ModuleDescriptor.Project"/>.</summary>
    public string? Project { get; set; }

    /// <summary>Gets or sets a replacement for <see cref="ModuleDescriptor.NativeProjects"/>.</summary>
    public string[]? NativeProjects { get; set; }

    /// <summary>Gets or sets a replacement for <see cref="ModuleDescriptor.Dependencies"/>.</summary>
    public string[]? Dependencies { get; set; }
}
