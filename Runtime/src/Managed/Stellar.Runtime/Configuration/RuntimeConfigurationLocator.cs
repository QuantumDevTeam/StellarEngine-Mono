using Stellar.Kernel.Configuration;

namespace Stellar.Runtime.Configuration;

/// <summary>
/// Finds the runtime configuration of an assembly: the assembly says where it is
/// (<see cref="StellarRuntimeConfigurationAttribute"/>), a source of that kind reads it. The assembly is not loaded.
/// </summary>
public sealed class RuntimeConfigurationLocator
{
    private readonly Dictionary<RuntimeConfigurationSource, IRuntimeConfigurationSource> _sources = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="RuntimeConfigurationLocator"/> class.
    /// </summary>
    /// <param name="sources">The sources; by default a file and an embedded resource. A later one of a kind wins.</param>
    public RuntimeConfigurationLocator(IEnumerable<IRuntimeConfigurationSource>? sources = null)
    {
        foreach (var source in sources ?? [new FileConfigurationSource(), new EmbeddedConfigurationSource()])
            _sources[source.Kind] = source;
    }

    /// <summary>
    /// Reads the runtime configuration of an assembly.
    /// </summary>
    /// <param name="assemblyPath">The assembly.</param>
    /// <returns>The configuration.</returns>
    /// <exception cref="RuntimeConfigurationException">
    /// Thrown if the assembly is missing, has no configuration, or it cannot be read.
    /// </exception>
    public RuntimeConfiguration Read(string assemblyPath)
    {
        if (!File.Exists(assemblyPath))
            throw new RuntimeConfigurationException($"The application is missing: {assemblyPath}");

        var attribute = AssemblyFile.ReadConfigurationAttribute(assemblyPath)
                        ?? throw new RuntimeConfigurationException(
                            $"{Path.GetFileName(assemblyPath)} has no runtime configuration: it is not built by Stellar.Sdk " +
                            "with a .stellar.project file");

        if (!_sources.TryGetValue(attribute.Source, out var source))
            throw new RuntimeConfigurationException(
                $"No source can read a runtime configuration of kind {attribute.Source}");

        return source.Read(assemblyPath, attribute.Name);
    }
}
