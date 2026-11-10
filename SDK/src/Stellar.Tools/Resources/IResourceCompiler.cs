using System;
using System.IO;
using Stellar.Tools.Diagnostics;

namespace Stellar.Tools.Resources;

/// <summary>
/// Input of a resource compiler.
/// </summary>
/// <param name="SourcePath">The absolute path of the source file.</param>
/// <param name="RelativePath">The path of the source relative to the project.</param>
/// <param name="Logger">The output.</param>
public sealed record ResourceCompileContext(string SourcePath, string RelativePath, IToolLogger Logger);

/// <summary>
/// Turns one kind of source file into the engine format.
/// </summary>
/// <remarks>
/// <para>This is the extension point of the resource pipeline. High-Level Modules ship their compilers
/// (models for graphics, sounds for audio) as assemblies, the pipeline itself knows no format.</para>
/// <para>A compiler runs the same way from an MSBuild task, from the CLI and from the Editor.</para>
/// </remarks>
public interface IResourceCompiler
{
    /// <summary>
    /// Gets the unique name of the compiler.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the version of the output. Increasing it invalidates everything compiled by previous versions.
    /// </summary>
    int Version { get; }

    /// <summary>
    /// Checks whether this compiler handles a source file.
    /// </summary>
    /// <param name="extension">The extension of the file, with the dot, in lower case.</param>
    bool CanCompile(string extension);

    /// <summary>
    /// Compiles a file.
    /// </summary>
    /// <param name="context">The input.</param>
    /// <returns>The compiled resource.</returns>
    CompiledResource Compile(ResourceCompileContext context);
}

/// <summary>
/// The fallback compiler: packs the bytes of a file with no processing.
/// </summary>
public sealed class RawResourceCompiler
    : IResourceCompiler
{
    /// <summary>The type of the resources this compiler produces.</summary>
    public static readonly FourCC ResourceType = new("RAW");

    /// <summary>The tag of the chunk with the file content.</summary>
    public static readonly FourCC DataChunk = new("DATA");

    /// <inheritdoc/>
    public string Id => "Stellar.Raw";

    /// <inheritdoc/>
    public int Version => 1;

    /// <inheritdoc/>
    public bool CanCompile(string extension)
    {
        return true;
    }

    /// <inheritdoc/>
    public CompiledResource Compile(ResourceCompileContext context)
    {
        return new CompiledResource(ResourceType,
            new[] { new ResourceChunk(DataChunk, File.ReadAllBytes(context.SourcePath)) });
    }
}
