using System;
using System.IO;
using System.Linq;
using Stellar.Kernel.Modules;

namespace Stellar.Tools.Modules;

/// <summary>
/// Where the description of a module came from.
/// </summary>
public enum ModuleSource
{
    /// <summary>Found in a folder the installation searches (<c>modules/</c>).</summary>
    Discovered,

    /// <summary>A folder the manifest points at, e.g. a repository being worked on.</summary>
    External,

    /// <summary>Declared by the manifest only: the module exists as a package.</summary>
    Package
}

/// <summary>
/// A module as the installation sees it: its descriptor with the defaults applied and the manifest overrides on
/// top, and where it lives.
/// </summary>
public sealed class ResolvedModule
{
    /// <summary>
    /// Creates a module.
    /// </summary>
    /// <param name="descriptor">The effective descriptor: every default is filled in.</param>
    /// <param name="directory">The module folder, or <c>null</c> for a module that exists as a package only.</param>
    /// <param name="source">Where the description came from.</param>
    /// <param name="link">How projects reference the module.</param>
    public ResolvedModule(ModuleDescriptor descriptor, string? directory, ModuleSource source, ModuleLink link)
    {
        Descriptor = descriptor;
        Directory = directory;
        Source = source;
        Link = link;
    }

    /// <summary>Gets the effective descriptor.</summary>
    public ModuleDescriptor Descriptor { get; }

    /// <summary>Gets the module folder, or <c>null</c> for a module that exists as a package only.</summary>
    public string? Directory { get; }

    /// <summary>Gets where the description came from.</summary>
    public ModuleSource Source { get; }

    /// <summary>Gets how projects reference the module.</summary>
    public ModuleLink Link { get; }

    /// <summary>Gets the identifier.</summary>
    public string Id => Descriptor.Id;

    /// <summary>Gets the descriptor file, if the module has sources.</summary>
    public string? DescriptorFile =>
        Directory is null ? null : Path.Combine(Directory, ModuleConventions.DescriptorFileName(Id));

    /// <summary>Gets the managed project, absolute, if there is one.</summary>
    public string? ProjectPath =>
        Directory is null || string.IsNullOrEmpty(Descriptor.Project)
            ? null
            : Path.GetFullPath(Path.Combine(Directory, Descriptor.Project));

    /// <summary>Gets the native projects, absolute.</summary>
    public string[] NativeProjectPaths => Absolute(Descriptor.NativeProjects);

    /// <summary>Gets the test projects, absolute.</summary>
    public string[] TestProjectPaths => Absolute(Descriptor.TestProjects);

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{Descriptor} from {Source}";
    }

    private string[] Absolute(string[]? paths)
    {
        return Directory is null || paths is null
            ? Array.Empty<string>()
            : paths.Select(path => Path.GetFullPath(Path.Combine(Directory, path))).ToArray();
    }
}
