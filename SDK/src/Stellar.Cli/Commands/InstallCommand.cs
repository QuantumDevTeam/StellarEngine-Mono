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
    [Description("Do not make this the installation of the current user")]
    public bool NoUserRegistration { get; set; }
}

public sealed class InstallCommand
    : Command<InstallSettings>
{
    public override int Execute(CommandContext context, InstallSettings settings)
    {
        var root = settings.Path
                   ?? Environment.GetEnvironmentVariable(StellarInstallation.EnvironmentVariable)
                   ?? Environment.CurrentDirectory;

        EngineInstaller.Install(new StellarInstallation(root), ConsoleToolLogger.Instance,
            !settings.NoUserRegistration);
        return 0;
    }
}
