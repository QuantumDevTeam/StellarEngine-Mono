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
/// (tools, tests) <see cref="Current"/> is <c>null</c>, and those parts fall back to the default instance of the
/// process.</para>
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
