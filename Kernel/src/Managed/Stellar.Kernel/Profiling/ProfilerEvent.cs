using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Stellar.Kernel.Profiling;

/// <summary>
/// What a profiler event says.
/// </summary>
public enum ProfilerEventKind : ushort
{
    /// <summary>A marked span of work starts on this thread.</summary>
    Begin,

    /// <summary>The last span of the same marker that started on this thread ends.</summary>
    End,

    /// <summary>Something happened at one moment: a failure, a load, a message.</summary>
    Instant,

    /// <summary>A counter has a value: <see cref="ProfilerEvent.Value"/>.</summary>
    Counter,

    /// <summary>A frame of a loop starts: the marker is the loop, <see cref="ProfilerEvent.Value"/> its frame index.</summary>
    Frame
}

/// <summary>
/// One event of a profiler: 24 bytes, blittable, written by C# and by C++ in the same form.
/// </summary>
/// <remarks>
/// <para>The layout matches <c>StellarProfilerEvent</c> of the native Kernel (<c>Abi/Profiling.h</c>): a profiler keeps
/// the events of each thread in a native list, and native code writes into the same lists.</para>
/// <para>The time is in ticks of the high-resolution clock of the platform, <see cref="Stopwatch.GetTimestamp"/>, the
/// clock native code reads too (<c>QueryPerformanceCounter</c>, <c>CLOCK_MONOTONIC</c>);
/// <see cref="Stopwatch.Frequency"/> ticks make a second.</para>
/// </remarks>
/// <param name="Timestamp">When, in ticks of the clock.</param>
/// <param name="Marker">The marker, the loop or the counter: an identifier of <see cref="ProfilerMarkers"/>.</param>
/// <param name="Kind">What happened.</param>
/// <param name="Value">The value of a counter, the index of a frame, or what an instant carries; 0 otherwise.</param>
[StructLayout(LayoutKind.Sequential)]
public readonly record struct ProfilerEvent(long Timestamp, int Marker, ProfilerEventKind Kind, long Value = 0)
{
    /// <summary>
    /// Makes an event of now.
    /// </summary>
    public static ProfilerEvent Now(int marker, ProfilerEventKind kind, long value = 0)
    {
        return new ProfilerEvent(Stopwatch.GetTimestamp(), marker, kind, value);
    }
}
