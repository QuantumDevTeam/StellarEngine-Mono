using Spectre.Console;
using Stellar.Tools.Diagnostics;

namespace Stellar.Cli;

/// <summary>
/// Shows tool messages in the console.
/// </summary>
internal sealed class ConsoleToolLogger
    : IToolLogger
{
    public static ConsoleToolLogger Instance { get; } = new();

    public void Log(ToolMessageLevel level, string message)
    {
        switch (level)
        {
            case ToolMessageLevel.Verbose:
                AnsiConsole.MarkupLineInterpolated($"[grey]{message}[/]");
                break;
            case ToolMessageLevel.Warning:
                AnsiConsole.MarkupLineInterpolated($"[yellow]{message}[/]");
                break;
            case ToolMessageLevel.Error:
                AnsiConsole.MarkupLineInterpolated($"[red]{message}[/]");
                break;
            case ToolMessageLevel.Info:
            default:
                AnsiConsole.WriteLine(message);
                break;
        }
    }
}
