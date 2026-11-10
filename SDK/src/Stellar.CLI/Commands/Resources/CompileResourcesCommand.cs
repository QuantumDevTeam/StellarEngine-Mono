using System.ComponentModel;
using Spectre.Console.Cli;
using Stellar.Tools.Environment;
using Stellar.Tools.Projects;
using Stellar.Tools.Resources;

namespace Stellar.Cli.Commands.Resources;

public sealed class CompileResourcesSettings
    : CommandSettings
{
    [CommandArgument(0, "[DIRECTORY]")]
    [Description("The project directory. The current directory when omitted")]
    public string? Directory { get; set; }

    [CommandOption("-o|--output <DIRECTORY>")]
    [Description("The output directory. <PROJECT>/obj/stellar/resources when omitted")]
    public string? Output { get; set; }
}

public sealed class CompileResourcesCommand
    : Command<CompileResourcesSettings>
{
    public override int Execute(CommandContext context, CompileResourcesSettings settings)
    {
        var project = StellarProject.Find(settings.Directory ?? Environment.CurrentDirectory);
        var output = settings.Output ?? Path.Combine(project.Directory, "obj", "stellar", "resources");

        var compilers = ResourceCompilerRegistry.CreateDefault();

        if (StellarInstallation.TryLocate(project.Directory) is { } home)
            compilers.AddFromDirectory(home.ResourceCompilers, ConsoleToolLogger.Instance);

        foreach (var assembly in project.Configuration.Resources?.Compilers ?? [])
            compilers.AddFrom(Path.Combine(project.Directory, assembly), ConsoleToolLogger.Instance);

        new ResourcePipeline(compilers, ConsoleToolLogger.Instance)
            .Compile(ProjectFileResolver.Resolve(project).Resources, output);

        return 0;
    }
}
