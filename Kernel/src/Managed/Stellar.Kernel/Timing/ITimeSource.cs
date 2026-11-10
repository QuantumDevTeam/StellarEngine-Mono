namespace Stellar.Kernel.Timing;

/// <summary>
/// Where time comes from: a monotonic reading of real time. Every clock of the engine is derived from one.
/// </summary>
/// <remarks>The machine by default; a test, a replay or a recording gives another.</remarks>
public interface ITimeSource
{
    /// <summary>
    /// Gets the seconds since the source started. Never goes back.
    /// </summary>
    double Now { get; }
}
