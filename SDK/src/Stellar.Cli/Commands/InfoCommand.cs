using Spectre.Console;
using Spectre.Console.Cli;
using Stellar.Tools.Environment;

namespace Stellar.Cli.Commands;

public sealed class InfoCommand
    : Command
{
    public override int Execute(CommandContext context)
    {
        var registry = InstallationRegistry.ForCurrentUser();
        var home = StellarInstallation.TryLocate(registry: registry);

        var table = new Table().AddColumn("Key").AddColumn("Value");
        table.AddRow("Orchester version", StellarVersions.Orchester.EscapeMarkup());
        table.AddRow("Registry", registry.File.EscapeMarkup());
        table.AddRow("Installation", home?.Root.EscapeMarkup() ?? "[yellow]not found[/]");
        AnsiConsole.Write(table);

        var installations = registry.List();
        if (installations.Count == 0)
            return 0;

        var registered = new Table().AddColumn("Orchestra").AddColumn("Root");
        foreach (var installation in installations)
            registered.AddRow(installation.OrchesterVersion.EscapeMarkup(), installation.Root.EscapeMarkup());

        AnsiConsole.Write(registered);
        return 0;
    }
}
