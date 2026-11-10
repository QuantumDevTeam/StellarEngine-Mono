using Stellar.Kernel.Configuration;

namespace Stellar.Runtime.Configuration;

/// <summary>
/// The configuration is a file next to the assembly.
/// </summary>
public sealed class FileConfigurationSource
    : IRuntimeConfigurationSource
{
    /// <inheritdoc/>
    public RuntimeConfigurationSource Kind => RuntimeConfigurationSource.External;

    /// <inheritdoc/>
    public RuntimeConfiguration Read(string assemblyPath, string name)
    {
        var path = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(assemblyPath))!, name);

        if (!File.Exists(path))
            throw new RuntimeConfigurationException(
                $"The runtime configuration of {Path.GetFileName(assemblyPath)} is missing: {path}");

        try
        {
            return RuntimeConfigurationReader.ReadFile(path);
        }
        catch (Exception exception)
        {
            throw new RuntimeConfigurationException($"Cannot read the runtime configuration {path}", exception);
        }
    }
}
