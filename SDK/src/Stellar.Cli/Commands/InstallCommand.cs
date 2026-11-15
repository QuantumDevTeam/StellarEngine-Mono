using System.ComponentModel;
using Spectre.Console.Cli;
using Stellar.Tools.Environment;
using Stellar.Tools.Installation;

namespace Stellar.Cli.Commands;

public sealed class InstallSettings
    : CommandSettings
{
    [CommandArgument(0, "[PATH]")]
    [Description(
        "The checkout of StellarEngine-Core to turn into an installation. $STELLAR_ROOT or the current directory when omitted")]
    public string? Path { get; set; }

    [CommandOption("--no-user-registration")]
    [Description("Do not register this installation for the current user ($STELLAR_HOME or ~/.stellar)")]
    public bool NoUserRegistration { get; set; }
}

public sealed class InstallCommand
    : Command<InstallSettings>
{
    public override int Execute(CommandContext context, InstallSettings settings)
    {
        EngineInstaller.Install(new StellarInstallation(InstallationPaths.Root(settings.Path)),
            ConsoleToolLogger.Instance,
            settings.NoUserRegistration ? null : InstallationRegistry.ForCurrentUser());
        return 0;
    }
}

/// <summary>
/// What install and uninstall take from the machine.
/// </summary>
internal static class InstallationPaths
{
    /// <summary>
    /// The root a command acts on: the argument, $STELLAR_ROOT or the current directory.
    /// </summary>
    public static string Root(string? path)
    {
        return path
               ?? Environment.GetEnvironmentVariable(StellarInstallation.EnvironmentVariable)
               ?? Environment.CurrentDirectory;
    }

    /// <summary>
    /// The NuGet package cache of the user: $NUGET_PACKAGES, or ~/.nuget/packages.
    /// </summary>
    public static string PackageCache()
    {
        var fromVariable = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
        return !string.IsNullOrWhiteSpace(fromVariable)
            ? System.IO.Path.GetFullPath(fromVariable)
            : System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget",
                "packages");
    }
}
