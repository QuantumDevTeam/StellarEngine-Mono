using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;

namespace Stellar.Tools.Environment;

/// <summary>
/// One installation known to the user: which orchestra it is and where it lies.
/// </summary>
public sealed class RegisteredInstallation
{
    /// <summary>Gets or sets the orchestra version of the installation.</summary>
    public string OrchesterVersion { get; set; } = string.Empty;

    /// <summary>Gets or sets the root of the installation, absolute.</summary>
    public string Root { get; set; } = string.Empty;
}

/// <summary>
/// The content of <see cref="InstallationRegistry.FileName"/>.
/// </summary>
public sealed class InstallationRegistryFile
{
    /// <summary>The current schema.</summary>
    public const int CurrentSchemaVersion = 1;

    /// <summary>Gets or sets the schema of the file.</summary>
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    /// <summary>Gets or sets the installations, one per orchestra version.</summary>
    public List<RegisteredInstallation> Installations { get; set; } = new();
}

/// <summary>
/// The installations of a user: the one place that knows where an orchestra lies on this machine. Everything else
/// finds the installation through here and then refers to it as <c>$(StellarRoot)</c> only.
/// </summary>
/// <remarks>
/// <para>
/// The registry is a folder (<c>~/.stellar</c> by default) with two files: <see cref="FileName"/>, the registry
/// itself, and <see cref="MSBuildFileName"/>, generated from it for Stellar.Sdk, since MSBuild cannot read JSON
/// while it evaluates a project. Tools change the JSON through this class only, so both stay in step.
/// </para>
/// <para>
/// One installation per orchestra version: registering a version again moves it to the new root. What an orchestra
/// holds (modules, versions) is not here: it is in the manifests of the installation itself.
/// </para>
/// <para>
/// An instance is a folder, not a global: tests and sandboxes create their own.
/// </para>
/// </remarks>
public sealed class InstallationRegistry
{
    /// <summary>The environment variable that moves the registry of the current user elsewhere.</summary>
    public const string EnvironmentVariable = "STELLAR_HOME";

    /// <summary>The registry file.</summary>
    public const string FileName = "installations.json";

    /// <summary>The file generated from the registry for Stellar.Sdk.</summary>
    public const string MSBuildFileName = "installations.props";

    private static readonly StringComparer RootComparer =
        Path.DirectorySeparatorChar == '\\' ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    /// <summary>
    /// Creates a registry kept in a folder. Nothing is read or created on disk.
    /// </summary>
    /// <param name="directory">The folder of the registry.</param>
    public InstallationRegistry(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
            throw new ArgumentException("The registry directory must not be empty", nameof(directory));

        Directory = Path.GetFullPath(directory);
    }

    /// <summary>Gets the folder of the registry.</summary>
    public string Directory { get; }

    /// <summary>Gets the registry file.</summary>
    public string File => Path.Combine(Directory, FileName);

    /// <summary>Gets the file generated for Stellar.Sdk.</summary>
    public string MSBuildFile => Path.Combine(Directory, MSBuildFileName);

