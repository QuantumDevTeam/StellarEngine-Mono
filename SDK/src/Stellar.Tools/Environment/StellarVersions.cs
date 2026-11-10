using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Stellar.Tools.Diagnostics;

namespace Stellar.Tools.Environment;

/// <summary>
/// The versions of the components this build of Tools belongs to, baked in at build time from Data/version.props.
/// </summary>
public static class StellarVersions
{
    private static readonly Dictionary<string, string> Metadata =
        typeof(StellarVersions).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Where(attribute => attribute.Value is not null)
            .GroupBy(attribute => attribute.Key)
            .ToDictionary(group => group.Key, group => group.First().Value!);

    /// <summary>Gets the version of the Stellar Software Kit release.</summary>
    public static string Orchester => Get("StellarOrchesterVersion");

    /// <summary>Gets the version of the Kernel.</summary>
    public static string Kernel => Get("StellarKernelVersion");

    /// <summary>Gets the version of Stellar.Core.</summary>
    public static string Core => Get("StellarCoreVersion");

    /// <summary>Gets the version of Tools.</summary>
    public static string Tools => Get("StellarToolsVersion");

    /// <summary>Gets the version of the SDK.</summary>
    public static string Sdk => Get("StellarSdkVersion");

    /// <summary>Gets the version of the CLI.</summary>
    public static string Cli => Get("StellarCliVersion");

    /// <summary>Gets the default version of the engine modules.</summary>
    public static string Engine => Get("StellarEngineVersion");

    /// <summary>Gets the version of the native C ABI.</summary>
    public static string NativeAbi => Get("StellarNativeAbiVersion");

    private static string Get(string key)
    {
        return Metadata.TryGetValue(key, out var value)
            ? value
            : throw new ToolException($"{key} is missing from the assembly metadata of Stellar.Tools");
    }
}
