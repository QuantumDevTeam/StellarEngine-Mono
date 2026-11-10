using System;

namespace Stellar.Kernel.Modules;

/// <summary>
/// Describes one module of the engine: what it is, where it sits and what it needs.
/// </summary>
/// <remarks>
/// <para>
/// This is the content of <c>&lt;Id&gt;.stellar.module</c>, the file at the root of a module folder. Build-time
/// tooling turns it into MSBuild items, the runtime uses it to order module startup, so both read the same
/// description. An entry of <c>manifests/engine.manifest.json</c> may override any field of it, see
/// <see cref="Configuration.Manifest.ModuleOverride"/>.
/// </para>
/// <para>
/// Every field but <see cref="Id"/> has a default, and every default can be replaced: a module needs exactly as
/// much description as it differs from the conventions.
/// </para>
/// </remarks>
public sealed class ModuleDescriptor
{
    /// <summary>
    /// The extension of a module descriptor file.
    /// </summary>
    public const string FileExtension = ".stellar.module";

    /// <summary>
    /// Gets or sets the module identifier. It is also the assembly and the package name, e.g. <c>Stellar.Logging</c>.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the module version, the same one its package has. The engine version when omitted.
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Gets or sets the architectural level of the module.
    /// </summary>
    public ModuleLevel Level { get; set; } = ModuleLevel.HighLevel;

    /// <summary>
    /// Gets or sets what the module is for, in one sentence. It becomes the package description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the managed project, relative to the module folder.
    /// </summary>
    /// <remarks>
    /// When omitted, the first that exists of <c>src/Managed/&lt;Id&gt;/&lt;Id&gt;.csproj</c>,
    /// <c>src/&lt;Id&gt;/&lt;Id&gt;.csproj</c> and <c>&lt;Id&gt;.csproj</c>. An empty string means the module has none.
    /// </remarks>
    public string? Project { get; set; }

    /// <summary>
    /// Gets or sets the native projects, relative to the module folder, in build order.
    /// </summary>
    /// <remarks>When omitted, every <c>.vcxproj</c> under <c>src/Native</c> and then <c>src/NativeInterop</c>.</remarks>
    public string[]? NativeProjects { get; set; }

    /// <summary>
    /// Gets or sets the test projects, relative to the module folder.
    /// </summary>
    /// <remarks>When omitted, every <c>.csproj</c> and <c>.vcxproj</c> under <c>tests</c>.</remarks>
    public string[]? TestProjects { get; set; }

    /// <summary>
    /// Gets or sets the git repository the module sources come from.
    /// </summary>
    public string? Repository { get; set; }

    /// <summary>
    /// Gets or sets the identifiers of the modules this module depends on. <c>Stellar.Kernel</c> and
    /// <c>Stellar.Core</c> are implicit.
    /// </summary>
    public string[] Dependencies { get; set; } = Array.Empty<string>();

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{Id} {Version} ({Level})";
    }
}
