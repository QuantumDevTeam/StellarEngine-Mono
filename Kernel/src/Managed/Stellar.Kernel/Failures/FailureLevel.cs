using System;

namespace Stellar.Kernel.Failures;

/// <summary>
/// How serious a failure is: a fixed scale, from the least to the most serious, so levels compare.
/// </summary>
/// <remarks>
/// What the engine does at each level is the reaction of its failure system (<see cref="IFailureSystem.Reactions"/>);
/// <see cref="FailureReactions.Default"/> gives the reactions of an engine that set none.
/// </remarks>
public enum FailureLevel : byte
{
    /// <summary>Something went wrong and nothing depends on it. By default nothing is done.</summary>
    NonCritical,

    /// <summary>Something went wrong and the work goes on. By default it is logged.</summary>
    Warning,

    /// <summary>The work cannot go on. By default it is logged and the work stops.</summary>
    Error,

    /// <summary>The engine cannot go on. By default it is logged, the work stops and the engine stops.</summary>
    CriticalError
}

/// <summary>
/// What a failure system does with a failure.
/// </summary>
[Flags]
public enum FailureReaction : byte
{
    /// <summary>Nothing: the failure is recorded and the work goes on.</summary>
    None = 0,

    /// <summary>The failure is logged.</summary>
    Log = 1 << 0,

    /// <summary>
    /// The work stops: a catcher throws the failure on, as if the call had not been caught, so the whole
    /// <c>try</c> around it stops.
    /// </summary>
    StopExecution = 1 << 1,

    /// <summary>The engine is asked to stop.</summary>
    StopEngine = 1 << 2
}

/// <summary>
/// The reactions of an engine that set none, for each level.
/// </summary>
public static class FailureReactions
{
    /// <summary>
    /// Gets the reaction to a level by default.
    /// </summary>
    /// <param name="level">The level.</param>
    /// <returns>
    /// Nothing for <see cref="FailureLevel.NonCritical"/>; a log for <see cref="FailureLevel.Warning"/>; a log and a
    /// stop of the work for <see cref="FailureLevel.Error"/>; all of it and a stop of the engine for
    /// <see cref="FailureLevel.CriticalError"/>.
    /// </returns>
    public static FailureReaction Default(FailureLevel level)
    {
        return level switch
        {
            FailureLevel.NonCritical => FailureReaction.None,
            FailureLevel.Warning => FailureReaction.Log,
            FailureLevel.Error => FailureReaction.Log | FailureReaction.StopExecution,
            _ => FailureReaction.Log | FailureReaction.StopExecution | FailureReaction.StopEngine
        };
    }

    /// <summary>
    /// Gets whether a reaction stops the work: of the execution or of the engine.
    /// </summary>
    /// <param name="reaction">The reaction.</param>
    /// <returns><c>true</c> if it does.</returns>
    public static bool Stops(this FailureReaction reaction)
    {
        return (reaction & (FailureReaction.StopExecution | FailureReaction.StopEngine)) != 0;
    }
}
