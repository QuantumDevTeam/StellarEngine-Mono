using System.Collections.Generic;
using Stellar.Kernel.Quantization;

namespace Stellar.Kernel.Timing;

/// <summary>
/// The clocks of one engine.
/// </summary>
/// <remarks>
/// Every engine instance has its own: two engines in one process (an editor and the game it runs) pause, scale and
/// step their time apart. Safe to use from any thread.
/// </remarks>
public interface ITimeSystem
    : IQuantumObject
{
    /// <summary>
    /// The name of the system clock.
    /// </summary>
    public const string SystemClockName = "System";

    /// <summary>
    /// The name of the engine clock.
    /// </summary>
    public const string EngineClockName = "Engine";

    /// <summary>
    /// Gets where the time comes from.
    /// </summary>
    ITimeSource Source { get; }

    /// <summary>
    /// Gets the system clock: real time, the root of every clock.
    /// </summary>
    IClock System { get; }

    /// <summary>
    /// Gets the clock of the engine: the parent of the clocks of modules and games, and the clock of the main loop
    /// unless it is given another. Pausing it pauses the engine time, not the main loop.
    /// </summary>
    IClock Engine { get; }

    /// <summary>
    /// Gets every clock, the system and engine clocks included.
    /// </summary>
    IReadOnlyCollection<IClock> Clocks { get; }

    /// <summary>
    /// Creates a clock.
    /// </summary>
    /// <param name="name">The name, unique within this system.</param>
    /// <param name="parent">The parent; the engine clock by default.</param>
    /// <param name="scale">How fast it runs compared with its parent.</param>
    /// <returns>The clock, at time 0.</returns>
    /// <exception cref="System.ArgumentException">Thrown if the name is taken, or the parent belongs to another system.</exception>
    IClock CreateClock(string name, IClock? parent = null, double scale = 1);

    /// <summary>
    /// Finds a clock by name.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns>The clock, or <c>null</c>.</returns>
    IClock? Find(string name);

    /// <summary>
    /// Removes a clock, and the clocks below it.
    /// </summary>
    /// <param name="clock">The clock. The system and engine clocks cannot be removed.</param>
    /// <returns><c>true</c> if it was there.</returns>
    bool Remove(IClock clock);
}
