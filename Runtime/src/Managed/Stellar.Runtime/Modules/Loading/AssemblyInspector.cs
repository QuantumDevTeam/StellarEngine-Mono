using System.Reflection.PortableExecutable;

namespace Stellar.Runtime.Modules.Loading;

/// <summary>
/// Reads what an assembly file is, without loading it.
/// </summary>
public static class AssemblyInspector
{
    /// <summary>
    /// Gets whether an assembly holds native code (C++/CLI). Such assemblies live in the default context only.
    /// </summary>
    /// <param name="path">The assembly.</param>
    /// <returns><c>true</c> if it is not IL only.</returns>
    public static bool IsMixedMode(string path)
    {
        using var stream = File.OpenRead(path);
        using var reader = new PEReader(stream);

        var header = reader.PEHeaders.CorHeader;
        return header is not null && (header.Flags & CorFlags.ILOnly) == 0;
    }
}
