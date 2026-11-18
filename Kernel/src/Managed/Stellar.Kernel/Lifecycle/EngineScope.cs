using System;
using System.Threading;

namespace Stellar.Kernel.Lifecycle;

/// <summary>
/// The engine the running code belongs to.
/// </summary>
/// <remarks>
/// <para>The runtime enters the scope of its engine on every thread it runs engine code on; the scope flows with the
/// execution, into <c>await</c> continuations, <c>Task.Run</c> and new threads. So the parts of the engine that are
/// reached without a context (registries, the services of Stellar.Core) find the right instance, and two engines
/// in one process never see each other's state.</para>
/// <para>Code that has a context uses <see cref="EntryPoint.IRunContext.Engine"/> instead. Outside any engine
/// <see cref="Current"/> is <c>null</c>, and what happens then is <see cref="Detached"/>: tools and tests fall back to
/// the default instance of the process, while a running application, whose code always belongs to an engine, treats
/// it as an error. Code reached without the scope (a native callback, an event of the operating system, work queued
/// with an <c>Unsafe</c> API) enters the scope of its engine itself.</para>
/// </remarks>
public static class EngineScope
{
    private static readonly AsyncLocal<IEngineInstance?> CurrentEngine = new();

    /// <summary>
    /// A scope entered with <see cref="Enter"/>.
    /// </summary>
    public readonly struct Scope
        : IDisposable
    {
        private readonly IEngineInstance? _previous;

        internal Scope(IEngineInstance? previous)
        {
            _previous = previous;
        }

        /// <summary>
        /// Brings back the engine that was current before.
        /// </summary>
        public void Dispose()
        {
            CurrentEngine.Value = _previous;
        }
    }

    /// <summary>
    /// Gets the engine of the running code, or <c>null</c> outside any engine.
    /// </summary>
    public static IEngineInstance? Current => CurrentEngine.Value;

    /// <summary>
    /// Gets or sets what code outside any engine gets: the default instance of the process, or an error.
    /// </summary>
    /// <remarks>
    /// <see cref="DetachedPolicy.Default"/> unless set. The runtime sets <see cref="DetachedPolicy.Throw"/> when it runs
    /// an application (<c>StellarRuntime.Run</c>), for the whole process.
    /// </remarks>
    public static DetachedPolicy Detached { get; set; } = DetachedPolicy.Default;

    /// <summary>
    /// Gets the engine of the running code for the parts of the engine reached without a context.
    /// </summary>
    /// <returns>The current engine, or <c>null</c> for the default instance of the process.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown outside any engine when <see cref="Detached"/> is <see cref="DetachedPolicy.Throw"/>.
    /// </exception>
    public static IEngineInstance? Resolve()
    {
        var engine = CurrentEngine.Value;
        if (engine is null && Detached == DetachedPolicy.Throw)
            throw new InvalidOperationException(
                "This code runs outside any engine: enter the scope of its engine (EngineScope.Enter) where it starts");
        return engine;
    }

    /// <summary>
    /// Makes an engine the current one, until the returned scope is disposed.
    /// </summary>
    /// <param name="engine">The engine; <c>null</c> to leave every engine.</param>
    /// <returns>The scope: disposing it brings back the engine that was current before.</returns>
    public static Scope Enter(IEngineInstance? engine)
    {
        var previous = CurrentEngine.Value;
        CurrentEngine.Value = engine;
        return new Scope(previous);
    }
}

/// <summary>
/// What code outside any engine gets from <see cref="EngineScope.Resolve"/>.
/// </summary>
public enum DetachedPolicy
{
    /// <summary>The default instance of the process: for tools and tests.</summary>
    Default,

    /// <summary>An error: in a running application, all code belongs to an engine.</summary>
    Throw
}
