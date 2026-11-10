using Stellar.Runtime.Failures;

namespace Stellar.Runtime.Lifecycle;

/// <summary>
/// A step of the life of the engine: it starts in order with the others, and stops in the reverse order.
/// </summary>
/// <remarks>
/// A stage that started is always stopped, even when a later one fails; a stage that failed to start is stopped too,
/// so that it takes down what it had set up so far.
/// </remarks>
public interface IRuntimeStage
{
    /// <summary>
    /// Gets the name of the stage.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the level that catches the start of the stage.
    /// </summary>
    CatchLevel CatchLevel { get; }

    /// <summary>
    /// Starts the stage.
    /// </summary>
    /// <param name="context">The runtime.</param>
    /// <returns>A task that completes when the stage is up.</returns>
    ValueTask StartAsync(RuntimeContext context);

    /// <summary>
    /// Stops the stage: undoes what <see cref="StartAsync"/> did, as far as it got.
    /// </summary>
    /// <param name="context">The runtime.</param>
    /// <returns>A task that completes when the stage is down.</returns>
    ValueTask StopAsync(RuntimeContext context);
}
