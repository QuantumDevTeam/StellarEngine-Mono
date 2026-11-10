using System;
using System.Collections.ObjectModel;
using Stellar.Kernel.Quantization;

namespace Stellar.Kernel.FileSystem;

/// <summary>
/// A resource served by the engine file system.
/// </summary>
public interface IResource
    : IMetaQuant, IEquatable<IResource>
{
    /// <summary>Gets the file reference (domain + path).</summary>
    IFile File { get; }

    /// <summary>Gets the raw loaded bytes.</summary>
    ReadOnlyCollection<byte> Data { get; }
}
