using System;
using System.IO;
using System.Linq;
using Stellar.Kernel.Modules;
using Stellar.Tools.Diagnostics;
using Stellar.Tools.Environment;

namespace Stellar.Tools.Modules;

/// <summary>
/// Reads and writes <c>&lt;Id&gt;.stellar.module</c> files.
/// </summary>
public static class ModuleDescriptorFile
{
    /// <summary>
    /// Reads a descriptor and checks that it names its module the way its file does.
    /// </summary>
    /// <param name="path">The descriptor file.</param>
    /// <returns>The descriptor.</returns>
    /// <exception cref="ToolException">Thrown if the file is missing, malformed or disagrees with its name.</exception>
    public static ModuleDescriptor Read(string path)
    {
        var descriptor = StellarJson.Read<ModuleDescriptor>(path);
        var expected = Path.GetFileName(path);

        // The file name is the identifier: a folder of modules can be listed without opening any file
        if (string.IsNullOrWhiteSpace(descriptor.Id))
            throw new ToolException($"{path} has no Id");

        if (!string.Equals(ModuleConventions.DescriptorFileName(descriptor.Id), expected, StringComparison.Ordinal))
            throw new ToolException(
                $"{path} describes {descriptor.Id}: the file must be named {ModuleConventions.DescriptorFileName(descriptor.Id)}");

        return descriptor;
    }

    /// <summary>
    /// Finds the descriptor of the module that lives in a folder.
    /// </summary>
    /// <param name="directory">The module folder.</param>
    /// <returns>The descriptor file, or <c>null</c> if the folder is not a module.</returns>
    /// <exception cref="ToolException">Thrown if the folder holds more than one descriptor.</exception>
    public static string? Find(string directory)
    {
        if (!Directory.Exists(directory))
            return null;

        var files = Directory.GetFiles(directory, "*" + ModuleDescriptor.FileExtension, SearchOption.TopDirectoryOnly);

        return files.Length switch
        {
            0 => null,
            1 => files[0],
            _ => throw new ToolException(
                $"{directory} holds several module descriptors: {string.Join(", ", files.Select(Path.GetFileName))}")
        };
    }

    /// <summary>
    /// Writes a descriptor next to the module, named after it.
    /// </summary>
    /// <param name="directory">The module folder.</param>
    /// <param name="descriptor">The descriptor.</param>
    /// <returns>The file written.</returns>
    public static string Write(string directory, ModuleDescriptor descriptor)
    {
        var path = Path.Combine(directory, ModuleConventions.DescriptorFileName(descriptor.Id));
        StellarJson.Write(path, descriptor);
        return path;
    }
}
