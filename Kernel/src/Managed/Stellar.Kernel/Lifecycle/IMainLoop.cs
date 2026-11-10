using System;
using Stellar.Kernel.Quantization;
using Stellar.Kernel.Threading;
using Stellar.Kernel.Timing;

namespace Stellar.Kernel.Lifecycle;

/// <summary>
/// A main loop: a service that runs frames, phase by phase, on one thread.
/// </summary>
/// <remarks>
/// <para>Everything that runs per frame runs in a loop, the engine itself included: its systems are participants like
/// the ones of a game, added with a lower order. There may be many loops (<see cref="IMainLoops"/>): a loop of their
/// own for the systems that need another rate or another thread, a loop per engine instance.</para>
/// <para>Every frame: the work posted to the thread of the loop, the timers that are due, the participants added or
/// removed since the previous frame, then the phases of <see cref="FramePhase"/> in order, then the loops attached
/// to this one. A participant takes part in the phases of the interfaces it implements (<see cref="IUpdatable"/>,
/// <see cref="IFixedUpdatable"/>, <see cref="IRenderable"/>); a participant whose clock is paused is skipped.</para>
/// <para>Adding, removing, scheduling and pausing are safe from any thread: changes take effect at the start of the
/// next frame.</para>
/// </remarks>
public interface IMainLoop
    : IQuantumObject
{
    /// <summary>
    /// Gets the name of the loop, unique within its engine.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the clock of the loop: the clock of the participants added without one.
    /// </summary>
    IClock Clock { get; }

    /// <summary>
    /// Gets the dispatcher of the thread of the loop.
    /// </summary>
    IMainThreadDispatcher Dispatcher { get; }

    /// <summary>
    /// Gets the state of the loop.
    /// </summary>
    EngineState State { get; }

    /// <summary>
    /// Gets the index of the current frame.
    /// </summary>
    ulong FrameIndex { get; }

    /// <summary>
    /// Adds a participant.
    /// </summary>
    /// <param name="participant">
    /// The participant: an <see cref="IUpdatable"/>, an <see cref="IFixedUpdatable"/>, an <see cref="IRenderable"/>, or
    /// several of them.
    /// </param>
    /// <param name="clock">The clock it follows; the clock of the loop by default.</param>
    /// <param name="order">Lower runs first in every phase; equal orders run in the order they were added.</param>
    /// <exception cref="ArgumentException">Thrown if the participant takes part in no phase.</exception>
    void Add(IQuant participant, IClock? clock = null, int order = 0);

    /// <summary>
    /// Removes a participant.
    /// </summary>
    /// <param name="participant">The participant.</param>
    /// <returns><c>true</c> if it was there.</returns>
    bool Remove(IQuant participant);

    /// <summary>
    /// Schedules work on a clock, run on the thread of the loop.
    /// </summary>
    /// <param name="delay">The seconds of the clock before it runs.</param>
    /// <param name="callback">The work.</param>
    /// <param name="clock">The clock; the clock of the loop by default.</param>
    /// <param name="period">Seconds of the clock between two runs, or 0 to run once.</param>
    /// <returns>The timer.</returns>
    ITimer Schedule(double delay, Action callback, IClock? clock = null, double period = 0);

    /// <summary>
    /// Pauses the loop: frames go on, posted work still runs, the phases are skipped. The clocks are not paused.
    /// </summary>
    void Pause();

    /// <summary>
    /// Resumes a paused loop.
    /// </summary>
    void Resume();

    /// <summary>
    /// Stops the loop. Stopping the primary loop of an engine stops the engine.
    /// </summary>
    void RequestStop();
}
