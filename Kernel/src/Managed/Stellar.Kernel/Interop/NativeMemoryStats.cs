using System.Runtime.InteropServices;

namespace Stellar.Kernel.Interop;

/// <summary>
/// What a native allocator holds now and held so far.
/// </summary>
/// <remarks>The layout is blittable and matches <c>StellarMemoryStats</c> of the native Kernel.</remarks>
/// <param name="AllocatedBytes">Bytes in blocks not freed yet.</param>
/// <param name="LiveAllocations">Blocks not freed yet.</param>
/// <param name="TotalAllocations">Blocks given since the allocator was made.</param>
/// <param name="PeakBytes">The most bytes held at once.</param>
[StructLayout(LayoutKind.Sequential)]
public readonly record struct NativeMemoryStats(ulong AllocatedBytes, ulong LiveAllocations, ulong TotalAllocations, ulong PeakBytes);
