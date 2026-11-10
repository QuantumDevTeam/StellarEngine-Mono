using System;
using System.IO;

namespace Stellar.Tools.Environment;

/// <summary>
/// The layout of an engine installation (<c>$STELLAR_ROOT</c>).
/// </summary>
/// <remarks>
/// The root of an installation is a checkout of StellarEngine-Core: the core lies right there, and the installer
/// adds everything else next to it.
/// <code>
/// `StellarEngine/` $STELLAR_ROOT, StellarEngine-Core itself
/// ├── `Kernel/ Core/ SDK/`  the sources of the core
/// ├── `Shared/` build files, schemas and templates shared by the core and the modules
/// ├── `Docs/` documentation; Docs/Modules/ is generated for the modules
/// ├── `modules/` the modules of the installation, one folder each (installer)
/// ├── `packages/` the local NuGet feed (installer)
/// ├── `manifests/` engine.manifest.json and what is generated from it (installer)
/// ├── `config/` installation-wide SDK customization (installer)
/// ├── `tools/` editor, comm-server, resource compilers (installer)
/// ├── `projects/` user games (installer)
/// └── `artifacts/` bin/ and obj/ of everything built, managed and native (build)
/// </code>
/// </remarks>
public sealed class StellarInstallation
{
    /// <summary>
    /// The environment variable that points at the installation.
    /// </summary>
    public const string EnvironmentVariable = "STELLAR_ROOT";

    /// <summary>
    /// The per-user pointer file, kept in <see cref="UserDirectory"/>.
    /// </summary>
    public const string PointerFileName = "installation_location.txt";

    /// <summary>
    /// Creates the layout for a directory. Nothing is created or checked on disk.
    /// </summary>
    /// <param name="root">The root of the installation.</param>
    public StellarInstallation(string root)
    {
        if (string.IsNullOrWhiteSpace(root))
            throw new ArgumentException("The installation root must not be empty", nameof(root));

        Root = Path.GetFullPath(root);
    }

    /// <summary>Gets the root of the installation, which is also the root of the core.</summary>
    public string Root { get; }

    /// <summary>Gets the folder of files shared by the core and the modules.</summary>
    public string Shared => Path.Combine(Root, "Shared");

    /// <summary>Gets the templates of projects, modules and of the installation itself.</summary>
    public string Templates => Path.Combine(Shared, "Templates");

    /// <summary>Gets the JSON schemas.</summary>
    public string Schemas => Path.Combine(Shared, "Schemas");

    /// <summary>Gets the directory with module folders.</summary>
    public string Modules => Path.Combine(Root, "modules");

    /// <summary>Gets the local NuGet feed.</summary>
    public string Packages => Path.Combine(Root, "packages");

    /// <summary>Gets the directory of standalone tools.</summary>
    public string Tools => Path.Combine(Root, "tools");

    /// <summary>Gets the directory of additional resource compilers.</summary>
    public string ResourceCompilers => Path.Combine(Tools, "compilers");

    /// <summary>Gets the directory of manifests.</summary>
    public string Manifests => Path.Combine(Root, "manifests");

    /// <summary>Gets the directory of installation-wide customization.</summary>
    public string Config => Path.Combine(Root, "config");

    /// <summary>Gets the directory of generated module documentation.</summary>
    public string ModuleDocs => Path.Combine(Root, "Docs", "Modules");

    /// <summary>Gets the directory of user projects.</summary>
    public string Projects => Path.Combine(Root, "projects");

    /// <summary>Gets the build output of everything: <c>bin/</c> and <c>obj/</c>, one folder per project.</summary>
    public string Artifacts => Path.Combine(Root, "artifacts");

    /// <summary>Gets the binaries: <c>artifacts/bin/&lt;project&gt;/&lt;configuration&gt;/</c>.</summary>
    public string ArtifactsBin => Path.Combine(Artifacts, "bin");

    /// <summary>Gets the intermediate files: <c>artifacts/obj/&lt;project&gt;/</c>.</summary>
    public string ArtifactsObj => Path.Combine(Artifacts, "obj");

    /// <summary>Gets the engine manifest file.</summary>
    public string EngineManifest => Path.Combine(Manifests, "engine.manifest.json");

    /// <summary>Gets the MSBuild module catalog generated from the manifest and the module descriptors.</summary>
    public string ModuleCatalog => Path.Combine(Manifests, ".generated", "stellar.modules.props");

    /// <summary>Gets the resolved module list, the same content as the catalog, for tools that read JSON.</summary>
    public string ModuleLock => Path.Combine(Manifests, ".generated", "stellar.modules.json");

    /// <summary>
    /// Gets a value indicating whether the directory holds the sources of the core. An installation is always a
    /// checkout of StellarEngine-Core.
    /// </summary>
    public bool HasCoreSources => File.Exists(Path.Combine(Shared, "Build", "Stellar.Versions.props"));

    /// <summary>
    /// Gets a value indicating whether <c>stellar install</c> ran here.
    /// </summary>
    public bool IsInstalled => File.Exists(EngineManifest);

    /// <summary>
    /// Gets the per-user directory of this Stellar release.
    /// </summary>
    public static string UserDirectory => Path.Combine(
        System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
        ".stellar",
        StellarVersions.Orchester);

    /// <summary>
    /// Resolves a path of a manifest or a descriptor: absolute as is, relative against the root.
    /// </summary>
    public string Resolve(string path)
    {
        return Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(Root, path));
    }

    /// <summary>
    /// Finds the installation: the environment variable, then the per-user pointer, then the directories above
    /// <paramref name="startDirectory"/>.
    /// </summary>
    /// <param name="startDirectory">Where to start walking up. The current directory when omitted.</param>
    /// <returns>The installation, or <c>null</c> if there is none.</returns>
    public static StellarInstallation? TryLocate(string? startDirectory = null)
    {
        var fromVariable = System.Environment.GetEnvironmentVariable(EnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(fromVariable))
            return new StellarInstallation(fromVariable!);

        for (var directory = new DirectoryInfo(startDirectory ?? System.Environment.CurrentDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            var candidate = new StellarInstallation(directory.FullName);
            if (candidate.IsInstalled)
                return candidate;
        }

        var pointer = Path.Combine(UserDirectory, PointerFileName);
        if (File.Exists(pointer))
        {
            var fromPointer = File.ReadAllText(pointer).Trim();
            if (fromPointer.Length != 0)
                return new StellarInstallation(fromPointer);
        }

        return null;
    }

    /// <summary>
    /// Finds the installation or fails with a message that tells how to get one.
    /// </summary>
    /// <param name="startDirectory">Where to start walking up. The current directory when omitted.</param>
    /// <returns>The installation.</returns>
    public static StellarInstallation Locate(string? startDirectory = null)
    {
        return TryLocate(startDirectory) ?? throw new Diagnostics.ToolException(
            $"StellarEngine installation not found. Set {EnvironmentVariable}, or run 'stellar install' in a checkout of StellarEngine-Core.");
    }
}
