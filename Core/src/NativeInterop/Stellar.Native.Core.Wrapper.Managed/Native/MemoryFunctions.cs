using System.Runtime.InteropServices;
using Stellar.Kernel.Interop;
using Stellar.Kernel.Versioning;

namespace Stellar.Core.Native;

/// <summary>
/// The functions of the allocators of the native core (<c>StellarMemoryApi</c>), read once: a block is given and
/// freed by a direct call, without a P/Invoke stub.
/// </summary>
internal static unsafe class MemoryFunctions
{
    private static readonly ContractVersion Required = new(1, 0);

    /// <summary>
    /// Gets the table.
    /// </summary>
    public static readonly Table* Api = Load();

    private static Table* Load()
    {
        NativeCore.EnsureAvailable();

        Table* api;
        Exports.Check(Exports.stellar_memory_get_api(&api));

        if (api->StructSize < (uint)sizeof(Table) || !api->Contract.Satisfies(Required))
            throw new NativeCoreException(NativeResult.VersionMismatch,
                $"The memory contract of the native core is {api->Contract}, {Required} is required");

        return api;
    }

    /// <summary>
    /// Mirrors <c>StellarMemoryApi</c>.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct Table
    {
        public uint StructSize;
        public ContractVersion Contract;
        public delegate* unmanaged[Cdecl]<nint> Create;
        public delegate* unmanaged[Cdecl]<nint, void> Acquire;
        public delegate* unmanaged[Cdecl]<nint, void> Release;
        public delegate* unmanaged[Cdecl]<nint, nuint, nuint, nint> Allocate;
        public delegate* unmanaged[Cdecl]<nint, nint, nuint, nuint, nuint, nint> Reallocate;
        public delegate* unmanaged[Cdecl]<nint, nint, nuint, void> Free;
        public delegate* unmanaged[Cdecl]<nint, NativeMemoryStats*, void> GetStats;
    }
}
