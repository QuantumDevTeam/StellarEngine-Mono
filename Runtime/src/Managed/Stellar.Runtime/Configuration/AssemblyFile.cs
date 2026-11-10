using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Stellar.Kernel.Configuration;

namespace Stellar.Runtime.Configuration;

/// <summary>
/// Reads what an assembly says about its runtime configuration from the file, without loading the assembly: the
/// runtime knows how to load an application before any of it is loaded.
/// </summary>
public static class AssemblyFile
{
    private static readonly string AttributeNamespace = typeof(StellarRuntimeConfigurationAttribute).Namespace!;
    private static readonly string AttributeName = nameof(StellarRuntimeConfigurationAttribute);

    /// <summary>
    /// Reads the <see cref="StellarRuntimeConfigurationAttribute"/> of an assembly.
    /// </summary>
    /// <param name="path">The assembly.</param>
    /// <returns>The attribute, or <c>null</c> if the assembly has none.</returns>
    public static StellarRuntimeConfigurationAttribute? ReadConfigurationAttribute(string path)
    {
        using var stream = File.OpenRead(path);
        using var pe = new PEReader(stream);
        var metadata = pe.GetMetadataReader();

        foreach (var handle in metadata.GetAssemblyDefinition().GetCustomAttributes())
        {
            var attribute = metadata.GetCustomAttribute(handle);
            if (!IsConfigurationAttribute(metadata, attribute)) continue;

            var blob = metadata.GetBlobReader(attribute.Value);
            if (blob.ReadUInt16() != 1) continue;

            var name = blob.ReadSerializedString() ?? string.Empty;
            var source = blob.ReadSerializedString() ?? string.Empty;
            return new StellarRuntimeConfigurationAttribute(name, source);
        }

        return null;
    }

    /// <summary>
    /// Reads a manifest resource of an assembly.
    /// </summary>
    /// <param name="path">The assembly.</param>
    /// <param name="name">The name of the resource.</param>
    /// <returns>The content, or <c>null</c> if the assembly has no such resource.</returns>
    public static byte[]? ReadResource(string path, string name)
    {
        using var stream = File.OpenRead(path);
        using var pe = new PEReader(stream);
        var metadata = pe.GetMetadataReader();

        foreach (var handle in metadata.ManifestResources)
        {
            var resource = metadata.GetManifestResource(handle);

            if (!resource.Implementation.IsNil || !metadata.StringComparer.Equals(resource.Name, name)) continue;

            var directory = pe.PEHeaders.CorHeader!.ResourcesDirectory;
            var reader = pe.GetSectionData(directory.RelativeVirtualAddress).GetReader();
            reader.Offset = checked((int)resource.Offset);

            var length = reader.ReadInt32();
            return reader.ReadBytes(length);
        }

        return null;
    }

    private static bool IsConfigurationAttribute(MetadataReader metadata, CustomAttribute attribute)
    {
        EntityHandle type;

        switch (attribute.Constructor.Kind)
        {
            case HandleKind.MemberReference:
                type = metadata.GetMemberReference((MemberReferenceHandle)attribute.Constructor).Parent;
                break;
            case HandleKind.MethodDefinition:
                type = metadata.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor).GetDeclaringType();
                break;
            default:
                return false;
        }

        return type.Kind switch
        {
            HandleKind.TypeReference => Matches(metadata, metadata.GetTypeReference((TypeReferenceHandle)type)),
            HandleKind.TypeDefinition => Matches(metadata, metadata.GetTypeDefinition((TypeDefinitionHandle)type)),
            _ => false
        };
    }

    private static bool Matches(MetadataReader metadata, TypeReference type)
    {
        return metadata.StringComparer.Equals(type.Name, AttributeName) &&
               metadata.StringComparer.Equals(type.Namespace, AttributeNamespace);
    }

    private static bool Matches(MetadataReader metadata, TypeDefinition type)
    {
        return metadata.StringComparer.Equals(type.Name, AttributeName) &&
               metadata.StringComparer.Equals(type.Namespace, AttributeNamespace);
    }
}
