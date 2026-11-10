using Stellar.Kernel.Configuration;

namespace Stellar.Runtime.Configuration;

/// <summary>
/// The configuration is a manifest resource of the assembly.
/// </summary>
public sealed class EmbeddedConfigurationSource
    : IRuntimeConfigurationSource
{
    /// <inheritdoc/>
    public RuntimeConfigurationSource Kind => RuntimeConfigurationSource.Embedded;

    /// <inheritdoc/>
    public RuntimeConfiguration Read(string assemblyPath, string name)
    {
        var content = AssemblyFile.ReadResource(assemblyPath, name)
                      ?? throw new RuntimeConfigurationException(
                          $"{Path.GetFileName(assemblyPath)} has no embedded runtime configuration '{name}'");

        try
        {
            return RuntimeConfigurationReader.Read(new MemoryStream(content, false));
        }
        catch (Exception exception)
        {
            throw new RuntimeConfigurationException(
                $"Cannot read the embedded runtime configuration '{name}' of {Path.GetFileName(assemblyPath)}",
                exception);
        }
    }
}
