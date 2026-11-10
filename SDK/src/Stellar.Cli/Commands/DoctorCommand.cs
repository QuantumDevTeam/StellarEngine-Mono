using Spectre.Console;
using Spectre.Console.Cli;
using Stellar.Tools.Building;
using Stellar.Tools.Diagnostics;
using Stellar.Tools.Environment;
using Stellar.Tools.Modules;

namespace Stellar.Cli.Commands;

public sealed class DoctorCommand
    : Command
{
    public override int Execute(CommandContext context)
    {
        var installation = StellarInstallation.TryLocate();
        var healthy = Report("Installation found", installation is not null, installation?.Root);

        if (installation is null)
            return 1;

        healthy &= Report("Sources of the core present", installation.HasCoreSources, installation.Root);
        healthy &= Report("Installed ('stellar install' ran)", installation.IsInstalled, installation.EngineManifest);
        healthy &= Report("Local feed present", Directory.Exists(installation.Packages), installation.Packages);
        healthy &= Report("Module catalog generated", File.Exists(installation.ModuleCatalog),
            installation.ModuleCatalog);
        Report("Native core built", File.Exists(Path.Combine(installation.ArtifactsBin, "Stellar.Native.Core", "Debug",
                                        "Stellar.Native.Core.dll"))
                                    || File.Exists(Path.Combine(installation.ArtifactsBin, "Stellar.Native.Core",
                                        "Release", "Stellar.Native.Core.dll")),
            "scripts/build.ps1", true);
        Report("MSBuild of Visual Studio (native modules)", MSBuildLocator.Find() is not null,
            MSBuildLocator.Find() ?? MSBuildLocator.EnvironmentVariable, true);

        if (installation.IsInstalled)
            try
            {
                var set = ModuleSynchronizer.Load(installation);
                healthy &= Report("Modules are consistent", true,
                    $"{set.LoadOrder.Count} module(s), {set.Resolution.Disabled.Count} disabled");

                foreach (var warning in set.Resolution.Warnings)
                    Report(warning, false, null, true);
            }
            catch (ToolException exception)
            {
                healthy &= Report("Modules are consistent", false, exception.Message);
            }

        return healthy ? 0 : 1;
    }

    private static bool Report(string check, bool passed, string? details, bool optional = false)
    {
        var mark = passed ? "[green]ok[/]  " : optional ? "[yellow]warn[/]" : "[red]FAIL[/]";

        AnsiConsole.MarkupLine($"{mark} {check.EscapeMarkup()} [grey]{(details ?? string.Empty).EscapeMarkup()}[/]");
        return passed;
    }
}
