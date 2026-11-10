using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Stellar.Tools.Diagnostics;

namespace Stellar.Tools.Resources;

/// <summary>
/// The set of compilers a pipeline works with.
/// </summary>
public sealed class ResourceCompilerRegistry
{
    private readonly List<IResourceCompiler> _compilers = [];

    /// <summary>
    /// Gets the compilers, the most recently added first.
    /// </summary>
    public IReadOnlyList<IResourceCompiler> Compilers => _compilers;

    /// <summary>
    /// Creates a registry that knows only the raw fallback.
    /// </summary>
    public static ResourceCompilerRegistry CreateDefault()
    {
        var registry = new ResourceCompilerRegistry();
        registry.Add(new RawResourceCompiler());
        return registry;
    }

    /// <summary>
    /// Adds a compiler. Later additions win over earlier ones.
    /// </summary>
    /// <param name="compiler">The compiler.</param>
    public void Add(IResourceCompiler compiler)
    {
        _compilers.Insert(0, compiler);
    }

    /// <summary>
    /// Finds the compiler for a file.
    /// </summary>
    /// <param name="path">The source file.</param>
    /// <returns>The compiler.</returns>
    /// <exception cref="ToolException">Thrown if no compiler handles the file.</exception>
    public IResourceCompiler Find(string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();

        return _compilers.FirstOrDefault(compiler => compiler.CanCompile(extension))
               ?? throw new ToolException($"No resource compiler handles {path}");
    }

    /// <summary>
    /// Adds every compiler of an assembly.
    /// </summary>
    /// <param name="assemblyPath">The assembly.</param>
    /// <param name="logger">The output.</param>
    /// <exception cref="ToolException">Thrown if the assembly cannot be loaded.</exception>
    public void AddFrom(string assemblyPath, IToolLogger logger)
    {
        Assembly assembly;
        try
        {
            assembly = Assembly.LoadFrom(assemblyPath);
        }
        catch (Exception exception) when (exception is IOException or BadImageFormatException)
        {
            throw new ToolException($"Cannot load resource compilers from {assemblyPath}: {exception.Message}",
                exception);
        }

        var types = assembly.GetExportedTypes()
            .Where(type => typeof(IResourceCompiler).IsAssignableFrom(type)
                           && type is { IsAbstract: false, IsInterface: false }
                           && type.GetConstructor(Type.EmptyTypes) is not null);

        foreach (var type in types)
        {
            Add((IResourceCompiler)Activator.CreateInstance(type)!);
            logger.Verbose($"Resource compiler {type.FullName} loaded from {assemblyPath}");
        }
    }

    /// <summary>
    /// Adds the compilers of every assembly in a directory. A missing directory is not an error.
    /// </summary>
    /// <param name="directory">The directory.</param>
    /// <param name="logger">The output.</param>
    public void AddFromDirectory(string directory, IToolLogger logger)
    {
        if (!Directory.Exists(directory)) return;

        foreach (var assembly in Directory.GetFiles(directory, "*.dll"))
            AddFrom(assembly, logger);
    }
}
