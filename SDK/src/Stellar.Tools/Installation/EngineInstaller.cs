using System.Collections.Generic;
using System.IO;
using Stellar.Kernel.Configuration.Manifest;
using Stellar.Tools.Diagnostics;
using Stellar.Tools.Environment;
using Stellar.Tools.Manifests;
using Stellar.Tools.Modules;
using Stellar.Tools.Templates;

namespace Stellar.Tools.Installation;

/// <summary>
/// Turns a checkout of StellarEngine-Core into an installation: the checkout stays the root, and everything an
/// installation needs is created next to the sources.
/// </summary>
/// <remarks>
/// The operation is idempotent and never overwrites: an existing manifest, configuration or isolation file is
/// kept, so it is safe to run it again after an update of the core.
/// </remarks>
public static class EngineInstaller
{
    /// <summary>
    /// The template of the installation, a folder of <c>Shared/Templates</c>. It mirrors the root.
    /// </summary>
    public const string TemplateName = "Installation";

    /// <summary>
    /// Installs in place.
    /// </summary>
    /// <param name="installation">The installation: a checkout of StellarEngine-Core.</param>
    /// <param name="logger">The output.</param>
    /// <param name="registerForUser">
    /// Whether to make this the installation of the current user. Sandboxes and tests must pass <c>false</c>.
    /// </param>
    /// <exception cref="ToolException">Thrown if the directory is not a checkout of StellarEngine-Core.</exception>
    public static void Install(StellarInstallation installation, IToolLogger logger, bool registerForUser = true)
    {
        if (!installation.HasCoreSources)
            throw new ToolException(
                $"{installation.Root} is not a checkout of StellarEngine-Core: the root of an installation is the core itself");

        foreach (var directory in new[]
                 {
                     installation.Modules, installation.Packages, installation.Manifests, installation.Config,
                     installation.Tools, installation.ResourceCompilers, installation.Projects
                 })
            Directory.CreateDirectory(directory);

        // Isolation of modules/ and projects/ from the build files of the core, and the customization points
        new TemplateRenderer(logger).Render(
            Path.Combine(installation.Templates, TemplateName),
            installation.Root,
            new Dictionary<string, string> { ["OrchesterVersion"] = StellarVersions.Orchester },
            TemplateConflict.KeepExisting);

        if (!File.Exists(installation.EngineManifest))
        {
            EngineManifestStore.Save(installation.EngineManifest,
                new EngineManifest { OrchesterVersion = StellarVersions.Orchester });
            logger.Info($"Created {installation.EngineManifest}");
        }

        ModuleSynchronizer.Synchronize(installation, logger);

        if (registerForUser)
            FileWriter.WriteAllText(
                Path.Combine(StellarInstallation.UserDirectory, StellarInstallation.PointerFileName),
                installation.Root);

        logger.Info($"StellarEngine {StellarVersions.Orchester} installed in {installation.Root}");
    }
}
