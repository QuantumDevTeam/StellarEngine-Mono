using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using Stellar.Tools.Environment;
using Stellar.Tools.Projects;

namespace Stellar.Cli.Commands.Project;

public sealed class NewProjectSettings
    : CommandSettings
{
    [CommandArgument(0, "<TEMPLATE>")]
    [Description("The template, see 'stellar project template list'")]
    public string Template { get; set; } = string.Empty;

    [CommandArgument(1, "<NAME>")]
    [Description("The name of the project")]
    public string Name { get; set; } = string.Empty;

    [CommandOption("-o|--output <DIRECTORY>")]
    [Description("The directory to create. $STELLAR_ROOT/projects/<NAME> when omitted")]
    public string? Output { get; set; }
}

public sealed class NewProjectCommand
    : Command<NewProjectSettings>
{
    public override int Execute(CommandContext context, NewProjectSettings settings)
    {
        var home = StellarInstallation.Locate();
        var output = settings.Output ?? Path.Combine(home.Projects, settings.Name);

        new ProjectScaffolder(Path.Combine(home.Templates, "Projects"), ConsoleToolLogger.Instance)
            .Create(settings.Template, settings.Name, output, home);

        return 0;
    }
}

public sealed class ListTemplatesCommand
    : Command
{
    public override int Execute(CommandContext context)
    {
        var home = StellarInstallation.Locate();
        var templates = new ProjectScaffolder(Path.Combine(home.Templates, "Projects"), ConsoleToolLogger.Instance)
            .ListTemplates();

        if (templates.Count == 0)
            AnsiConsole.MarkupLine("[grey]No templates available[/]");

        foreach (var template in templates)
            AnsiConsole.WriteLine($"- {template}");

        return 0;
    }
}

public sealed class ProjectInfoSettings
    : CommandSettings
{
    [CommandArgument(0, "[DIRECTORY]")]
    [Description("The project directory. The current directory when omitted")]
    public string? Directory { get; set; }
}

public sealed class ProjectInfoCommand
    : Command<ProjectInfoSettings>
{
    public override int Execute(CommandContext context, ProjectInfoSettings settings)
    {
        var project = StellarProject.Find(settings.Directory ?? Environment.CurrentDirectory);
        var files = ProjectFileResolver.Resolve(project);

        var table = new Table().AddColumn("Key").AddColumn("Value");
        table.AddRow("Name", project.Name.EscapeMarkup());
        table.AddRow("Entry point", project.ResolveEntryPoint(project.Name).EscapeMarkup());
        table.AddRow("External assets", files.ExternalAssets.Count.ToString());
        table.AddRow("Embedded assets", files.EmbeddedAssets.Count.ToString());
        table.AddRow("Resources", files.Resources.Count.ToString());
        table.AddRow("Localization files", files.LocalizationFiles.Count.ToString());

        AnsiConsole.Write(table);
        return 0;
    }
}
