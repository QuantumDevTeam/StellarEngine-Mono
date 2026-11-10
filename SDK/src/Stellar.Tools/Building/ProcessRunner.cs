using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Stellar.Tools.Diagnostics;

namespace Stellar.Tools.Building;

/// <summary>
/// Runs a program. An abstraction, so build plans can be checked without building anything.
/// </summary>
public interface IProcessRunner
{
    /// <summary>
    /// Runs a program to its end.
    /// </summary>
    /// <param name="fileName">The program.</param>
    /// <param name="arguments">Its arguments, each passed as one.</param>
    /// <param name="workingDirectory">The directory it runs in.</param>
    /// <returns>The exit code.</returns>
    int Run(string fileName, IReadOnlyList<string> arguments, string workingDirectory);
}

/// <summary>
/// Runs programs for real, forwarding their output to a logger.
/// </summary>
public sealed class ProcessRunner
    : IProcessRunner
{
    private readonly IToolLogger _logger;

    /// <summary>
    /// Creates a runner.
    /// </summary>
    public ProcessRunner(IToolLogger logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public int Run(string fileName, IReadOnlyList<string> arguments, string workingDirectory)
    {
        var start = new ProcessStartInfo(fileName, string.Join(" ", arguments.Select(Quote)))
        {
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = new Process { StartInfo = start };
        process.OutputDataReceived += (_, line) =>
        {
            if (line.Data is not null) _logger.Verbose(line.Data);
        };
        process.ErrorDataReceived += (_, line) =>
        {
            if (line.Data is not null) _logger.Error(line.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        return process.ExitCode;
    }

    private static string Quote(string argument)
    {
        return argument.Length != 0 && argument.IndexOfAny(new[] { ' ', '\t', '"' }) < 0
            ? argument
            : "\"" + argument.Replace("\"", "\\\"") + "\"";
    }
}
