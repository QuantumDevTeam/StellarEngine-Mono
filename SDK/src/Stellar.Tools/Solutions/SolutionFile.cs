using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Stellar.Tools.Solutions;

/// <summary>
/// A project of a solution, with the solution folder it sits in.
/// </summary>
/// <param name="Path">The full path of the project file.</param>
/// <param name="Folder">The solution folder; <c>null</c> at the root of the solution.</param>
/// <param name="BuildsInRelease">Whether the solution builds it in Release; engine sources are referenced only by Debug.</param>
public sealed record SolutionProject(string Path, string? Folder = null, bool BuildsInRelease = true);

/// <summary>
/// Writes a classic Visual Studio solution (.sln): projects at the root or in solution folders, Debug and Release.
/// </summary>
/// <remarks>
/// The identifiers of projects and folders are made from their paths and names, so writing the same solution again
/// gives the same file: a solution under version control does not change for nothing.
/// </remarks>
public static class SolutionFile
{
    /// <summary>The extension of a solution.</summary>
    public const string Extension = ".sln";

    private const string CSharpProjectType = "{9A19103F-16F7-4668-BE54-9A1E7A4F7556}";
    private const string FolderType = "{2150E333-8FDC-42A3-9474-1A3956D46DE8}";
    private static readonly string[] Configurations = ["Debug", "Release"];

    /// <summary>
    /// Writes a solution.
    /// </summary>
    /// <param name="path">The path of the .sln file.</param>
    /// <param name="projects">The projects, in the order they appear.</param>
    public static void Write(string path, IReadOnlyList<SolutionProject> projects)
    {
        File.WriteAllText(path, Render(Path.GetDirectoryName(Path.GetFullPath(path))!, projects), new UTF8Encoding(true));
    }

    /// <summary>
    /// Renders a solution as text.
    /// </summary>
    /// <param name="directory">The directory of the solution: project paths are written relative to it.</param>
    /// <param name="projects">The projects, in the order they appear.</param>
    /// <returns>The text of the .sln file.</returns>
    public static string Render(string directory, IReadOnlyList<SolutionProject> projects)
    {
        var text = new StringBuilder();
        text.Append("\r\nMicrosoft Visual Studio Solution File, Format Version 12.00\r\n");
        text.Append("# Visual Studio Version 17\r\n");
        text.Append("VisualStudioVersion = 17.0.31903.59\r\n");
        text.Append("MinimumVisualStudioVersion = 10.0.40219.1\r\n");

        var folders = projects.Select(project => project.Folder).OfType<string>().Distinct(StringComparer.Ordinal).ToList();
        var ids = projects.ToDictionary(project => project, project => IdOf("project:" + Relative(directory, project.Path)));

        foreach (var project in projects)
        {
            var name = Path.GetFileNameWithoutExtension(project.Path);
            text.Append($"Project(\"{CSharpProjectType}\") = \"{name}\", \"{Relative(directory, project.Path)}\", \"{ids[project]}\"\r\nEndProject\r\n");
        }

        foreach (var folder in folders)
            text.Append($"Project(\"{FolderType}\") = \"{folder}\", \"{folder}\", \"{IdOf("folder:" + folder)}\"\r\nEndProject\r\n");

        text.Append("Global\r\n");
        text.Append("\tGlobalSection(SolutionConfigurationPlatforms) = preSolution\r\n");
        foreach (var configuration in Configurations)
            text.Append($"\t\t{configuration}|Any CPU = {configuration}|Any CPU\r\n");
        text.Append("\tEndGlobalSection\r\n");

        text.Append("\tGlobalSection(ProjectConfigurationPlatforms) = postSolution\r\n");
        foreach (var project in projects)
        foreach (var configuration in Configurations)
        {
            text.Append($"\t\t{ids[project]}.{configuration}|Any CPU.ActiveCfg = {configuration}|Any CPU\r\n");
            if (configuration != "Release" || project.BuildsInRelease)
                text.Append($"\t\t{ids[project]}.{configuration}|Any CPU.Build.0 = {configuration}|Any CPU\r\n");
        }

        text.Append("\tEndGlobalSection\r\n");
        text.Append("\tGlobalSection(SolutionProperties) = preSolution\r\n");
        text.Append("\t\tHideSolutionNode = FALSE\r\n");
        text.Append("\tEndGlobalSection\r\n");

        if (folders.Count > 0)
        {
            text.Append("\tGlobalSection(NestedProjects) = preSolution\r\n");
            foreach (var project in projects.Where(project => project.Folder is not null))
                text.Append($"\t\t{ids[project]} = {IdOf("folder:" + project.Folder)}\r\n");
            text.Append("\tEndGlobalSection\r\n");
        }

        text.Append("EndGlobal\r\n");
        return text.ToString();
    }

    /// <summary>
    /// Gets a path as the solution writes it: relative to its directory when they share a root, with backslashes.
    /// </summary>
    public static string Relative(string directory, string path)
    {
        var from = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var to = Path.GetFullPath(path);

        if (!string.Equals(Path.GetPathRoot(from), Path.GetPathRoot(to), StringComparison.OrdinalIgnoreCase))
            return to.Replace('/', '\\');

        var relative = Uri.UnescapeDataString(new Uri(from).MakeRelativeUri(new Uri(to)).ToString());
        return relative.Replace('/', '\\');
    }

    // The same name always gives the same identifier
    private static string IdOf(string key)
    {
        using var md5 = MD5.Create();
        var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(key.ToUpperInvariant()));
        return new Guid(bytes).ToString("B").ToUpperInvariant();
    }
}
