using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using Stellar.Tools.Diagnostics;
using Stellar.Tools.Environment;

namespace Stellar.Cli.Commands;

public sealed class VersionSettings
    : CommandSettings
{
    [CommandArgument(0, "[COMPONENT]")]
    [Description("orchester, kernel, core, tools, sdk, cli, engine or abi. All of them when omitted")]
    public string? Component { get; set; }
}

public sealed class VersionCommand
    : Command<VersionSettings>
{
    private static readonly Dictionary<string, Func<string>> Components = new(StringComparer.OrdinalIgnoreCase)
    {
        ["orchester"] = () => StellarVersions.Orchester,
        ["kernel"] = () => StellarVersions.Kernel,
        ["core"] = () => StellarVersions.Core,
        ["tools"] = () => StellarVersions.Tools,
        ["sdk"] = () => StellarVersions.Sdk,
        ["cli"] = () => StellarVersions.Cli,
        ["engine"] = () => StellarVersions.Engine,
        ["abi"] = () => StellarVersions.NativeAbi
    };

    public override int Execute(CommandContext context, VersionSettings settings)
    {
        if (settings.Component is { } component)
        {
            if (!Components.TryGetValue(component, out var version))
                throw new ToolException($"Unknown component '{component}'");

            AnsiConsole.WriteLine(version());
            return 0;
        }

        var table = new Table().AddColumn("Component").AddColumn("Version");

        foreach (var (name, version) in Components)
            table.AddRow(name, version());

        AnsiConsole.Write(table);
        return 0;
    }
}
