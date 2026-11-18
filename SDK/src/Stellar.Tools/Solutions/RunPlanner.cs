using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Stellar.Tools.Diagnostics;

namespace Stellar.Tools.Solutions;

/// <summary>
/// What <c>stellar run</c> does: the projects, then the commands that build and run them.
/// </summary>
public sealed class RunPlan
{
    /// <summary>Gets what runs: a profile or a project.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets the projects, full paths.</summary>
    public IReadOnlyList<string> Projects { get; init; } = [];

    /// <summary>Gets the configuration.</summary>
    public string Configuration { get; init; } = "Debug";

    /// <summary>Gets the environment variables of the processes.</summary>
    public IReadOnlyDictionary<string, string> Environment { get; init; } = new Dictionary<string, string>();

    /// <summary>Gets the commands that build, one after another: empty when one project runs, which builds itself.</summary>
    public IReadOnlyList<IReadOnlyList<string>> Builds { get; init; } = [];

    /// <summary>Gets the commands that run, all at once: the arguments of <c>dotnet</c>.</summary>
    public IReadOnlyList<IReadOnlyList<string>> Runs { get; init; } = [];
}

/// <summary>
/// Works out what <c>stellar run</c> runs: from the profiles of the solution, the executable projects in it, or the
/// project of the directory when there is no solution.
/// </summary>
/// <remarks>
/// <para>One project runs with <c>dotnet run</c>, which builds it. Several are built first, one after another, so two
/// builds never write the same engine project at once, then they run at once, each in a process of its own.</para>
/// <para>A name is a profile first, then an executable project. Without a name: the default of the solution, its only
/// profile, or its only executable project.</para>
/// </remarks>
public static class RunPlanner
{
    private static readonly Regex Executable = new(@"<StellarSdkUsage>\s*ExecutableModule\s*</StellarSdkUsage>", RegexOptions.Compiled);
    private static readonly string[] Skipped = ["bin", "obj", ".git", ".vs", ".idea", "artifacts"];

    /// <summary>
    /// Plans a run.
    /// </summary>
    /// <param name="directory">Where <c>stellar run</c> runs.</param>
    /// <param name="target">A profile or a project; the default if <c>null</c>.</param>
    /// <param name="configuration">The configuration, over the one of the profile.</param>
    /// <param name="arguments">Arguments for the application, after those of the profile.</param>
    /// <param name="noBuild">Runs what was built before.</param>
    /// <returns>The plan.</returns>
    public static RunPlan Plan(string directory, string? target = null, string? configuration = null,
        IReadOnlyList<string>? arguments = null, bool noBuild = false)
    {
        arguments ??= [];
        var solution = StellarSolution.Find(directory);

        if (solution is null)
        {
            var project = ProjectAbove(directory);
            if (target is not null && !NameOf(project).Equals(target, StringComparison.OrdinalIgnoreCase))
                throw new ToolException($"No solution here, and the project is {NameOf(project)}, not {target}");

            return Make(NameOf(project), [project], new RunProfile(), configuration, arguments, noBuild);
        }

        var projects = ProjectsIn(solution.Directory);
        var executables = projects.Where(IsExecutable).ToList();
        var profiles = solution.File.Run?.Profiles ?? new Dictionary<string, RunProfile>(StringComparer.OrdinalIgnoreCase);
        var name = target ?? solution.File.Run?.Default;

        if (name is null)
        {
            if (profiles.Count == 1) name = profiles.Keys.Single();
            else if (profiles.Count == 0 && executables.Count == 1) name = NameOf(executables[0]);
            else
                throw new ToolException(
                    $"Name what to run: {Choices(profiles, executables)}, or set Run.Default in {solution.Name}{StellarSolution.Extension}");
        }

        if (profiles.TryGetValue(name, out var profile))
        {
            var entries = profile.Projects.Count > 0 ? profile.Projects : profile.Project is { } one ? [one] : [];
            if (entries.Count == 0) throw new ToolException($"The profile {name} names no project");

            return Make(name, entries.Select(entry => Resolve(solution, projects, entry)).ToList(), profile, configuration,
                arguments, noBuild);
        }

        var executable = executables.FirstOrDefault(path => NameOf(path).Equals(name, StringComparison.OrdinalIgnoreCase));
        return executable is not null
            ? Make(NameOf(executable), [executable], new RunProfile(), configuration, arguments, noBuild)
            : throw new ToolException($"Nothing to run is named {name}: {Choices(profiles, executables)}");
    }

    /// <summary>
    /// Gets whether a project is an executable module: a game, a tool, a server.
    /// </summary>
    public static bool IsExecutable(string projectPath)
    {
        return Executable.IsMatch(File.ReadAllText(projectPath));
    }