    /// <summary>
    /// Gets the folder of the registry of the current user: <c>$STELLAR_HOME</c>, or <c>~/.stellar</c>.
    /// </summary>
    public static string UserDirectory
    {
        get
        {
            var fromVariable = System.Environment.GetEnvironmentVariable(EnvironmentVariable);
            return !string.IsNullOrWhiteSpace(fromVariable)
                ? Path.GetFullPath(fromVariable!)
                : Path.Combine(
                    System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), ".stellar");
        }
    }

    /// <summary>
    /// Gets the registry of the current user.
    /// </summary>
    public static InstallationRegistry ForCurrentUser()
    {
        return new InstallationRegistry(UserDirectory);
    }

    /// <summary>
    /// Reads the registered installations. A missing registry is an empty one.
    /// </summary>
    public IReadOnlyList<RegisteredInstallation> List()
    {
        return Read().Installations;
    }

    /// <summary>
    /// Finds the installation of an orchestra version.
    /// </summary>
    /// <returns>The installation, or <c>null</c> if the version is not registered.</returns>
    public StellarInstallation? Find(string orchesterVersion)
    {
        var entry = Read().Installations.FirstOrDefault(installation =>
            string.Equals(installation.OrchesterVersion, orchesterVersion, StringComparison.Ordinal));
        return entry is null ? null : new StellarInstallation(entry.Root);
    }

    /// <summary>
    /// Registers an installation for an orchestra version, replacing the one registered for it before.
    /// </summary>
    public void Register(string orchesterVersion, StellarInstallation installation)
    {
        if (string.IsNullOrWhiteSpace(orchesterVersion))
            throw new ArgumentException("The orchestra version must not be empty", nameof(orchesterVersion));

        var file = Read();
        file.Installations.RemoveAll(entry =>
            string.Equals(entry.OrchesterVersion, orchesterVersion, StringComparison.Ordinal));
        file.Installations.Add(new RegisteredInstallation
            { OrchesterVersion = orchesterVersion, Root = installation.Root });
        Write(file);
    }

    /// <summary>
    /// Removes every registration of an installation.
    /// </summary>
    /// <returns>The orchestra versions that were registered for it.</returns>
    public IReadOnlyList<string> Unregister(StellarInstallation installation)
    {
        var file = Read();
        var removed = file.Installations
            .Where(entry => RootComparer.Equals(Normalize(entry.Root), Normalize(installation.Root)))
            .ToList();

        if (removed.Count == 0)
            return Array.Empty<string>();

        file.Installations.RemoveAll(removed.Contains);
        Write(file);
        return removed.Select(entry => entry.OrchesterVersion).ToList();
    }

    private InstallationRegistryFile Read()
    {
        return System.IO.File.Exists(File)
            ? StellarJson.Read<InstallationRegistryFile>(File)
            : new InstallationRegistryFile();
    }

    private void Write(InstallationRegistryFile file)
    {
        file.SchemaVersion = InstallationRegistryFile.CurrentSchemaVersion;
        file.Installations.Sort((left, right) =>
            string.CompareOrdinal(left.OrchesterVersion, right.OrchesterVersion));

        if (file.Installations.Count == 0)
        {
            // Nothing registered: the registry leaves nothing behind, and an empty folder goes with it
            Delete(File);
            Delete(MSBuildFile);
            if (System.IO.Directory.Exists(Directory) && !System.IO.Directory.EnumerateFileSystemEntries(Directory).Any())
                System.IO.Directory.Delete(Directory);
            return;
        }

        StellarJson.Write(File, file);
        FileWriter.WriteAllText(MSBuildFile, RenderMSBuild(file));
    }

    /// <summary>
    /// Renders the MSBuild view of a registry: the root of the orchestra Stellar.Sdk belongs to.
    /// </summary>
    internal static string RenderMSBuild(InstallationRegistryFile file)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<Project>");
        builder.AppendLine($"    <!-- Generated by the Stellar tools from {FileName}, next to this file: do not edit. -->");
        builder.AppendLine("    <PropertyGroup>");

        foreach (var entry in file.Installations)
        {
            var version = SecurityElement.Escape(entry.OrchesterVersion);
            var root = SecurityElement.Escape(entry.Root.TrimEnd('/', '\\') + Path.DirectorySeparatorChar);
            builder.AppendLine(
                $"        <StellarRoot Condition=\"'$(StellarRoot)' == '' and '$(StellarOrchesterVersion)' == '{version}'\">{root}</StellarRoot>");
        }

        builder.AppendLine("    </PropertyGroup>");
        builder.AppendLine("</Project>");
        return builder.ToString();
    }

    private static string Normalize(string path)
    {
        return Path.GetFullPath(path).TrimEnd('/', '\\');
    }

    private static void Delete(string path)
    {
        if (System.IO.File.Exists(path))
            System.IO.File.Delete(path);
    }
}
