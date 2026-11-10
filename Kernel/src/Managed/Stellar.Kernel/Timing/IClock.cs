using Stellar.Kernel.Quantization;

namespace Stellar.Kernel.Timing;

/// <summary>
/// A clock: a time that runs at its own rate, derived from its parent.
/// </summary>
/// <remarks>
/// <para>Clocks form a tree. The root is the system clock: real time, never scaled, never paused. Below it is the clock
/// of an engine, and below that any clock a module or a game makes: a simulation, the interface, a cutscene. A clock
/// runs at <see cref="Scale"/> times the rate of its parent; a paused clock stops, and every clock below it with it.
/// Pausing the simulation clock stops the simulation, while the main loop and everything on the engine clock go on.</para>
/// <para>The time of a clock is a function of real time, so it can be read at any moment from any thread; the main
/// loop reads it once per frame and gives every participant the step of its own clock
/// (<see cref="Lifecycle.IFrameContext"/>).</para>
/// </remarks>
public interface IClock
    : IQuantumObject
{
    /// <summary>
    /// Gets the name of the clock, unique within its time system.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the clock this one is derived from, or <c>null</c> for the system clock.
    /// </summary>
    IClock? Parent { get; }

    /// <summary>
    /// Gets the seconds this clock has run.
    /// </summary>
    double Time { get; }

    /// <summary>
    /// Gets or sets how fast this clock runs compared with its parent: 1 the same, 0.5 half as fast, 0 frozen
    /// (without being paused). Never negative.
    /// </summary>
    double Scale { get; set; }

    /// <summary>
    /// Gets how fast this clock runs compared with real time: its scale times the rate of its parent, 0 when it
    /// or a clock above it is paused.
    /// </summary>
    double Rate { get; }

    /// <summary>
    /// Gets whether this clock itself is paused.
    /// </summary>
    bool IsPaused { get; }

    /// <summary>
    /// Gets whether this clock runs: neither it nor a clock above it is paused.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Gets or sets the fixed step of this clock, in seconds of this clock: the step of
    /// <see cref="Lifecycle.IFixedUpdatable"/>. 0 takes the fixed step of the parent.
    /// </summary>
    double FixedStep { get; set; }

    /// <summary>
    /// Gets the fixed step in effect: this clock's, or the nearest one above it.
    /// </summary>
    double EffectiveFixedStep { get; }

    /// <summary>
    /// Pauses the clock, and every clock below it.
    /// </summary>
    /// <exception cref="System.InvalidOperationException">Thrown for the system clock.</exception>
    void Pause();

    /// <summary>
    /// Resumes the clock.
    /// </summary>
    void Resume();
}
