using System.ComponentModel;
using Spectre.Console.Cli;
using Stellar.Tools.Environment;
using Stellar.Tools.Installation;

namespace Stellar.Cli.Commands;

public sealed class UninstallSettings
    : CommandSettings
{
    [CommandArgument(0, "[PATH]")]
    [Description("The installation to remove. $STELLAR_ROOT or the current directory when omitted")]
    public string? Path { get; set; }

    [CommandOption("--all")]
    [Description("Also remove the files you may have edited: config/, the isolation files, the engine manifest")]
    public bool All { get; set; }

    [CommandOption("--artifacts")]
    [Description("Also remove the build output, artifacts/")]
    public bool Artifacts { get; set; }

    [CommandOption("--keep-registration")]
    [Description("Keep the registration of the current user ($STELLAR_HOME or ~/.stellar)")]
    public bool KeepRegistration { get; set; }

    [CommandOption("--keep-package-cache")]
    [Description("Keep the packages of this installation in the NuGet cache")]
    public bool KeepPackageCache { get; set; }

    [CommandOption("--dry-run")]
    [Description("Only tell what would be removed")]
    public bool DryRun { get; set; }
}

public sealed class UninstallCommand
    : Command<UninstallSettings>
{
    public override int Execute(CommandContext context, UninstallSettings settings)
    {
        EngineUninstaller.Uninstall(new StellarInstallation(InstallationPaths.Root(settings.Path)),
            ConsoleToolLogger.Instance,
            new UninstallOptions
            {
                Registry = settings.KeepRegistration ? null : InstallationRegistry.ForCurrentUser(),
                PackageCache = settings.KeepPackageCache ? null : InstallationPaths.PackageCache(),
                IncludeEdited = settings.All,
                IncludeArtifacts = settings.Artifacts,
                DryRun = settings.DryRun
            });
        return 0;
    }
}
