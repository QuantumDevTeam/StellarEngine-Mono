namespace Stellar.Kernel.Profiling;

/// <summary>
/// How much the engine marks for a session: each level adds to the one before.
/// </summary>
public enum ProfilerDetail
{
    /// <summary>The frames of the loops, their phases, timers and posted work, and the counters of a frame.</summary>
    Frames,

    /// <summary>Also every call of every participant of the loops: costly with many participants.</summary>
    Participants
}

/// <summary>
/// A recording of one engine: what a profiler module gives the engine, which writes the events into it.
/// </summary>
/// <remarks>
/// <para>The engine writes on the thread where the work happens, from any thread at once, and on the hot paths of a
/// frame: <see cref="Write"/> must be cheap and safe from any thread, which a buffer per thread (a native list) gives.
/// It must not throw.</para>
/// <para>A session is attached to its engine with <see cref="Profiler.Attach"/>.</para>
/// </remarks>
public interface IProfilerSession
{
    /// <summary>
    /// Gets how much the engine marks.
    /// </summary>
    ProfilerDetail Detail { get; }

    /// <summary>
    /// Records an event of the calling thread.
    /// </summary>
    /// <param name="profilerEvent">The event.</param>
    void Write(in ProfilerEvent profilerEvent);
}
