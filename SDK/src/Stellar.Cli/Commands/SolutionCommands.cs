using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using Stellar.Tools.Diagnostics;
using Stellar.Tools.Environment;
using Stellar.Tools.Solutions;

namespace Stellar.Cli.Commands;

/// <summary>
/// The settings of <c>stellar solution sync</c>.
/// </summary>
public sealed class SyncSolutionSettings
    : CommandSettings
{
    [CommandArgument(0, "[DIRECTORY]")]
    [Description("A directory of the solution. The current directory when omitted")]
    public string? Directory { get; set; }
}

/// <summary>
/// <c>stellar solution sync</c>: writes the catalog of the modules of the solution, then the .sln again, after a
/// project or a module was added or "Engine" switched.
/// </summary>
public sealed class SyncSolutionCommand
    : Command<SyncSolutionSettings>
{
    public override int Execute(CommandContext context, SyncSolutionSettings settings)
    {
        var directory = settings.Directory ?? Environment.CurrentDirectory;
        var solution = StellarSolution.Find(directory)
                       ?? throw new ToolException($"No {StellarSolution.Extension} in {directory} or above it");

        var home = StellarInstallation.Locate();

        // First: the references of the projects are read with this catalog
        SolutionModules.Sync(solution, home, ConsoleToolLogger.Instance);
        var path = solution.Sync(new SolutionGenerator(home.Root, new MSBuildProjectReferenceReader(), ConsoleToolLogger.Instance));

        AnsiConsole.MarkupLine($"[green]Solution written:[/] {path.EscapeMarkup()} [grey](engine: {(solution.File.Engine ?? "Packages").EscapeMarkup()})[/]");
        return 0;
    }
}