    private static RunPlan Make(string name, IReadOnlyList<string> projects, RunProfile profile, string? configuration,
        IReadOnlyList<string> arguments, bool noBuild)
    {
        var chosen = configuration ?? profile.Configuration ?? "Debug";
        var properties = profile.Properties.Select(pair => $"-p:{pair.Key}={pair.Value}").ToList();
        var application = profile.Arguments.Concat(arguments).ToList();
        var buildFirst = projects.Count > 1 && !noBuild;

        var builds = buildFirst
            ? projects.Select(project => (IReadOnlyList<string>)["build", project, "-c", chosen, .. properties]).ToList()
            : [];

        var runs = projects.Select(project =>
        {
            List<string> command = ["run", "--project", project, "-c", chosen, .. properties];
            if (noBuild || buildFirst) command.Add("--no-build");
            if (application.Count > 0) command.AddRange(["--", .. application]);
            return (IReadOnlyList<string>)command;
        }).ToList();

        return new RunPlan
        {
            Name = name,
            Projects = projects,
            Configuration = chosen,
            Environment = profile.Environment,
            Builds = builds,
            Runs = runs
        };
    }

    // A path to a project file, relative to the solution, or the name of a project in it
    private static string Resolve(StellarSolution solution, IReadOnlyList<string> projects, string entry)
    {
        if (entry.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            var path = Path.GetFullPath(Path.Combine(solution.Directory, entry));
            return File.Exists(path) ? path : throw new ToolException($"No project at {entry} in {solution.Name}");
        }

        return projects.FirstOrDefault(path => NameOf(path).Equals(entry, StringComparison.OrdinalIgnoreCase))
               ?? throw new ToolException($"No project {entry} in {solution.Name}");
    }

    /// <summary>
    /// Gets the project files under a directory, but in build output and IDE folders.
    /// </summary>
    public static IReadOnlyList<string> ProjectsIn(string directory)
    {
        var found = new List<string>();
        var pending = new Stack<string>([directory]);
        while (pending.Count > 0)
        {
            var current = pending.Pop();
            found.AddRange(Directory.GetFiles(current, "*.csproj"));
            foreach (var child in Directory.GetDirectories(current))
                if (!Skipped.Contains(Path.GetFileName(child), StringComparer.OrdinalIgnoreCase))
                    pending.Push(child);
        }

        return found.OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static string ProjectAbove(string directory)
    {
        for (var current = new DirectoryInfo(Path.GetFullPath(directory)); current is not null; current = current.Parent)
        {
            var files = current.GetFiles("*.csproj");
            if (files.Length == 1) return files[0].FullName;
            if (files.Length > 1)
                throw new ToolException($"Several projects in {current.FullName} and no {StellarSolution.Extension}: name one in a solution file");
        }

        throw new ToolException($"No project in {directory} or above it");
    }

    private static string Choices(IReadOnlyDictionary<string, RunProfile> profiles, IReadOnlyList<string> executables)
    {
        var names = profiles.Keys.Select(profile => $"{profile} (profile)")
            .Concat(executables.Select(NameOf).Where(name => !profiles.ContainsKey(name)))
            .ToList();
        return names.Count == 0 ? "the solution has no executable project" : string.Join(", ", names);
    }

    private static string NameOf(string projectPath)
    {
        return Path.GetFileNameWithoutExtension(projectPath);
    }
}

/// <summary>
/// Carries out a run plan, attached to the console: the builds one by one, then every run at once.
/// </summary>
public static class RunExecutor
{
    /// <summary>
    /// Runs a plan.
    /// </summary>
    /// <param name="plan">The plan.</param>
    /// <param name="workingDirectory">Where the processes start.</param>
    /// <returns>0 if every process succeeded, otherwise the first exit code that is not 0.</returns>
    public static int Execute(RunPlan plan, string workingDirectory)
    {
        foreach (var build in plan.Builds)
        {
            var code = Start(build, plan.Environment, workingDirectory).WaitAndGet();
            if (code != 0) return code;
        }

        var processes = plan.Runs.Select(run => Start(run, plan.Environment, workingDirectory)).ToList();
        var codes = processes.Select(process => process.WaitAndGet()).ToList();
        return codes.FirstOrDefault(code => code != 0);
    }

    private static Process Start(IReadOnlyList<string> arguments, IReadOnlyDictionary<string, string> environment,
        string workingDirectory)
    {
        var start = new ProcessStartInfo("dotnet", string.Join(" ", arguments.Select(Quote)))
        {
            WorkingDirectory = workingDirectory,
            UseShellExecute = false
        };

        foreach (var pair in environment) start.Environment[pair.Key] = pair.Value;
        return Process.Start(start) ?? throw new ToolException("Cannot start dotnet");
    }

    private static int WaitAndGet(this Process process)
    {
        using (process)
        {
            process.WaitForExit();
            return process.ExitCode;
        }
    }

    private static string Quote(string argument)
    {
        return argument.Length != 0 && argument.IndexOfAny([' ', '\t', '"']) < 0
            ? argument
            : "\"" + argument.Replace("\"", "\\\"") + "\"";
    }
}
