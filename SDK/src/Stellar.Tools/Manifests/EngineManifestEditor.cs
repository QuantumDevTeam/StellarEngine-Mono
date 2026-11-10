using System;
using System.Linq;
using Stellar.Kernel.Configuration.Manifest;

namespace Stellar.Tools.Manifests;

/// <summary>
/// Changes the module entries of a manifest. One entry per module: a change to a module edits its entry, or
/// creates one.
/// </summary>
public static class EngineManifestEditor
{
    /// <summary>
    /// Gets the entry of a module, creating it.
    /// </summary>
    /// <param name="manifest">The manifest.</param>
    /// <param name="id">The module.</param>
    /// <returns>The entry, already part of the manifest.</returns>
    public static ModuleOverride Entry(EngineManifest manifest, string id)
    {
        var existing = manifest.Modules.FirstOrDefault(entry => string.Equals(entry.Id, id, StringComparison.Ordinal));
        if (existing is not null)
            return existing;

        var created = new ModuleOverride { Id = id };
        manifest.Modules = manifest.Modules.Append(created).OrderBy(entry => entry.Id, StringComparer.Ordinal)
            .ToArray();
        return created;
    }

    /// <summary>
    /// Removes the entry of a module: the module is back to what its descriptor and discovery say.
    /// </summary>
    /// <returns><c>true</c> if there was an entry.</returns>
    public static bool Reset(EngineManifest manifest, string id)
    {
        var count = manifest.Modules.Length;
        manifest.Modules = manifest.Modules.Where(entry => !string.Equals(entry.Id, id, StringComparison.Ordinal))
            .ToArray();
        return manifest.Modules.Length != count;
    }

    /// <summary>
    /// Drops the entries that change nothing, so the manifest only holds what the installation does differently.
    /// </summary>
    public static void Compact(EngineManifest manifest)
    {
        manifest.Modules = manifest.Modules.Where(entry =>
            entry.Enabled is not null || entry.Path is not null || entry.Link is not null || entry.Version is not null
            || entry.Level is not null || entry.Description is not null || entry.Project is not null
            || entry.NativeProjects is not null || entry.Dependencies is not null).ToArray();
    }
}
