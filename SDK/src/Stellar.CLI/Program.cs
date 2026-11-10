using Spectre.Console;
using Spectre.Console.Cli;
using Stellar.Cli.Commands;
using Stellar.Cli.Commands.Modules;
using Stellar.Cli.Commands.Project;
using Stellar.Cli.Commands.Resources;
using Stellar.Tools.Diagnostics;

var app = new CommandApp();

app.Configure(config =>
{
    config.SetApplicationName("stellar");
    config.PropagateExceptions();

    config.AddCommand<InfoCommand>("info")
        .WithDescription("Show the installation");

    config.AddCommand<VersionCommand>("version")
        .WithDescription("Show component versions");

    config.AddCommand<DoctorCommand>("doctor")
        .WithDescription("Check that the installation is healthy");

    config.AddCommand<InstallCommand>("install")
        .WithDescription("Create or update an installation");

    config.AddBranch("modules", modules =>
    {
        modules.SetDescription("Engine modules");

        modules.AddCommand<ListModulesCommand>("list")
            .WithDescription("List the modules of the installation in load order");

        modules.AddCommand<SyncModulesCommand>("sync")
            .WithDescription("Find the modules, apply the manifest, check the rules and regenerate the catalog");

        modules.AddCommand<NewModuleCommand>("new")
            .WithDescription("Create a module from a template");

        modules.AddCommand<ListModuleTemplatesCommand>("templates")
            .WithDescription("List the module templates");

        modules.AddCommand<AddModuleCommand>("add")
            .WithDescription("Make a module folder outside modules/ part of the installation");

        modules.AddCommand<ResetModuleCommand>("reset")
            .WithDescription("Remove what the manifest changes in a module");

        modules.AddCommand<EnableModuleCommand>("enable")
            .WithDescription("Turn a module back on");

        modules.AddCommand<DisableModuleCommand>("disable")
            .WithDescription("Turn a module off without removing it");

        modules.AddCommand<BuildModulesCommand>("build")
            .WithDescription("Build modules with their dependencies: native, managed, tests, packages");
    }).WithAlias("module");

    config.AddBranch("project", project =>
    {
        project.SetDescription("Project related commands");

        project.AddCommand<NewProjectCommand>("new")
            .WithDescription("Create a project from a template");

        project.AddCommand<ProjectInfoCommand>("info")
            .WithDescription("Show what a project selects");

        project.AddCommand<GenerateProjectDocsCommand>("generate-docs")
            .WithDescription("Generate Markdown documentation for an assembly");

        project.AddBranch("template", template =>
        {
            template.SetDescription("Project templates");

            template.AddCommand<ListTemplatesCommand>("list")
                .WithDescription("List available project templates");
        });
    }).WithAlias("proj");

    config.AddBranch("resources", resources =>
    {
        resources.SetDescription("Resource pipeline");

        resources.AddCommand<CompileResourcesCommand>("compile")
            .WithDescription("Compile the resources of a project");
    });
});

try
{
    return app.Run(args);
}
catch (ToolException exception)
{
    AnsiConsole.MarkupLineInterpolated($"[red]{exception.Message}[/]");
    return 1;
}
catch (CommandParseException exception)
{
    AnsiConsole.MarkupLineInterpolated($"[red]{exception.Message}[/]");
    return 2;
}
catch (CommandRuntimeException exception)
{
    AnsiConsole.MarkupLineInterpolated($"[red]{exception.Message}[/]");
    return 2;
}
