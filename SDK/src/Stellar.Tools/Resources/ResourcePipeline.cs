using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Stellar.Tools.Diagnostics;
using Stellar.Tools.Environment;
using Stellar.Tools.Projects;

namespace Stellar.Tools.Resources;

/// <summary>
/// The outcome of a pipeline run.
/// </summary>
/// <param name="Outputs">Every output file, compiled now or up to date.</param>
/// <param name="Compiled">How many files were compiled.</param>
/// <param name="UpToDate">How many files needed no work.</param>
/// <param name="Removed">How many stale outputs were deleted.</param>
public sealed record ResourcePipelineResult(IReadOnlyList<string> Outputs, int Compiled, int UpToDate, int Removed);

/// <summary>
/// Compiles source files into containers, incrementally.
/// </summary>
/// <remarks>
/// A file is recompiled when its content, its compiler or the version of the compiler changes. The state is
/// kept in a cache file next to the outputs, so deleting the output directory is always a safe full rebuild.
/// </remarks>
public sealed class ResourcePipeline
{
    private sealed class CacheEntry
    {
        public string Hash { get; set; } = string.Empty;

        public string Compiler { get; set; } = string.Empty;

        public int CompilerVersion { get; set; }

        public bool Matches(CacheEntry other)
        {
            return Hash == other.Hash && Compiler == other.Compiler && CompilerVersion == other.CompilerVersion;
        }
    }

    /// <summary>
    /// The name of the cache file.
    /// </summary>
    public const string CacheFileName = ".stellar.resources.cache.json";

    private readonly ResourceCompilerRegistry _compilers;
    private readonly IToolLogger _logger;

    /// <summary>
    /// Creates a pipeline.
    /// </summary>
    /// <param name="compilers">The compilers.</param>
    /// <param name="logger">The output.</param>
    public ResourcePipeline(ResourceCompilerRegistry compilers, IToolLogger logger)
    {
        _compilers = compilers;
        _logger = logger;
    }

    private Dictionary<string, CacheEntry> LoadCache(string path)
    {
        if (!File.Exists(path))
            return new Dictionary<string, CacheEntry>(StringComparer.Ordinal);

        try
        {
            return StellarJson.Read<Dictionary<string, CacheEntry>>(path);
        }
        catch (ToolException exception)
        {
            _logger.Warning($"Resource cache ignored: {exception.Message}");
            return new Dictionary<string, CacheEntry>(StringComparer.Ordinal);
        }
    }

    private static string HashOf(string path)
    {
        using var stream = File.OpenRead(path);
        using var sha = SHA256.Create();

        return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
    }

    private void CompileOne(IResourceCompiler compiler, ProjectFile source, string outputPath)
    {
        _logger.Verbose($"{compiler.Id}: {source.RelativePath}");

        CompiledResource resource;
        try
        {
            resource = compiler.Compile(new ResourceCompileContext(source.FullPath, source.RelativePath, _logger));
        }
        catch (Exception exception) when (exception is not ToolException)
        {
            throw new ToolException($"{compiler.Id} failed on {source.RelativePath}: {exception.Message}", exception);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        using var output = File.Create(outputPath);
        ResourceContainer.Write(output, resource);
    }

    private static int RemoveStale(
        Dictionary<string, CacheEntry> previous, Dictionary<string, CacheEntry> current, string outputDirectory)
    {
        var removed = 0;

        foreach (var relativePath in previous.Keys.Where(path => !current.ContainsKey(path)))
        {
            var stale = Path.Combine(outputDirectory, relativePath + ResourceContainer.Extension);
            if (!File.Exists(stale)) continue;

            File.Delete(stale);
            removed++;
        }

        return removed;
    }


    /// <summary>
    /// Compiles files.
    /// </summary>
    /// <param name="sources">The source files.</param>
    /// <param name="outputDirectory">The directory for the containers. The relative layout of sources is kept.</param>
    /// <returns>The outcome.</returns>
    /// <exception cref="ToolException">Thrown if a compiler fails.</exception>
    public ResourcePipelineResult Compile(IEnumerable<ProjectFile> sources, string outputDirectory)
    {
        var cachePath = Path.Combine(outputDirectory, CacheFileName);
        var previous = LoadCache(cachePath);
        var current = new Dictionary<string, CacheEntry>(StringComparer.Ordinal);
        var outputs = new List<string>();
        int compiled = 0, upToDate = 0;

        foreach (var source in sources)
        {
            var compiler = _compilers.Find(source.FullPath);
            var outputPath = Path.Combine(outputDirectory, source.RelativePath + ResourceContainer.Extension);
            var entry = new CacheEntry
            {
                Hash = HashOf(source.FullPath),
                Compiler = compiler.Id,
                CompilerVersion = compiler.Version
            };

            current[source.RelativePath] = entry;
            outputs.Add(outputPath);

            if (previous.TryGetValue(source.RelativePath, out var cached) && cached.Matches(entry) &&
                File.Exists(outputPath))
            {
                upToDate++;
                continue;
            }

            CompileOne(compiler, source, outputPath);
            compiled++;
        }

        var removed = RemoveStale(previous, current, outputDirectory);

        Directory.CreateDirectory(outputDirectory);
        StellarJson.Write(cachePath, current);

        _logger.Info($"Resources: {compiled} compiled, {upToDate} up to date, {removed} removed");
        return new ResourcePipelineResult(outputs, compiled, upToDate, removed);
    }
}
