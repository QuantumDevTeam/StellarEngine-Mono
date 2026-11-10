using Stellar.Kernel.Configuration.Manifest;
using Stellar.Tools.Diagnostics;
using Stellar.Tools.Environment;

namespace Stellar.Tools.Manifests;

/// <summary>
/// Loads and saves the engine manifest.
/// </summary>
public static class EngineManifestStore
{
    /// <summary>
    /// Loads a manifest and checks that this version of the tools understands it.
    /// </summary>
    /// <param name="path">The manifest file.</param>
    /// <returns>The manifest.</returns>
    /// <exception cref="ToolException">Thrown if the file is missing, malformed or of an unknown format version.</exception>
    public static EngineManifest Load(string path)
    {
        var manifest = StellarJson.Read<EngineManifest>(path);

        if (manifest.SchemaVersion != EngineManifest.CurrentSchemaVersion)
            throw new ToolException(
                $"{path} has format version {manifest.SchemaVersion}, " +
                $"this build understands {EngineManifest.CurrentSchemaVersion}");

        return manifest;
    }

    /// <summary>
    /// Saves a manifest.
    /// </summary>
    /// <param name="path">The manifest file.</param>
    /// <param name="manifest">The manifest.</param>
    public static void Save(string path, EngineManifest manifest)
    {
        StellarJson.Write(path, manifest);
    }
}
