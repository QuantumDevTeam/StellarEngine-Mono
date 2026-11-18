using System;
using Stellar.Kernel.Data.Catalog;
using Stellar.Kernel.Quantization;

namespace Stellar.Kernel.Failures;

/// <summary>
/// The failures of one engine: what is done about them, and who hears of them.
/// </summary>
/// <remarks>
/// <para>Each engine instance has its own, published in its services; nothing of it is static. Catchers report to the
/// system of the current engine (<see cref="Catcher"/>), the engine reports its own failures there too.</para>
/// <para>The reaction to a failure comes from two catalogs: by kind and level (<see cref="KindReactions"/>), then by
/// level (<see cref="Reactions"/>), then <see cref="FailureReactions.Default"/>.</para>
/// <para>Every member is safe to call from any thread.</para>
/// </remarks>
public interface IFailureSystem
    : IQuantumObject
{
    /// <summary>
    /// Gets the reactions, by level.
    /// </summary>
    ICatalog<FailureLevel, FailureReaction> Reactions { get; }

    /// <summary>
    /// Gets the reactions to a level of one kind, which win over <see cref="Reactions"/>: a module that wants its
    /// errors only logged, for instance.
    /// </summary>
    ICatalog<(FailureKind Kind, FailureLevel Level), FailureReaction> KindReactions { get; }

    /// <summary>
    /// Gets what would be done about a failure.
    /// </summary>
    /// <param name="failure">The failure.</param>
    /// <returns>The reaction.</returns>
    FailureReaction ReactionTo(Failure failure);

    /// <summary>
    /// Reports a failure: decides the reaction, logs it if the reaction says so, tells the handlers, asks the engine to
    /// stop if the reaction says so. A failure already reported is not reported again.
    /// </summary>
    /// <param name="failure">The failure.</param>
    /// <returns>The reaction: the one decided now, or the one of the first report.</returns>
    FailureReaction Report(Failure failure);

    /// <summary>
    /// Makes a handler hear of the failures that pass a filter.
    /// </summary>
    /// <param name="handler">The handler.</param>
    /// <param name="filter">Which failures; all of them by default.</param>
    /// <returns>Disposing it stops the handler from hearing.</returns>
    IDisposable AddHandler(IFailureHandler handler, FailureFilter filter = default);
}

/// <summary>
/// Hears of failures: shows them, sends them, counts them. What is done about them is the reaction of the system.
/// </summary>
public interface IFailureHandler
{
    /// <summary>
    /// Hears of a reported failure, its reaction decided. Called from the thread that reported it; a handler that
    /// throws is logged and skipped.
    /// </summary>
    /// <param name="failure">The failure.</param>
    void Handle(Failure failure);
}

/// <summary>
/// Which failures a handler hears of.
/// </summary>
/// <param name="MinimumLevel">The least serious level heard.</param>
/// <param name="Kind">The one kind heard; <c>null</c> for every kind.</param>
public readonly record struct FailureFilter(FailureLevel MinimumLevel = FailureLevel.NonCritical, FailureKind? Kind = null)
{
    /// <summary>
    /// Gets whether a failure passes the filter.
    /// </summary>
    /// <param name="failure">The failure.</param>
    /// <returns><c>true</c> if it does.</returns>
    public bool Matches(Failure failure)
    {
        return failure.Level >= MinimumLevel && (Kind is null || Kind.Value == failure.Kind);
    }
}
