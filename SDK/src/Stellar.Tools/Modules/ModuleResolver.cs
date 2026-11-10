using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Stellar.Kernel.Configuration.Manifest;
using Stellar.Kernel.Modules;
using Stellar.Tools.Diagnostics;
using Stellar.Tools.Environment;

namespace Stellar.Tools.Modules;

/// <summary>
/// The modules of an installation, and what is wrong with them.
/// </summary>
/// <param name="Modules">The enabled modules, sorted by identifier.</param>
/// <param name="Disabled">The identifiers of the modules the manifest turned off.</param>
/// <param name="Errors">Problems that make the set unusable.</param>
/// <param name="Warnings">Problems worth knowing about.</param>
public sealed record ModuleResolution(
    IReadOnlyList<ResolvedModule> Modules,
    IReadOnlyList<string> Disabled,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings)
{
    /// <summary>Gets a value indicating whether the set can be used.</summary>
    public bool IsValid => Errors.Count == 0;
}

/// <summary>
/// Puts together the modules of an installation: the discovered descriptors, the folders the manifest points at,
/// the overrides of the manifest, and the conventions for everything left unsaid. The order of precedence, from
/// the strongest: the manifest, the descriptor, the conventions.
/// </summary>
public static class ModuleResolver
{
    /// <summary>
    /// The modules of the core. They are not modules of an installation and cannot be declared as such.
    /// </summary>
    public static readonly string[] CoreModules = { "Stellar.Kernel", "Stellar.Core" };

    /// <summary>
    /// Resolves the modules of an installation.
    /// </summary>
    /// <param name="installation">The installation.</param>
    /// <param name="manifest">Its manifest.</param>
    /// <returns>The modules and the problems.</returns>
    public static ModuleResolution Resolve(StellarInstallation installation, EngineManifest manifest)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        var found =
            new Dictionary<string, (ModuleDescriptor Descriptor, string Directory, ModuleSource Source)>(StringComparer
                .Ordinal);

        foreach (var file in ModuleDiscovery.Discover(installation, manifest.Discovery))
            TryAdd(file, ModuleSource.Discovered);

        var overrides = new Dictionary<string, ModuleOverride>(StringComparer.Ordinal);
        foreach (var entry in manifest.Modules)
            if (string.IsNullOrWhiteSpace(entry.Id))
                errors.Add("A module entry of the manifest has no Id");
            else if (overrides.ContainsKey(entry.Id))
                errors.Add($"The manifest has two entries for {entry.Id}");
            else
                overrides.Add(entry.Id, entry);

        // A folder of the manifest wins over a discovered one: that is how a module is worked on in place
        foreach (var entry in overrides.Values.Where(entry => !string.IsNullOrWhiteSpace(entry.Path)))
        {
            var directory = installation.Resolve(entry.Path!);
            var file = Path.Combine(directory, ModuleConventions.DescriptorFileName(entry.Id));

            if (!File.Exists(file))
            {
                errors.Add(
                    $"{entry.Id}: the manifest points at {directory}, which has no {ModuleConventions.DescriptorFileName(entry.Id)}");
                continue;
            }

            found.Remove(entry.Id);
            TryAdd(file, ModuleSource.External);
        }

        var modules = new List<ResolvedModule>();
        var disabled = new List<string>();

        foreach (var id in found.Keys.Union(overrides.Keys).OrderBy(id => id, StringComparer.Ordinal))
        {
            overrides.TryGetValue(id, out var entry);

            if (CoreModules.Contains(id))
            {
                errors.Add($"{id} is part of the core, not a module of the installation");
                continue;
            }

            if (entry?.Enabled == false)
            {
                disabled.Add(id);
                continue;
            }

            if (found.TryGetValue(id, out var source))
            {
                var descriptor = Apply(Copy(source.Descriptor), entry);
                ApplyConventions(descriptor, source.Directory);
                modules.Add(new ResolvedModule(descriptor, source.Directory, source.Source,
                    entry?.Link ?? ModuleLink.Default));
            }
            else if (string.IsNullOrWhiteSpace(entry!.Version))
            {
                errors.Add(
                    $"{id}: the manifest declares it, but there is no module folder, and no Version to take it as a package");
            }
            else
            {
                var descriptor = Apply(new ModuleDescriptor { Id = id }, entry);
                modules.Add(new ResolvedModule(descriptor, null, ModuleSource.Package, ModuleLink.Package));

                if (entry.Link == ModuleLink.Sources)
                    warnings.Add(
                        $"{id}: Link is Sources, but the module has no sources here; it is taken as a package");
            }
        }

        foreach (var module in modules)
        {
            foreach (var dependency in module.Descriptor.Dependencies.Where(disabled.Contains))
                errors.Add($"{module.Id} depends on {dependency}, which the manifest turned off");

            foreach (var dependency in module.Descriptor.Dependencies.Where(CoreModules.Contains))
                warnings.Add($"{module.Id}: {dependency} is implicit, it need not be listed in Dependencies");

            module.Descriptor.Dependencies = module.Descriptor.Dependencies
                .Where(dependency => !CoreModules.Contains(dependency)).ToArray();

            if (module.Directory is not null && module.ProjectPath is null && module.NativeProjectPaths.Length == 0)
                warnings.Add($"{module.Id}: no managed and no native project found in {module.Directory}");

            if (module.ProjectPath is { } project && !File.Exists(project))
                errors.Add($"{module.Id}: the project {project} does not exist");
        }

        return new ModuleResolution(modules, disabled, errors, warnings);

        void TryAdd(string file, ModuleSource source)
        {
            ModuleDescriptor descriptor;
            try
            {
                descriptor = ModuleDescriptorFile.Read(file);
            }
            catch (ToolException exception)
            {
                errors.Add(exception.Message);
                return;
            }

            if (found.TryGetValue(descriptor.Id, out var existing))
            {
                errors.Add(
                    $"{descriptor.Id} is found twice: in {existing.Directory} and in {Path.GetDirectoryName(file)}");
                return;
            }

            found.Add(descriptor.Id, (descriptor, Path.GetDirectoryName(file)!, source));
        }
    }

    private static ModuleDescriptor Apply(ModuleDescriptor descriptor, ModuleOverride? entry)
    {
        if (entry is null)
            return descriptor;

        descriptor.Version = entry.Version ?? descriptor.Version;
        descriptor.Level = entry.Level ?? descriptor.Level;
        descriptor.Description = entry.Description ?? descriptor.Description;
        descriptor.Project = entry.Project ?? descriptor.Project;
        descriptor.NativeProjects = entry.NativeProjects ?? descriptor.NativeProjects;
        descriptor.Dependencies = entry.Dependencies ?? descriptor.Dependencies;
        return descriptor;
    }

    private static void ApplyConventions(ModuleDescriptor descriptor, string directory)
    {
        descriptor.Version ??= StellarVersions.Engine;
        descriptor.Project ??= ModuleConventions.FindManagedProject(directory, descriptor.Id) ?? string.Empty;
        descriptor.NativeProjects ??= ModuleConventions.FindNativeProjects(directory);
        descriptor.TestProjects ??= ModuleConventions.FindTestProjects(directory);
    }

    private static ModuleDescriptor Copy(ModuleDescriptor source)
    {
        return new ModuleDescriptor
        {
            Id = source.Id,
            Version = source.Version,
            Level = source.Level,
            Description = source.Description,
            Project = source.Project,
            NativeProjects = source.NativeProjects,
            TestProjects = source.TestProjects,
            Repository = source.Repository,
            Dependencies = source.Dependencies ?? Array.Empty<string>()
        };
    }
}
