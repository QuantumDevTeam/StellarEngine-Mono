using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Stellar.Tools.Diagnostics;

namespace Stellar.Tools.Solutions;

/// <summary>
/// Reads the projects a project references, as its build sees them.
/// </summary>
public interface IProjectReferenceReader
{
    /// <summary>
    /// Gets the full paths of the projects a project references in Debug.
    /// </summary>
    /// <param name="projectPath">The project.</param>
    /// <returns>The referenced projects.</returns>
    IReadOnlyList<string> Read(string projectPath);
}

/// <summary>
/// Reads references by asking MSBuild: <c>dotnet msbuild -getItem:ProjectReference</c>, which evaluates the project
/// with the Stellar SDK as a build does, and builds nothing.
/// </summary>
public sealed class MSBuildProjectReferenceReader
    : IProjectReferenceReader
{
    /// <inheritdoc/>
    public IReadOnlyList<string> Read(string projectPath)
    {
        var start = new ProcessStartInfo("dotnet",
            $"msbuild \"{projectPath}\" -getItem:ProjectReference -p:Configuration=Debug -nologo")
        {
            WorkingDirectory = Path.GetDirectoryName(projectPath)!,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(start) ?? throw new ToolException("Cannot start dotnet");
        var error = process.StandardError.ReadToEndAsync();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
            throw new ToolException($"MSBuild could not read {projectPath}: {output}{error.Result}".Trim());

        var json = output.Substring(Math.Max(0, output.IndexOf('{')));
        if (JObject.Parse(json)["Items"]?["ProjectReference"] is not JArray references) return [];

        return references
            .Select(reference => reference["FullPath"]?.ToString() ?? reference["Identity"]?.ToString())
            .OfType<string>()
            .Select(path => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(projectPath)!, path)))
            .ToList();
    }
}

/// <summary>
/// Makes the solution of a game or a library: its projects at the root, and the engine they are built against in
/// solution folders, so an IDE shows the sources of the engine apart from the game.
/// </summary>
/// <remarks>
/// <para>The engine projects are what the build references (in Debug, the engine is built from its sources): the
/// projects of the core (Kernel, Core, Runtime, the wrapper) go to the folder <c>Engine</c>, the modules of the
/// installation to <c>Modules</c>. They are built by Debug only: Release takes the engine from its packages.</para>
/// <para>With packages only, nothing is referenced as sources and the folders are left out.</para>
/// </remarks>
/// <param name="stellarRoot">The root of the installation: what is under it is the engine.</param>
/// <param name="references">Reads the references of a project.</param>
/// <param name="logger">Tells what happens.</param>
public sealed class SolutionGenerator(string stellarRoot, IProjectReferenceReader references, IToolLogger logger)
{
    /// <summary>The solution folder of the projects of the core.</summary>
    public const string EngineFolder = "Engine";

    /// <summary>The solution folder of the modules of the installation.</summary>
    public const string ModulesFolder = "Modules";

    private readonly string _root = Normalize(stellarRoot);

    /// <summary>
    /// Writes a solution.
    /// </summary>
    /// <param name="solutionPath">The path of the .sln file.</param>
    /// <param name="projects">The projects of the solution itself, with their own folders if any.</param>
    /// <returns>The projects written, the engine with them.</returns>
    public IReadOnlyList<SolutionProject> Generate(string solutionPath, IReadOnlyList<SolutionProject> projects)
    {
        var all = new List<SolutionProject>(projects);
        var known = new HashSet<string>(projects.Select(project => Normalize(project.Path)), StringComparer.OrdinalIgnoreCase);
        var pending = new Queue<string>(projects.Select(project => project.Path));

        while (pending.Count > 0)
        {
            var project = pending.Dequeue();
            IReadOnlyList<string> referenced;
            try
            {
                referenced = references.Read(project);
            }
            catch (ToolException exception)
            {
                // The solution is still useful without the engine in it: say so and go on
                logger.Warning($"The references of {Path.GetFileName(project)} are not in the solution: {exception.Message}");
                continue;
            }

            foreach (var reference in referenced)
            {
                if (!known.Add(Normalize(reference)) || !File.Exists(reference)) continue;

                all.Add(new SolutionProject(reference, FolderOf(reference), BuildsInRelease: FolderOf(reference) is null));
                pending.Enqueue(reference);
            }
        }

        SolutionFile.Write(solutionPath, all);
        logger.Verbose($"Created {solutionPath}");
        return all;
    }

    private string? FolderOf(string project)
    {
        var path = Normalize(project);
        if (!path.StartsWith(_root, StringComparison.OrdinalIgnoreCase)) return null;

        return path.Substring(_root.Length).StartsWith("modules" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            ? ModulesFolder
            : EngineFolder;
    }

    private static string Normalize(string path)
    {
        var full = Path.GetFullPath(path);
        return Directory.Exists(full) ? full.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar : full;
    }
}
