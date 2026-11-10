using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace Stellar.Runtime.Modules.Loading;

/// <summary>
/// Finds the files of an application: its <c>.deps.json</c> says where every assembly and native library is, the
/// folder of the application is the fallback.
/// </summary>
public sealed class ApplicationAssemblyResolver
{
    private readonly AssemblyDependencyResolver? _dependencies;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationAssemblyResolver"/> class.
    /// </summary>
    /// <param name="applicationPath">The main assembly of the application.</param>
    public ApplicationAssemblyResolver(string applicationPath)
    {
        ApplicationPath = Path.GetFullPath(applicationPath);
        Directory = Path.GetDirectoryName(ApplicationPath)!;

        try
        {
            _dependencies = new AssemblyDependencyResolver(ApplicationPath);
        }
        catch (InvalidOperationException)
        {
            // No .deps.json, or one the host cannot read: the folder alone
            _dependencies = null;
        }
    }

    /// <summary>
    /// Gets the main assembly of the application.
    /// </summary>
    public string ApplicationPath { get; }

    /// <summary>
    /// Gets the folder of the application.
    /// </summary>
    public string Directory { get; }

    /// <summary>
    /// Finds an assembly.
    /// </summary>
    /// <param name="name">The assembly.</param>
    /// <returns>Its path, or <c>null</c>.</returns>
    public string? ResolveAssembly(AssemblyName name)
    {
        if (_dependencies?.ResolveAssemblyToPath(name) is { } path && File.Exists(path))
            return path;

        var local = Path.Combine(Directory, name.Name + ".dll");
        return File.Exists(local) ? local : null;
    }

    /// <summary>
    /// Finds a native library.
    /// </summary>
    /// <param name="name">The library, as <c>DllImport</c> names it.</param>
    /// <returns>Its path, or <c>null</c>.</returns>
    public string? ResolveNativeLibrary(string name)
    {
        if (_dependencies?.ResolveUnmanagedDllToPath(name) is { } path && File.Exists(path))
            return path;

        var file = Path.HasExtension(name) ? name : name + NativeExtension;

        foreach (var candidate in new[]
                 {
                     Path.Combine(Directory, file),
                     Path.Combine(Directory, "runtimes", RuntimeInformation.RuntimeIdentifier, "native", file)
                 })
            if (File.Exists(candidate))
                return candidate;

        return null;
    }

    private static string NativeExtension =>
        OperatingSystem.IsWindows() ? ".dll" : OperatingSystem.IsMacOS() ? ".dylib" : ".so";
}
