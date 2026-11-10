using System;

namespace Stellar.Kernel.Versioning;

/// <summary>
/// Version of a contract between two independently built parts of the engine.
/// </summary>
/// <remarks>
/// <para>The rule is the same on the managed and on the native side: a provider satisfies a consumer when the
/// major versions are equal and the provider's minor version is not lower than the required one.</para>
/// <para>The layout is blittable and matches <c>StellarContractVersion</c> of the native Kernel.</para>
/// </remarks>
/// <param name="Major">Incremented on every incompatible change.</param>
/// <param name="Minor">Incremented on every backward-compatible extension.</param>
public readonly record struct ContractVersion(uint Major, uint Minor)
    : IComparable<ContractVersion>
{
    /// <summary>
    /// Checks whether a provider of this version satisfies a consumer that requires <paramref name="required"/>.
    /// </summary>
    /// <param name="required">The version requested by the consumer.</param>
    /// <returns><c>true</c> if this version can serve the consumer.</returns>
    public bool Satisfies(ContractVersion required)
    {
        return Major == required.Major && Minor >= required.Minor;
    }

    /// <inheritdoc/>
    public int CompareTo(ContractVersion other)
    {
        var major = Major.CompareTo(other.Major);
        return major != 0 ? major : Minor.CompareTo(other.Minor);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{Major}.{Minor}";
    }
}
