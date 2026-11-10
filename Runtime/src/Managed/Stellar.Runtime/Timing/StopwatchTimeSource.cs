using System.Diagnostics;
using Stellar.Kernel.Timing;

namespace Stellar.Runtime.Timing;

/// <summary>
/// The time of the machine.
/// </summary>
public sealed class StopwatchTimeSource
    : ITimeSource
{
    private readonly long _start = Stopwatch.GetTimestamp();

    /// <inheritdoc/>
    public double Now => Stopwatch.GetElapsedTime(_start).TotalSeconds;
}
