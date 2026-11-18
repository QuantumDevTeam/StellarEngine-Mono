using System;

namespace Stellar.Kernel.Profiling;

/// <summary>
/// Marks a span of work for a profiler: made once, kept in a static field, used where the work is.
/// </summary>
/// <remarks>
/// <code>
/// private static readonly ProfilerMarker Step = new("Physics.Step", ProfilerCategory.Physics);
///
/// using (Step.Auto())
///     Simulate(delta);
/// </code>
/// <para>While no profiler records, a span costs one check and allocates nothing. When one does, the span goes to the
/// session of the engine of the running code (<see cref="Profiler.Current"/>). Code that marks many spans in a row
/// reads the session once and passes it: <see cref="Auto(IProfilerSession)"/>.</para>
/// </remarks>
public readonly struct ProfilerMarker
{
    /// <summary>
    /// Makes a marker, registering its name.
    /// </summary>
    /// <param name="name">The name; by convention dotted, from the general to the precise: <c>Physics.Step</c>.</param>
    /// <param name="category">Where it belongs.</param>
    public ProfilerMarker(string name, ProfilerCategory category = default)
    {
        Id = ProfilerMarkers.Register(name, category);
    }

    /// <summary>
    /// Gets the number of the marker; 0 for the default marker, which marks nothing.
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// Gets the name.
    /// </summary>
    public string Name => ProfilerMarkers.NameOf(Id);

    /// <summary>
    /// Gets the category.
    /// </summary>
    public ProfilerCategory Category => ProfilerMarkers.CategoryOf(Id);

    /// <summary>
    /// Marks the span of a <c>using</c> block, for the session of the running code.
    /// </summary>
    /// <returns>The scope; disposing it ends the span.</returns>
    public ProfilerScope Auto()
    {
        return Auto(Profiler.Current);
    }

    /// <summary>
    /// Marks the span of a <c>using</c> block, for a session read before.
    /// </summary>
    /// <param name="session">The session; <c>null</c> marks nothing.</param>
    /// <returns>The scope; disposing it ends the span.</returns>
    public ProfilerScope Auto(IProfilerSession? session)
    {
        if (session is null || Id == 0) return default;

        session.Write(ProfilerEvent.Now(Id, ProfilerEventKind.Begin));
        return new ProfilerScope(session, Id);
    }

    /// <summary>
    /// Marks a moment, for the session of the running code.
    /// </summary>
    /// <param name="value">What the moment carries.</param>
    public void Instant(long value = 0)
    {
        if (Id != 0) Profiler.Current?.Write(ProfilerEvent.Now(Id, ProfilerEventKind.Instant, value));
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return Id == 0 ? "(no marker)" : Name;
    }
}

/// <summary>
/// A marked span: ends when disposed. The default scope marks nothing.
/// </summary>
public readonly struct ProfilerScope
    : IDisposable
{
    private readonly IProfilerSession? _session;
    private readonly int _marker;

    internal ProfilerScope(IProfilerSession session, int marker)
    {
        _session = session;
        _marker = marker;
    }

    /// <summary>
    /// Ends the span.
    /// </summary>
    public void Dispose()
    {
        _session?.Write(ProfilerEvent.Now(_marker, ProfilerEventKind.End));
    }
}

/// <summary>
/// A value a profiler follows over time: bytes, objects, items in a queue. Made once, kept in a static field.
/// </summary>
/// <param name="name">The name; by convention dotted: <c>Memory.Native</c>.</param>
/// <param name="category">Where it belongs.</param>
public readonly struct ProfilerCounter(string name, ProfilerCategory category = default)
{
    /// <summary>
    /// Gets the number of the counter.
    /// </summary>
    public int Id { get; } = ProfilerMarkers.Register(name, category);

    /// <summary>
    /// Gives the counter a value, for the session of the running code.
    /// </summary>
    public void Set(long value)
    {
        Set(Profiler.Current, value);
    }

    /// <summary>
    /// Gives the counter a value, for a session read before.
    /// </summary>
    public void Set(IProfilerSession? session, long value)
    {
        if (Id != 0) session?.Write(ProfilerEvent.Now(Id, ProfilerEventKind.Counter, value));
    }
}
