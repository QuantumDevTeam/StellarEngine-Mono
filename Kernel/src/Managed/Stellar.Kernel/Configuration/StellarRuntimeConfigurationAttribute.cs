using System;

namespace Stellar.Kernel.Configuration;

/// <summary>
/// Tells the runtime where the runtime configuration of this assembly is: the assembly describes itself, so whoever
/// loads it needs nothing but the assembly.
/// </summary>
/// <remarks>
/// Stellar.Sdk stamps it on every assembly it generates a runtime configuration for. The arguments are strings so
/// that it can be written as an <c>AssemblyAttribute</c> item of MSBuild.
/// </remarks>
[AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
public sealed class StellarRuntimeConfigurationAttribute
    : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StellarRuntimeConfigurationAttribute"/> class.
    /// </summary>
    /// <param name="name">
    /// For <see cref="RuntimeConfigurationSource.External"/>, the file, relative to the assembly. For
    /// <see cref="RuntimeConfigurationSource.Embedded"/>, the name of the manifest resource.
    /// </param>
    /// <param name="source">The name of a value of <see cref="RuntimeConfigurationSource"/>.</param>
    /// <exception cref="ArgumentException">Thrown if <paramref name="source"/> is not a known value.</exception>
    public StellarRuntimeConfigurationAttribute(string name, string source)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Source = Parse(source);
    }

    /// <summary>
    /// Gets the file or the resource of the configuration.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets where the configuration is kept.
    /// </summary>
    public RuntimeConfigurationSource Source { get; }

    private static RuntimeConfigurationSource Parse(string source)
    {
        foreach (RuntimeConfigurationSource value in Enum.GetValues(typeof(RuntimeConfigurationSource)))
            if (string.Equals(value.ToString(), source, StringComparison.OrdinalIgnoreCase))
                return value;

        throw new ArgumentException($"Unknown runtime configuration source '{source}'", nameof(source));
    }
}
