using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using XmlDocMarkdown.Core;

namespace Stellar.Cli.Commands.Project;

public sealed class GenerateProjectDocsSettings
    : CommandSettings
{
    [CommandArgument(0, "<INPUT>")]
    [Description("The assembly")]
    public string Input { get; set; } = string.Empty;

    [CommandArgument(1, "<OUTPUT>")]
    [Description("The output directory")]
    public string Output { get; set; } = string.Empty;
}

public sealed class GenerateProjectDocsCommand
    : Command<GenerateProjectDocsSettings>
{
    public override int Execute(CommandContext context, GenerateProjectDocsSettings settings)
    {
        AnsiConsole.MarkupLineInterpolated($"[green]Generating docs for:[/] [grey]{settings.Input}[/]");
        AnsiConsole.MarkupLineInterpolated($"[green]Output directory:[/] [grey]{settings.Output}[/]");

        IReadOnlyList<string> arguments = [settings.Input, settings.Output, .. context.Remaining.Raw];

        return XmlDocMarkdownApp.Run(arguments);
    }
}
