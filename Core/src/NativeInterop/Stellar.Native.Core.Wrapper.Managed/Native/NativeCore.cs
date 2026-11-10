using Stellar.Kernel.Versioning;

namespace Stellar.Core.Native;

/// <summary>
/// Information about the wrapper of the native core this process uses.
/// </summary>
public static unsafe class NativeCore
{
    /// <summary>
    /// The name of this wrapper.
    /// </summary>
    public const string WrapperName = "Managed";

    /// <summary>
    /// The version of the native exports this wrapper was written for.
    /// </summary>
    internal static readonly ContractVersion RequiredAbi = new(2, 0);

    /// <summary>
    /// Gets the name of the wrapper: <c>Managed</c> (P/Invoke) or <c>Cli</c> (C++/CLI).
    /// </summary>
    public static string Wrapper => WrapperName;

    /// <summary>
    /// Gets a value indicating whether the native libraries can be loaded.
    /// </summary>
    public static bool IsAvailable
    {
        get
        {
            try
            {
                EnsureAvailable();
                return true;
            }
            catch (DllNotFoundException)
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Fails if the native libraries are missing or incompatible with this wrapper.
    /// </summary>
    /// <exception cref="DllNotFoundException">Thrown if the native libraries are missing.</exception>
    /// <exception cref="Kernel.Interop.NativeCoreException">Thrown if the versions are incompatible.</exception>
    public static void EnsureAvailable()
    {
        ContractVersion version;
        Exports.Check(Exports.stellar_core_get_abi_version(&version));

        if (!version.Satisfies(RequiredAbi))
            throw new Kernel.Interop.NativeCoreException(
                Kernel.Interop.NativeResult.VersionMismatch,
                $"Native exports are {version}, {RequiredAbi} is required");
    }
}
