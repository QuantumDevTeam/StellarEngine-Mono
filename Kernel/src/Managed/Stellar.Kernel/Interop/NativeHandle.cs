using System.Runtime.InteropServices;

namespace Stellar.Kernel.Interop;

/// <summary>
/// Opaque generational handle to an object owned by native code.
/// </summary>
/// <remarks>
/// <para>Raw pointers never cross the module or the managed/native boundary, handles do. The generation is
/// incremented when a slot is reused, so a stale handle is detected instead of touching a foreign object.</para>
/// <para>The layout is blittable and matches <c>StellarHandle</c> of the native Kernel.</para>
/// </remarks>
/// <param name="Index">Slot index inside the owning pool.</param>
/// <param name="Generation">Generation of the slot. Zero means an invalid handle.</param>
[StructLayout(LayoutKind.Sequential)]
public readonly record struct NativeHandle(uint Index, uint Generation)
{
    /// <summary>
    /// The handle that refers to nothing.
    /// </summary>
    public static readonly NativeHandle Invalid = default;

    /// <summary>
    /// Gets a value indicating whether the handle may refer to an object.
    /// </summary>
    public bool IsValid => Generation != 0;
}
