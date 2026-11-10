using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using Stellar.Kernel.Modules;
using Stellar.Tools.Building;
using Stellar.Tools.Diagnostics;
using Stellar.Tools.Environment;
using Stellar.Tools.Manifests;
using Stellar.Tools.Modules;

namespace Stellar.Cli.Commands.Modules;

public sealed class ListModulesCommand
    : Command
{
    public override int Execute(CommandContext context)
    {
        var set = ModuleSynchronizer.Load(StellarInstallation.Locate());

        var table = new Table()
            .AddColumn("#").AddColumn("Module").AddColumn("Version").AddColumn("Level")
            .AddColumn("Source").AddColumn("Link").AddColumn("Dependencies");

        for (var index = 0; index < set.LoadOrder.Count; index++)
        {
            var module = set.LoadOrder[index];
            table.AddRow(
                (index + 1).ToString(),
                module.Id.EscapeMarkup(),
                (module.Descriptor.Version ?? string.Empty).EscapeMarkup(),
                module.Descriptor.Level.ToString(),
                module.Source.ToString(),
                module.Link.ToString(),
                string.Join(", ", module.Descriptor.Dependencies).EscapeMarkup());
        }

        AnsiConsole.Write(table);

        foreach (var id in set.Resolution.Disabled)
            AnsiConsole.MarkupLineInterpolated($"[grey]disabled: {id}[/]");

        foreach (var warning in set.Resolution.Warnings)
            AnsiConsole.MarkupLineInterpolated($"[yellow]{warning}[/]");

        return 0;
    }
}

public sealed class SyncModulesCommand
    : Command
{
    public override int Execute(CommandContext context)
    {
        ModuleSynchronizer.Synchronize(StellarInstallation.Locate(), ConsoleToolLogger.Instance);
        return 0;
    }
}

public sealed class NewModuleSettings
    : CommandSettings
{
    [CommandArgument(0, "<TEMPLATE>")]
    [Description("The template, see 'stellar modules templates'")]
    public string Template { get; set; } = string.Empty;

    [CommandArgument(1, "<ID>")]
    [Description("The module identifier, e.g. Stellar.Logging")]
    public string Id { get; set; } = string.Empty;

    [CommandOption("-l|--level <LEVEL>")]
    [Description("CoreModule, Runtime, HighLevel or EndPoint. HighLevel by default")]
    [DefaultValue(ModuleLevel.HighLevel)]
    public ModuleLevel Level { get; set; } = ModuleLevel.HighLevel;

    [CommandOption("-d|--description <TEXT>")]
    [Description("What the module is for, in one sentence")]
    public string? Description { get; set; }

    [CommandOption("-o|--output <DIRECTORY>")]
    [Description(
        "Where to create it. modules/<ID> of the installation by default; elsewhere, it is added to the manifest")]
    public string? Output { get; set; }
}

public sealed class NewModuleCommand
    : Command<NewModuleSettings>
{
    public override int Execute(CommandContext context, NewModuleSettings settings)
    {
        if (settings.Level is ModuleLevel.Kernel or ModuleLevel.Core)
            throw new ToolException(
                $"{settings.Level} is the core itself: a module is CoreModule, Runtime, HighLevel or EndPoint");

        var installation = StellarInstallation.Locate();
        var directory = new ModuleScaffolder(installation, ConsoleToolLogger.Instance)
            .Create(new ModuleRequest(settings.Id, settings.Level, settings.Description, settings.Template,
                settings.Output));

        // Outside the searched folders the installation would not find it: point the manifest at it
        if (settings.Output is not null)
            ManifestChanges.Apply(installation,
                manifest => EngineManifestEditor.Entry(manifest, settings.Id).Path = directory);
        else
            ModuleSynchronizer.Synchronize(installation, ConsoleToolLogger.Instance);

        return 0;
    }
}

public sealed class ListModuleTemplatesCommand
    : Command
{
    public override int Execute(CommandContext context)
    {
        var scaffolder = new ModuleScaffolder(StellarInstallation.Locate(), ConsoleToolLogger.Instance);

        foreach (var template in scaffolder.ListTemplates())
            AnsiConsole.MarkupLineInterpolated(
                $"- {template} [grey]{scaffolder.DescribeTemplate(template) ?? string.Empty}[/]");

        return 0;
    }
}

public sealed class AddModuleSettings
    : CommandSettings
{
    [CommandArgument(0, "<DIRECTORY>")]
    [Description("The module folder: it holds <Id>.stellar.module")]
    public string Directory { get; set; } = string.Empty;

    [CommandOption("--link <LINK>")]
    [Description("Default, Sources or Package: how projects reference the module")]
    public ModuleLink? Link { get; set; }
}

