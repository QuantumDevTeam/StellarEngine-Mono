using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Stellar.Tools.Diagnostics;
using Stellar.Tools.Environment;

namespace Stellar.Tools.Templates;

/// <summary>
/// What to do with a file a template would write over.
/// </summary>
public enum TemplateConflict
{
    /// <summary>The target folder must be empty or missing.</summary>
    RequireEmpty,

    /// <summary>Existing files are kept as they are: the template only adds what is missing.</summary>
    KeepExisting,

    /// <summary>Existing files are replaced: a template layered over its base.</summary>
    Overwrite
}

/// <summary>
/// The optional <c>.template.json</c> of a template folder.
/// </summary>
public sealed class TemplateManifest
{
    /// <summary>
    /// The file name. The file itself is never copied.
    /// </summary>
    public const string FileName = ".template.json";

    /// <summary>Gets or sets what the template is for, as the listing shows it.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the template, of the same folder, rendered first: this one only adds and replaces files.
    /// </summary>
    public string? Base { get; set; }
}

/// <summary>
/// Copies a template folder: <c>{{Name}}</c> placeholders are replaced in the content, <c>__Name__</c> ones in the
/// paths. Every template of <c>Shared/Templates</c> goes through here: projects, modules and the installation.
/// </summary>
public sealed class TemplateRenderer
{
    private readonly IToolLogger _logger;

    /// <summary>
    /// Creates a renderer.
    /// </summary>
    /// <param name="logger">The output.</param>
    public TemplateRenderer(IToolLogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Lists the templates of a folder: one subfolder each.
    /// </summary>
    public static IReadOnlyList<string> List(string directory)
    {
        return Directory.Exists(directory)
            ? Directory.GetDirectories(directory).Select(Path.GetFileName).OrderBy(name => name, StringComparer.Ordinal)
                .ToList()!
            : Array.Empty<string>();
    }

    /// <summary>
    /// Renders a template.
    /// </summary>
    /// <param name="templateDirectory">The template.</param>
    /// <param name="outputDirectory">Where to write.</param>
    /// <param name="values">The placeholder values.</param>
    /// <param name="conflict">What to do with existing files.</param>
    /// <returns>The files written.</returns>
    /// <exception cref="ToolException">Thrown if the template is missing, or the target is not empty when it must be.</exception>
    public IReadOnlyList<string> Render(
        string templateDirectory,
        string outputDirectory,
        IReadOnlyDictionary<string, string> values,
        TemplateConflict conflict = TemplateConflict.RequireEmpty)
    {
        if (!Directory.Exists(templateDirectory))
            throw new ToolException($"Template not found: {templateDirectory}");

        var manifest = ReadManifest(templateDirectory);
        if (manifest.Base is { Length: > 0 } baseName)
        {
            var written = new List<string>(Render(
                BaseDirectory(templateDirectory, baseName), outputDirectory, values, conflict));

            written.AddRange(RenderFiles(templateDirectory, outputDirectory, values,
                conflict == TemplateConflict.KeepExisting
                    ? TemplateConflict.KeepExisting
                    : TemplateConflict.Overwrite));
            return written.Distinct().ToList();
        }

        return RenderFiles(templateDirectory, outputDirectory, values, conflict);
    }

    /// <summary>
    /// Tells what a template would write, without writing anything: the files of its base first, then its own,
    /// a later file replacing an earlier one with the same path.
    /// </summary>
    /// <param name="templateDirectory">The template.</param>
    /// <param name="outputDirectory">Where it would write.</param>
    /// <param name="values">The placeholder values.</param>
    /// <returns>The target paths and their content.</returns>
    /// <exception cref="ToolException">Thrown if the template is missing.</exception>
    public static IReadOnlyDictionary<string, string> Preview(
        string templateDirectory,
        string outputDirectory,
        IReadOnlyDictionary<string, string> values)
    {
        if (!Directory.Exists(templateDirectory))
            throw new ToolException($"Template not found: {templateDirectory}");

        var files = new Dictionary<string, string>(StringComparer.Ordinal);
        var manifest = ReadManifest(templateDirectory);
        if (manifest.Base is { Length: > 0 } baseName)
            foreach (var pair in Preview(BaseDirectory(templateDirectory, baseName), outputDirectory, values))
                files[pair.Key] = pair.Value;

        foreach (var (target, content) in Plan(templateDirectory, outputDirectory, values))
            files[target] = content;

        return files;
    }

    /// <summary>
    /// Reads the <c>.template.json</c> of a template, or an empty one.
    /// </summary>
    public static TemplateManifest ReadManifest(string templateDirectory)
    {
        var path = Path.Combine(templateDirectory, TemplateManifest.FileName);
        return File.Exists(path) ? StellarJson.Read<TemplateManifest>(path) : new TemplateManifest();
    }

    private IReadOnlyList<string> RenderFiles(
        string templateDirectory,
        string outputDirectory,
        IReadOnlyDictionary<string, string> values,
        TemplateConflict conflict)
    {
        if (conflict == TemplateConflict.RequireEmpty
            && Directory.Exists(outputDirectory)
            && Directory.EnumerateFileSystemEntries(outputDirectory).Any())
            throw new ToolException($"{outputDirectory} already exists and is not empty");

        var written = new List<string>();

        foreach (var (target, content) in Plan(templateDirectory, outputDirectory, values))
        {
            if (conflict == TemplateConflict.KeepExisting && File.Exists(target))
            {
                _logger.Verbose($"Kept {target}");
                continue;
            }

            FileWriter.WriteAllText(target, content);
            _logger.Verbose($"Created {target}");
            written.Add(target);
        }

        return written;
    }

    private static IEnumerable<(string Target, string Content)> Plan(
        string templateDirectory,
        string outputDirectory,
        IReadOnlyDictionary<string, string> values)
    {
        var prefix = Path.GetFullPath(templateDirectory).TrimEnd(Path.DirectorySeparatorChar) +
                     Path.DirectorySeparatorChar;

        foreach (var source in Directory.GetFiles(templateDirectory, "*", SearchOption.AllDirectories)
                     .OrderBy(path => path, StringComparer.Ordinal))
        {
            var relative = Path.GetFullPath(source).Substring(prefix.Length);
            if (string.Equals(relative, TemplateManifest.FileName, StringComparison.Ordinal))
                continue;

            yield return (Path.Combine(outputDirectory, Substitute(relative, values, "__", "__")),
                Substitute(File.ReadAllText(source), values, "{{", "}}"));
        }
    }

    private static string BaseDirectory(string templateDirectory, string baseName)
    {
        return Path.Combine(
            Path.GetDirectoryName(Path.GetFullPath(templateDirectory).TrimEnd(Path.DirectorySeparatorChar))!,
            baseName);
    }

    private static string Substitute(string text, IReadOnlyDictionary<string, string> values, string opening,
        string closing)
    {
        return values.Aggregate(text, (current, pair) => current.Replace(opening + pair.Key + closing, pair.Value));
    }
}
