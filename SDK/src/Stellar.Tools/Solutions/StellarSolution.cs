using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Stellar.Tools.Diagnostics;
using Stellar.Tools.Environment;

namespace Stellar.Tools.Solutions;

/// <summary>
/// The content of a <c>.stellar.solution</c> file: what a solution of Stellar projects says about itself.
/// </summary>
public sealed class StellarSolutionFile
{
    /// <summary>
    /// Gets or sets how the projects take the engine: <c>Packages</c> (the default), or <c>Sources</c> to work on the
    /// engine itself, built from its sources in Debug and listed in the solution. The Directory.Build.props of the
    /// solution reads it for MSBuild.
    /// </summary>
    public string? Engine { get; set; }

    /// <summary>
    /// Gets or sets how <c>stellar run</c> runs the solution.
    /// </summary>
    public RunConfiguration? Run { get; set; }

    /// <summary>
    /// Gets or sets folders of modules of the solution outside its directory, relative to it. The modules below the
    /// directory of the solution are found without being listed.
    /// </summary>
    public List<string> Modules { get; set; } = [];
}

/// <summary>
/// How <c>stellar run</c> runs a solution.
/// </summary>
public sealed class RunConfiguration
{
    /// <summary>
    /// Gets or sets what runs without a name: a profile, or a project.
    /// </summary>
    public string? Default { get; set; }

    /// <summary>
    /// Gets or sets the profiles, by name.
    /// </summary>
    public Dictionary<string, RunProfile> Profiles { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// One way to run: which projects, how they are built, what they get.
/// </summary>
public sealed class RunProfile
{
    /// <summary>
    /// Gets or sets the project to run: a path relative to the solution, or the name of a project in it.
    /// </summary>
    public string? Project { get; set; }

    /// <summary>
    /// Gets or sets several projects to run at once, each in a process of its own: a server and its clients.
    /// </summary>
    public List<string> Projects { get; set; } = [];

    /// <summary>
    /// Gets or sets the configuration to build; Debug if <c>null</c>.
    /// </summary>
    public string? Configuration { get; set; }

    /// <summary>
    /// Gets or sets the arguments given to the application.
    /// </summary>
    public List<string> Arguments { get; set; } = [];

    /// <summary>
    /// Gets or sets environment variables of the processes.
    /// </summary>
    public Dictionary<string, string> Environment { get; set; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets or sets MSBuild properties of the build: <c>"StellarNativeWrapper": "Managed"</c>.
    /// </summary>
    public Dictionary<string, string> Properties { get; set; } = new(StringComparer.Ordinal);
}

/// <summary>
/// A solution of Stellar projects: a directory with a <c>.stellar.solution</c> file, next to its <c>.sln</c>.
/// </summary>
public sealed class StellarSolution
{
    /// <summary>The extension of the file.</summary>
    public const string Extension = ".stellar.solution";

    private StellarSolution(string name, string directory, StellarSolutionFile file)
    {
        Name = name;
        Directory = directory;
        File = file;
    }

    /// <summary>Gets the name: the file name without its extension.</summary>
    public string Name { get; }

    /// <summary>Gets the directory of the solution.</summary>
    public string Directory { get; }

    /// <summary>Gets what the file says.</summary>
    public StellarSolutionFile File { get; }

    /// <summary>
    /// Reads a solution file.
    /// </summary>
    public static StellarSolution Load(string path)
    {
        var full = Path.GetFullPath(path);
        var fileName = Path.GetFileName(full);
        var name = fileName.Substring(0, fileName.Length - Extension.Length);
        return new StellarSolution(name, Path.GetDirectoryName(full)!, StellarJson.Read<StellarSolutionFile>(full));
    }

    /// <summary>
    /// Writes the .sln again: the projects in the directory of the solution, and the engine they reference as sources
    /// in solution folders. What changed in the solution (a project added, "Engine" switched) is then in it; folders
    /// made by hand in the IDE are not kept.
    /// </summary>
    /// <param name="generator">Writes the solution.</param>
    /// <returns>The path of the .sln.</returns>
    public string Sync(SolutionGenerator generator)
    {
        var projects = RunPlanner.ProjectsIn(Directory).Select(path => new SolutionProject(path)).ToList();
        if (projects.Count == 0) throw new ToolException($"No project in {Directory}");

        var path = Path.Combine(Directory, Name + SolutionFile.Extension);
        generator.Generate(path, projects);
        return path;
    }

    /// <summary>
    /// Finds the solution a directory is in: its own, or the first one above it.
    /// </summary>
    /// <param name="directory">Where to start.</param>
    /// <returns>The solution, or <c>null</c> if there is none.</returns>
    public static StellarSolution? Find(string directory)
    {
        for (var current = new DirectoryInfo(Path.GetFullPath(directory)); current is not null; current = current.Parent)
        {
            var files = current.GetFiles("*" + Extension);
            if (files.Length == 1) return Load(files[0].FullName);
            if (files.Length > 1)
                throw new ToolException($"Several solutions in {current.FullName}: name the one to use");
        }

        return null;
    }
}
