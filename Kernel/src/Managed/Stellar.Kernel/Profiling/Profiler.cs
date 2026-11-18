using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Stellar.Kernel.Lifecycle;

namespace Stellar.Kernel.Profiling;

/// <summary>
/// Where the engine finds the profiler session of the running code. The profiler itself is a module: it makes a
/// session and attaches it to an engine; the engine and the code marked with <see cref="ProfilerMarker"/> write into it.
/// </summary>
/// <remarks>
/// <para>One session per engine: an editor records the game it runs, not itself, and two games in one process are
/// recorded apart. Code outside any engine (tools, tests) has a session of its own, attached for <c>null</c>.</para>
/// <para>While nothing is attached anywhere, <see cref="Current"/> is one read of a field. While something is, it is the
/// engine of the running code (<see cref="EngineScope"/>) compared with the one of the previous call on the thread.</para>
/// </remarks>
public static class Profiler
{
    private static readonly ConditionalWeakTable<IEngineInstance, IProfilerSession> Sessions = new();
    private static readonly object Gate = new();

    private static int _attached;
    private static int _version = 1;
    private static IProfilerSession? _detached;

    // The last answer on this thread: valid while no session came or went since (the version)
    [ThreadStatic] private static IEngineInstance? _lastEngine;
    [ThreadStatic] private static IProfilerSession? _lastSession;
    [ThreadStatic] private static int _lastVersion;

    /// <summary>
    /// Gets whether a session is attached anywhere in the process.
    /// </summary>
    public static bool IsActive => Volatile.Read(ref _attached) != 0;

    /// <summary>
    /// Gets how many ticks of <see cref="ProfilerEvent.Timestamp"/> make a second.
    /// </summary>
    public static long Frequency => Stopwatch.Frequency;

    /// <summary>
    /// Gets the session of the engine of the running code, or <c>null</c> when it is not recorded.
    /// </summary>
    public static IProfilerSession? Current
    {
        get
        {
            if (Volatile.Read(ref _attached) == 0) return null;

            var engine = EngineScope.Current;
            if (_lastVersion == Volatile.Read(ref _version) && ReferenceEquals(engine, _lastEngine)) return _lastSession;

            return Refresh(engine);
        }
    }

    /// <summary>
    /// Gets the session of an engine.
    /// </summary>
    /// <param name="engine">The engine; <c>null</c> for code outside any engine.</param>
    /// <returns>The session, or <c>null</c>.</returns>
    public static IProfilerSession? SessionOf(IEngineInstance? engine)
    {
        if (engine is null) return Volatile.Read(ref _detached);
        return Sessions.TryGetValue(engine, out var session) ? session : null;
    }

    /// <summary>
    /// Attaches a session to an engine: from now on, the engine records into it.
    /// </summary>
    /// <param name="engine">The engine; <c>null</c> for code outside any engine.</param>
    /// <param name="session">The session.</param>
    /// <returns>The attachment: disposing it detaches the session.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the engine has a session already.</exception>
    public static IDisposable Attach(IEngineInstance? engine, IProfilerSession session)
    {
        if (session is null) throw new ArgumentNullException(nameof(session));

        lock (Gate)
        {
            if (SessionOf(engine) is not null)
                throw new InvalidOperationException($"{engine?.Name ?? "Code outside any engine"} is recorded already");

            if (engine is null) Volatile.Write(ref _detached, session);
            else Sessions.Add(engine, session);

            Interlocked.Increment(ref _attached);
            Interlocked.Increment(ref _version);
        }

        return new Attachment(engine, session);
    }

    private static IProfilerSession? Refresh(IEngineInstance? engine)
    {
        var version = Volatile.Read(ref _version);
        var session = SessionOf(engine);

        _lastEngine = engine;
        _lastSession = session;
        _lastVersion = version;
        return session;
    }

    private static void Detach(IEngineInstance? engine, IProfilerSession session)
    {
        lock (Gate)
        {
            if (!ReferenceEquals(SessionOf(engine), session)) return;

            if (engine is null) Volatile.Write(ref _detached, null);
            else Sessions.Remove(engine);

            Interlocked.Decrement(ref _attached);
            Interlocked.Increment(ref _version);
        }
    }

    private sealed class Attachment(IEngineInstance? engine, IProfilerSession session)
        : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0) Detach(engine, session);
        }
    }
}
