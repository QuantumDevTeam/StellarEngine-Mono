using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using Stellar.Tools.Solutions;

namespace Stellar.Cli.Commands;

/// <summary>
/// The settings of <c>stellar run</c>.
/// </summary>
public sealed class RunSettings
    : CommandSettings
{
    [CommandArgument(0, "[TARGET]")]
    [Description("A profile of the .stellar.solution, or an executable project. The default of the solution when omitted")]
    public string? Target { get; set; }

    [CommandOption("-c|--configuration <CONFIGURATION>")]
    [Description("Debug or Release, over the one of the profile")]
    public string? Configuration { get; set; }

    [CommandOption("--no-build")]
    [Description("Run what was built before")]
    public bool NoBuild { get; set; }

    [CommandOption("--dry-run")]
    [Description("Show the commands without running them")]
    public bool DryRun { get; set; }
}

/// <summary>
/// <c>stellar run</c>: builds and runs a game, a tool, or several of them at once, from anywhere in its solution.
/// Arguments after <c>--</c> go to the application.
/// </summary>
public sealed class RunCommand
    : Command<RunSettings>
{
    public override int Execute(CommandContext context, RunSettings settings)
    {
        var directory = Environment.CurrentDirectory;
        var plan = RunPlanner.Plan(directory, settings.Target, settings.Configuration, context.Remaining.Raw.ToList(),
            settings.NoBuild);

        AnsiConsole.MarkupLine(
            $"[grey]Running[/] {plan.Name.EscapeMarkup()} [grey]({plan.Configuration}, {plan.Projects.Count} project(s))[/]");

        if (settings.DryRun)
        {
            foreach (var command in plan.Builds.Concat(plan.Runs))
                AnsiConsole.WriteLine("dotnet " + string.Join(" ", command));
            return 0;
        }

        return RunExecutor.Execute(plan, directory);
    }
}
