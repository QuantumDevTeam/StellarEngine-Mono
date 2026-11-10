using System.Collections.Generic;
using Stellar.Kernel.Quantization;
using Stellar.Kernel.Timing;

namespace Stellar.Kernel.Lifecycle;

/// <summary>
/// How a loop gets its frames.
/// </summary>
public enum MainLoopThreading
{
    /// <summary>
    /// On the thread of another loop: each frame of that loop ends with a frame of this one.
    /// </summary>
    Attached,

    /// <summary>
    /// On a thread of its own, at its own rate.
    /// </summary>
    Dedicated
}

/// <summary>
/// How to create a loop.
/// </summary>
public sealed record MainLoopOptions
{
    /// <summary>
    /// Gets the name of the loop, unique within its engine.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets how the loop gets its frames.
    /// </summary>
    public MainLoopThreading Threading { get; init; } = MainLoopThreading.Attached;

    /// <summary>
    /// Gets the loop an attached loop runs in; the primary loop by default.
    /// </summary>
    public IMainLoop? Host { get; init; }

    /// <summary>
    /// Gets the clock of the loop; a new clock below the engine clock, named after the loop, by default.
    /// </summary>
    public IClock? Clock { get; init; }

    /// <summary>
    /// Gets the frames per second a dedicated loop aims at; 0 for as fast as possible.
    /// </summary>
    public double TargetFrameRate { get; init; }

    /// <summary>
    /// Gets the longest step a frame reports, in real seconds; 0 for no limit.
    /// </summary>
    public double MaxDeltaTime { get; init; } = 0.25;

    /// <summary>
    /// Gets the most fixed steps a participant runs in one frame.
    /// </summary>
    public int MaxFixedSteps { get; init; } = 8;
}

/// <summary>
/// The loops of one engine.
/// </summary>
public interface IMainLoops
    : IQuantumObject
{
    /// <summary>
    /// Gets the primary loop: the one on the main thread of the engine, which runs until the engine stops.
    /// </summary>
    IMainLoop Primary { get; }

    /// <summary>
    /// Gets every loop, the primary one first.
    /// </summary>
    IReadOnlyList<IMainLoop> Loops { get; }

    /// <summary>
    /// Creates a loop. A dedicated loop starts on its thread at once if the engine runs, with the engine otherwise;
    /// every loop stops with the engine.
    /// </summary>
    /// <param name="options">How.</param>
    /// <returns>The loop.</returns>
    /// <exception cref="System.ArgumentException">Thrown if the name is taken.</exception>
    IMainLoop Create(MainLoopOptions options);

    /// <summary>
    /// Finds a loop by name.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns>The loop, or <c>null</c>.</returns>
    IMainLoop? Find(string name);

    /// <summary>
    /// Stops and removes a loop. The primary loop cannot be removed.
    /// </summary>
    /// <param name="loop">The loop.</param>
    /// <returns><c>true</c> if it was there.</returns>
    bool Remove(IMainLoop loop);
}