public sealed class AddModuleCommand
    : Command<AddModuleSettings>
{
    public override int Execute(CommandContext context, AddModuleSettings settings)
    {
        var directory = Path.GetFullPath(settings.Directory);
        var file = ModuleDescriptorFile.Find(directory)
                   ?? throw new ToolException($"{directory} is not a module: it has no .stellar.module file");
        var descriptor = ModuleDescriptorFile.Read(file);
        var installation = StellarInstallation.Locate();

        ModuleFeed.EnsureReachable(installation, directory, ConsoleToolLogger.Instance);
        ManifestChanges.Apply(installation, manifest =>
        {
            var entry = EngineManifestEditor.Entry(manifest, descriptor.Id);
            entry.Path = directory;
            entry.Link = settings.Link ?? entry.Link;
        });

        return 0;
    }
}

public sealed class ModuleIdSettings
    : CommandSettings
{
    [CommandArgument(0, "<ID>")]
    [Description("The module identifier")]
    public string Id { get; set; } = string.Empty;
}

public sealed class ResetModuleCommand
    : Command<ModuleIdSettings>
{
    public override int Execute(CommandContext context, ModuleIdSettings settings)
    {
        ManifestChanges.Apply(StellarInstallation.Locate(), manifest =>
        {
            if (!EngineManifestEditor.Reset(manifest, settings.Id))
                throw new ToolException($"The manifest has no entry for {settings.Id}");
        });

        return 0;
    }
}

public sealed class EnableModuleCommand
    : Command<ModuleIdSettings>
{
    public override int Execute(CommandContext context, ModuleIdSettings settings)
    {
        ManifestChanges.Apply(StellarInstallation.Locate(),
            manifest => EngineManifestEditor.Entry(manifest, settings.Id).Enabled = null);
        return 0;
    }
}

public sealed class DisableModuleCommand
    : Command<ModuleIdSettings>
{
    public override int Execute(CommandContext context, ModuleIdSettings settings)
    {
        ManifestChanges.Apply(StellarInstallation.Locate(),
            manifest => EngineManifestEditor.Entry(manifest, settings.Id).Enabled = false);
        return 0;
    }
}

public sealed class BuildModulesSettings
    : CommandSettings
{
    [CommandArgument(0, "[ID]")]
    [Description("The modules to build, with their dependencies. All of them when omitted")]
    public string[] Ids { get; set; } = [];

    [CommandOption("-c|--configuration <CONFIGURATION>")]
    [DefaultValue("Debug")]
    public string Configuration { get; set; } = "Debug";

    [CommandOption("--wrapper <WRAPPER>")]
    [Description("The wrapper of the native core: Cli or Managed. The default of the SDK when omitted")]
    public string? Wrapper { get; set; }

    [CommandOption("--no-native")]
    [Description("Skip the native projects")]
    public bool NoNative { get; set; }

    [CommandOption("--tests")]
    [Description("Also run the managed tests")]
    public bool Tests { get; set; }

    [CommandOption("--pack")]
    [Description("Also put the packages into the local feed")]
    public bool Pack { get; set; }

    [CommandOption("--plan")]
    [Description("Only show what would run")]
    public bool Plan { get; set; }
}

public sealed class BuildModulesCommand
    : Command<BuildModulesSettings>
{
    public override int Execute(CommandContext context, BuildModulesSettings settings)
    {
        var installation = StellarInstallation.Locate();
        var set = ModuleSynchronizer.Synchronize(installation, ConsoleToolLogger.Instance);
        var options = new ModuleBuildOptions(settings.Configuration, !settings.NoNative, true, settings.Tests,
            settings.Pack, settings.Wrapper);
        var steps = ModuleBuildPlanner.Plan(installation, set, settings.Ids, options,
            settings.NoNative ? null : MSBuildLocator.Find());

        if (settings.Plan)
        {
            foreach (var step in steps)
                AnsiConsole.MarkupLineInterpolated(
                    $"{step.Module}: [grey]{step.FileName} {string.Join(" ", step.Arguments)}[/]");

            return 0;
        }

        new ModuleBuilder(new ProcessRunner(ConsoleToolLogger.Instance), ConsoleToolLogger.Instance).Run(steps,
            installation.Root);
        return 0;
    }
}

/// <summary>
/// Changes the manifest and brings the generated files in line, or changes nothing if the result is inconsistent.
/// </summary>
internal static class ManifestChanges
{
    public static void Apply(StellarInstallation installation,
        Action<Kernel.Configuration.Manifest.EngineManifest> change)
    {
        var original = File.ReadAllText(installation.EngineManifest);
        var manifest = EngineManifestStore.Load(installation.EngineManifest);

        change(manifest);
        EngineManifestEditor.Compact(manifest);
        EngineManifestStore.Save(installation.EngineManifest, manifest);

        try
        {
            ModuleSynchronizer.Synchronize(installation, ConsoleToolLogger.Instance);
        }
        catch (ToolException)
        {
            File.WriteAllText(installation.EngineManifest, original);
            throw;
        }
    }
}
