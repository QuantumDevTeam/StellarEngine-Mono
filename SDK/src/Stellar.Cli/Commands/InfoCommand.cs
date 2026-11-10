using Spectre.Console;
using Spectre.Console.Cli;
using Stellar.Tools.Environment;

namespace Stellar.Cli.Commands;

public sealed class InfoCommand
    : Command
{
    public override int Execute(CommandContext context)
    {
        var home = StellarInstallation.TryLocate();

        var table = new Table().AddColumn("Key").AddColumn("Value");
        table.AddRow("Orchester version", StellarVersions.Orchester.EscapeMarkup());
        table.AddRow("User directory", StellarInstallation.UserDirectory.EscapeMarkup());
        table.AddRow("Installation", home?.Root.EscapeMarkup() ?? "[yellow]not found[/]");

        AnsiConsole.Write(table);
        return 0;
    }
}
