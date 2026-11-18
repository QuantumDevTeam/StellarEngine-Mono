using Stellar.Kernel.EntryPoint;
using Stellar.Kernel.Failures;
using Stellar.Kernel.Lifecycle;
using Stellar.Kernel.Profiling;
using Stellar.Kernel.Quantization;
using Stellar.Kernel.Threading;
using Stellar.Kernel.Timing;
using ITimer = Stellar.Kernel.Timing.ITimer;
using Stellar.Runtime.Failures;
using Stellar.Runtime.Lifecycle;
using Stellar.Runtime.Threading;
using Stellar.Runtime.Timing;
using Timer = Stellar.Runtime.Timing.Timer;

namespace Stellar.Runtime.Loop;

/// <summary>
/// A main loop: runs frames on one thread until it, or its engine, is asked to stop.
/// </summary>
/// <remarks>
/// <para>A frame: the work posted to the thread (when the loop owns it), the participants added or removed since the
/// previous frame, the clocks read once, the timers that are due, the phases of <see cref="FramePhase"/>, then the
/// loops attached to this one. Then the wait for the next frame, if the frame rate is limited.</para>
/// <para>Each call of each participant is caught on its own (<see cref="CatchLevel.MainLoop"/>): a failing
/// participant does not take down the frame, the failure system decides whether the engine stops.</para>
/// </remarks>
public sealed class MainLoop
    : IMainLoop
{
    private static readonly FramePhase[] Phases = Enum.GetValues<FramePhase>();

    // What a profiler sees of a loop: its frames, their phases, timers and posted work, and two counters a frame
    private static readonly ProfilerMarker[] PhaseMarkers = Phases
        .Select(phase => new ProfilerMarker($"Loop.{phase}",
            phase <= FramePhase.PostUpdate ? ProfilerCategory.Update : ProfilerCategory.Render))
        .ToArray();

    private static readonly ProfilerMarker TimersMarker = new("Loop.Timers", ProfilerCategory.Loop);
    private static readonly ProfilerMarker PostedMarker = new("Loop.Posted", ProfilerCategory.Jobs);
    private static readonly ProfilerCounter AllocatedCounter = new("Memory.AllocatedInFrame", ProfilerCategory.Memory);
    private static readonly ProfilerCounter CollectionsCounter = new("Memory.Gen0Collections", ProfilerCategory.Memory);

    private readonly ProfilerMarker _frameMarker;

    private readonly MainThreadDispatcher _dispatcher;
    private readonly bool _ownsDispatcher;
    private readonly Catcher _catcher;
    private readonly EngineControl _control;
    private readonly ITimeSource _source;
    private readonly Participants _participants = new();
    private readonly Dictionary<IClock, ClockFrame> _clocks = new(ReferenceEqualityComparer.Instance);
    private readonly List<Timer> _timers = [];
    private readonly List<Timer> _due = [];
    private readonly List<IClock> _unused = [];
    private readonly object _gate = new();
    private MainLoop[] _attached = [];
    private int _state = (int)EngineState.Created;
    private volatile bool _paused;
    private volatile bool _stopRequested;
    private ulong _frameIndex;
    private double _previousNow = double.NaN;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainLoop"/> class.
    /// </summary>
    /// <param name="options">How the loop runs.</param>
    /// <param name="clock">The clock of the loop.</param>
    /// <param name="source">Where the time comes from: the source of the time system of the clocks.</param>
    /// <param name="dispatcher">The dispatcher of the thread of the loop.</param>
    /// <param name="ownsDispatcher">Whether the loop runs the posted work (it does unless it is attached).</param>
    /// <param name="catcher">Catches what the participants throw.</param>
    /// <param name="control">The control of the engine: every loop stops with it.</param>
    /// <param name="isPrimary">Whether stopping this loop stops the engine.</param>
    public MainLoop(MainLoopOptions options, IClock clock, ITimeSource source, MainThreadDispatcher dispatcher,
        bool ownsDispatcher, Catcher catcher, EngineControl control, bool isPrimary = false)
    {
        Options = options;
        Clock = clock;
        _source = source;
        _dispatcher = dispatcher;
        _ownsDispatcher = ownsDispatcher;
        _catcher = catcher;
        _control = control;
        IsPrimary = isPrimary;
        _frameMarker = new ProfilerMarker($"Loop.{options.Name}", ProfilerCategory.Loop);
    }

    /// <summary>
    /// Gets how the loop runs.
    /// </summary>
    public MainLoopOptions Options { get; private set; }

    /// <summary>
    /// Gets whether stopping this loop stops the engine.
    /// </summary>
    public bool IsPrimary { get; }

    /// <summary>
    /// Gets or sets the number of frames after which the loop stops; 0 for no limit.
    /// </summary>
    public ulong MaxFrames { get; set; }

    /// <inheritdoc/>
    public string Name => Options.Name;

    /// <inheritdoc/>
    public IClock Clock { get; }

    /// <inheritdoc/>
    public IMainThreadDispatcher Dispatcher => _dispatcher;

    /// <inheritdoc/>
    public EngineState State => (EngineState)Volatile.Read(ref _state);

    /// <inheritdoc/>
    public ulong FrameIndex => Interlocked.Read(ref _frameIndex);

    /// <summary>
    /// Gets whether the loop, or its engine, was asked to stop.
    /// </summary>
    public bool IsStopRequested => _stopRequested || _control.IsStopRequested;

    /// <inheritdoc/>
    public void Add(IQuant participant, IClock? clock = null, int order = 0)
    {
        ArgumentNullException.ThrowIfNull(participant);

        if (!Participant.TakesPart(participant))
            throw new ArgumentException(
                $"{participant.GetType().FullName} takes part in no phase: it is not updatable, fixed updatable or renderable",
                nameof(participant));

        _participants.Add(participant, clock ?? Clock, order);
    }

    /// <inheritdoc/>
    public bool Remove(IQuant participant)
    {
        return _participants.Remove(participant);
    }

    /// <inheritdoc/>
    public ITimer Schedule(double delay, Action callback, IClock? clock = null, double period = 0)
    {
        ArgumentNullException.ThrowIfNull(callback);
        if (delay < 0 || double.IsNaN(delay)) throw new ArgumentOutOfRangeException(nameof(delay));

        var timerClock = clock ?? Clock;
        var timer = new Timer(timerClock, Sample(timerClock, _source.Now) + delay, period, callback);

        lock (_gate)
        {
            _timers.Add(timer);
        }

        return timer;
    }

    /// <inheritdoc/>
    public void Pause()
    {
        _paused = true;
    }

    /// <inheritdoc/>
    public void Resume()
    {
        _paused = false;
    }

    /// <inheritdoc/>
    public void RequestStop()
    {
        if (IsPrimary)
        {
            _control.RequestStop(StopReason.Regular);
            return;
        }

        _stopRequested = true;
        _dispatcher.Post(static () => { });
    }

    /// <summary>
    /// Runs frames until the loop or the engine is asked to stop.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if called from another thread than the one of the loop.</exception>
    public void Run()
    {
        if (!_dispatcher.IsMainThread)
            throw new InvalidOperationException($"The loop {Name} runs on the thread of its dispatcher");

        var frameDuration = Options.TargetFrameRate > 0 ? 1 / Options.TargetFrameRate : 0;
        var nextFrame = _source.Now;

        while (!IsStopRequested)
        {
            RunFrame(_source.Now);

            if (MaxFrames > 0 && FrameIndex >= MaxFrames)
            {
                RequestStop();
                break;
            }

            if (frameDuration > 0)
                nextFrame = WaitForNextFrame(nextFrame + frameDuration, frameDuration);
        }

        SetState(EngineState.Stopped);
    }

    /// <summary>
    /// Runs one frame.
    /// </summary>
    /// <param name="now">The moment of the frame, as the time source says it.</param>
    public void RunFrame(double now)
    {
        var realDelta = double.IsNaN(_previousNow) ? 0 : Math.Max(0, now - _previousNow);
        _previousNow = now;

        var reportedDelta = Options.MaxDeltaTime > 0 ? Math.Min(realDelta, Options.MaxDeltaTime) : realDelta;
        var shrink = realDelta > 0 ? reportedDelta / realDelta : 1;

        // Read once a frame: null while nobody records, and then nothing below costs more than a check
        var profiler = Profiler.Current;
        var allocated = 0L;
        if (profiler is not null)
        {
            profiler.Write(ProfilerEvent.Now(_frameMarker.Id, ProfilerEventKind.Frame, (long)_frameIndex));
            allocated = GC.GetAllocatedBytesForCurrentThread();
        }

        using var frameScope = _frameMarker.Auto(profiler);

        if (_ownsDispatcher)
            using (PostedMarker.Auto(profiler))
                if (_dispatcher.Drain(_catcher, CatchLevel.MainLoop))
                    _control.RequestStop(StopReason.CriticalError);

        _participants.Apply();
        SampleClocks(now, shrink);

        using (TimersMarker.Auto(profiler))
            RunTimers();

        SetState(_paused ? EngineState.Paused : EngineState.Running);

        if (!_paused)
        {
            // Read once: only this thread changes it, and Interlocked.Read is not free
            var frameIndex = _frameIndex;
            foreach (var phase in Phases)
            {
                if (IsStopRequested) break;
                RunPhase(phase, reportedDelta, frameIndex, profiler);
            }
        }

        if (profiler is not null)
        {
            AllocatedCounter.Set(profiler, GC.GetAllocatedBytesForCurrentThread() - allocated);
            CollectionsCounter.Set(profiler, GC.CollectionCount(0));
        }

        foreach (var attached in Volatile.Read(ref _attached))
            if (!attached.IsStopRequested)
                attached.RunFrame(now);

        Interlocked.Increment(ref _frameIndex);
    }

    /// <summary>
    /// Attaches a loop: it runs a frame at the end of every frame of this one.
    /// </summary>
    internal void Attach(MainLoop loop)
    {
        lock (_gate)
        {
            _attached = [.. _attached, loop];
        }
    }

    /// <summary>
    /// Detaches a loop.
    /// </summary>
    internal void Detach(MainLoop loop)
    {
        lock (_gate)
        {
            _attached = _attached.Where(attached => !ReferenceEquals(attached, loop)).ToArray();
        }
    }

    internal void MarkStopped()
    {
        SetState(EngineState.Stopped);
    }

    /// <summary>
    /// Changes how the loop runs, before it runs: the name stays.
    /// </summary>
    internal void Configure(MainLoopOptions options)
    {
        Options = options with { Name = Options.Name };
    }

    private static double Sample(IClock clock, double now)
    {
        return clock is Clock own ? own.TimeAt(now) : clock.Time;
    }

    private void SampleClocks(double now, double shrink)
    {
        foreach (var frame in _clocks.Values) frame.Used = false;

        // A participant keeps the frame of its clock: no lookup while the loop follows the clock
        foreach (var participant in _participants.Items)
        {
            var frame = participant.Frame;
            if (frame is null || frame.Forgotten) participant.Frame = frame = FrameOf(participant.Clock);
            Use(participant.Clock, frame);
        }

        lock (_gate)
        {
            foreach (var timer in _timers)
                Use(timer.Clock, FrameOf(timer.Clock));
        }

        // Clocks nobody follows any more are forgotten: their accumulated steps with them
        foreach (var (clock, frame) in _clocks)
            if (!frame.Used)
                _unused.Add(clock);

        if (_unused.Count > 0)
        {
            foreach (var clock in _unused)
                if (_clocks.Remove(clock, out var frame))
                    frame.Forgotten = true;

            _unused.Clear();
        }

        return;

        ClockFrame FrameOf(IClock clock)
        {
            if (_clocks.TryGetValue(clock, out var frame)) return frame;

            frame = new ClockFrame { Time = Sample(clock, now) };
            _clocks[clock] = frame;
            return frame;
        }

        void Use(IClock clock, ClockFrame frame)
        {
            if (frame.Used) return;
            frame.Used = true;

            var time = Sample(clock, now);
            frame.Delta = Math.Max(0, time - frame.Time) * shrink;
            frame.Time = time;
            frame.Running = clock.IsRunning;

            var step = clock.EffectiveFixedStep;
            frame.FixedStep = step;

            if (step > 0 && frame.Running)
            {
                frame.Accumulator += frame.Delta;
                frame.FixedSteps = (int)Math.Min(Math.Floor(frame.Accumulator / step), Options.MaxFixedSteps);
                frame.Accumulator -= frame.FixedSteps * step;

                // Too far behind: drop what cannot be caught up, instead of slowing down every next frame
                if (frame.FixedSteps == Options.MaxFixedSteps && frame.Accumulator > step)
                    frame.Accumulator = step;
            }
            else
            {
                frame.FixedSteps = frame.Running ? 1 : 0;
            }
        }
    }

    private void RunTimers()
    {
        lock (_gate)
        {
            if (_timers.Count == 0) return;

            _timers.RemoveAll(static timer => !timer.IsActive);
            foreach (var timer in _timers)
                if (_clocks[timer.Clock].Time >= timer.DueTime)
                    _due.Add(timer);
        }

        try
        {
            foreach (var timer in _due)
            {
                if (!timer.IsActive) continue;

                var time = _clocks[timer.Clock].Time;
                var failure = _catcher.Guard(timer.Callback, $"a timer on {timer.Clock.Name}", FailureKind.Engine, this);
                timer.Ran(time);

                if (RuntimeCatchers.MustStop(CatchLevel.MainLoop, failure)) _control.RequestStop(failure!);
            }
        }
        finally
        {
            _due.Clear();
        }
    }

    // Only the participants with code for the phase: their contracts are known to be there
    private void RunPhase(FramePhase phase, double unscaledDelta, ulong frameIndex, IProfilerSession? profiler)
    {
        using var phaseScope = PhaseMarkers[(int)phase].Auto(profiler);
        var detailed = profiler is { Detail: ProfilerDetail.Participants } ? profiler : null;

        foreach (var participant in _participants.In(phase))
        {
            var frame = participant.Frame!;
            if (!frame.Running) continue;

            detailed?.Write(ProfilerEvent.Now(participant.Marker, ProfilerEventKind.Begin));

            try
            {
                switch (phase)
                {
                    case FramePhase.PreUpdate:
                    {
                        var context = Context(phase, frameIndex, participant, frame, frame.Delta, unscaledDelta);
                        participant.Updatable!.PreUpdate(in context);
                        break;
                    }
                    case FramePhase.FixedUpdate:
                    {
                        var step = frame.FixedStep > 0 ? frame.FixedStep : frame.Delta;
                        for (var index = 0; index < frame.FixedSteps; index++)
                        {
                            var context = Context(phase, frameIndex, participant, frame, step, unscaledDelta);
                            participant.FixedUpdatable!.FixedUpdate(in context);
                        }

                        break;
                    }
                    case FramePhase.Update:
                    {
                        var context = Context(phase, frameIndex, participant, frame, frame.Delta, unscaledDelta);
                        participant.Updatable!.Update(in context);
                        break;
                    }
                    case FramePhase.PostUpdate:
                    {
                        var context = Context(phase, frameIndex, participant, frame, frame.Delta, unscaledDelta);
                        participant.Updatable!.PostUpdate(in context);
                        break;
                    }
                    case FramePhase.PreRender:
                    {
                        var context = Context(phase, frameIndex, participant, frame, frame.Delta, unscaledDelta);
                        participant.Renderable!.PreRender(in context);
                        break;
                    }
                    case FramePhase.Render:
                    {
                        var context = Context(phase, frameIndex, participant, frame, frame.Delta, unscaledDelta);
                        participant.Renderable!.Render(in context);
                        break;
                    }
                    case FramePhase.PostRender:
                    {
                        var context = Context(phase, frameIndex, participant, frame, frame.Delta, unscaledDelta);
                        participant.Renderable!.PostRender(in context);
                        break;
                    }
                }
            }
            catch (Exception exception)
            {
                var failure = _catcher.Report(exception, $"{phase} of {participant.Quant.GetType().FullName} in {Name}",
                    sender: this, phase: phase);

                if (RuntimeCatchers.MustStop(CatchLevel.MainLoop, failure)) _control.RequestStop(failure!);
            }

            detailed?.Write(ProfilerEvent.Now(participant.Marker, ProfilerEventKind.End));
        }
    }

    private FrameContext Context(FramePhase phase, ulong frameIndex, Participant participant, ClockFrame frame,
        double delta, double unscaledDelta)
    {
        return new FrameContext(this, phase, frameIndex, participant.Clock, frame.Time, delta, unscaledDelta);
    }

    private double WaitForNextFrame(double nextFrame, double frameDuration)
    {
        while (!IsStopRequested)
        {
            var remaining = nextFrame - _source.Now;
            if (remaining <= 0) break;

            if (_dispatcher.WaitForWork(TimeSpan.FromSeconds(remaining)) && _ownsDispatcher &&
                _dispatcher.Drain(_catcher, CatchLevel.MainLoop))
                _control.RequestStop(StopReason.CriticalError);
        }

        // Behind by more than a frame: do not run the missed frames back to back
        var now = _source.Now;
        return nextFrame < now - frameDuration ? now : nextFrame;
    }

    private void SetState(EngineState state)
    {
        Volatile.Write(ref _state, (int)state);
    }
}
