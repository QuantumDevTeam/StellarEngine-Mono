using Stellar.Kernel.EntryPoint;
using Stellar.Kernel.Failures;
using Stellar.Kernel.Lifecycle;
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
    private readonly MainThreadDispatcher _dispatcher;
    private readonly bool _ownsDispatcher;
    private readonly RuntimeCatcher _catcher;
    private readonly EngineControl _control;
    private readonly ITimeSource _source;
    private readonly Participants _participants = new();
    private readonly Dictionary<IClock, ClockFrame> _clocks = new(ReferenceEqualityComparer.Instance);
    private readonly List<Timer> _timers = [];
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
        bool ownsDispatcher, RuntimeCatcher catcher, EngineControl control, bool isPrimary = false)
    {
        Options = options;
        Clock = clock;
        _source = source;
        _dispatcher = dispatcher;
        _ownsDispatcher = ownsDispatcher;
        _catcher = catcher;
        _control = control;
        IsPrimary = isPrimary;
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

        if (_ownsDispatcher && _dispatcher.Drain(_catcher))
            _control.RequestStop(StopReason.CriticalError);

        _participants.Apply();
        SampleClocks(now, shrink);
        RunTimers();

        SetState(_paused ? EngineState.Paused : EngineState.Running);

        if (!_paused)
            foreach (var phase in Enum.GetValues<FramePhase>())
            {
                if (IsStopRequested) break;
                RunPhase(phase, reportedDelta);
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

        foreach (var participant in _participants.Items) Use(participant.Clock);

        lock (_gate)
        {
            foreach (var timer in _timers)
                Use(timer.Clock);
        }

        // Clocks nobody follows any more are forgotten: their accumulated steps with them
        foreach (var unused in _clocks.Where(pair => !pair.Value.Used).Select(pair => pair.Key).ToList())
            _clocks.Remove(unused);

        return;

        void Use(IClock clock)
        {
            if (!_clocks.TryGetValue(clock, out var frame))
            {
                frame = new ClockFrame { Time = Sample(clock, now) };
                _clocks[clock] = frame;
            }

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
        List<Timer> due;

        lock (_gate)
        {
            _timers.RemoveAll(timer => !timer.IsActive);
            due = _timers.Where(timer => _clocks[timer.Clock].Time >= timer.DueTime).ToList();
        }

        foreach (var timer in due)
        {
            if (!timer.IsActive) continue;

            var time = _clocks[timer.Clock].Time;
            var result = _catcher.Run(timer.Callback, $"a timer on {timer.Clock.Name}", FailureType.Engine, this);
            timer.Ran(time);

            if (result.MustStop) _control.RequestStop(result.Exception!);
        }
    }

    private void RunPhase(FramePhase phase, double unscaledDelta)
    {
        foreach (var participant in _participants.Items)
        {
            var frame = _clocks[participant.Clock];
            if (!frame.Running) continue;

            try
            {
                switch (phase)
                {
                    case FramePhase.PreUpdate when participant.Updatable is { } updatable:
                    {
                        var context = Context(phase, participant, frame, frame.Delta, unscaledDelta);
                        updatable.PreUpdate(in context);
                        break;
                    }
                    case FramePhase.FixedUpdate when participant.FixedUpdatable is { } fixedUpdatable:
                    {
                        var step = frame.FixedStep > 0 ? frame.FixedStep : frame.Delta;
                        for (var index = 0; index < frame.FixedSteps; index++)
                        {
                            var context = Context(phase, participant, frame, step, unscaledDelta);
                            fixedUpdatable.FixedUpdate(in context);
                        }

                        break;
                    }
                    case FramePhase.Update when participant.Updatable is { } updatable:
                    {
                        var context = Context(phase, participant, frame, frame.Delta, unscaledDelta);
                        updatable.Update(in context);
                        break;
                    }
                    case FramePhase.PostUpdate when participant.Updatable is { } updatable:
                    {
                        var context = Context(phase, participant, frame, frame.Delta, unscaledDelta);
                        updatable.PostUpdate(in context);
                        break;
                    }
                    case FramePhase.PreRender when participant.Renderable is { } renderable:
                    {
                        var context = Context(phase, participant, frame, frame.Delta, unscaledDelta);
                        renderable.PreRender(in context);
                        break;
                    }
                    case FramePhase.Render when participant.Renderable is { } renderable:
                    {
                        var context = Context(phase, participant, frame, frame.Delta, unscaledDelta);
                        renderable.Render(in context);
                        break;
                    }
                    case FramePhase.PostRender when participant.Renderable is { } renderable:
                    {
                        var context = Context(phase, participant, frame, frame.Delta, unscaledDelta);
                        renderable.PostRender(in context);
                        break;
                    }
                }
            }
            catch (Exception exception)
            {
                var type = phase <= FramePhase.PostUpdate ? FailureType.Update : FailureType.Render;
                var result = _catcher.Handle(exception, $"{phase} of {participant.Quant.GetType().FullName} in {Name}",
                    type, this);

                if (result.MustStop) _control.RequestStop(exception);
            }
        }
    }

    private FrameContext Context(FramePhase phase, Participant participant, ClockFrame frame, double delta,
        double unscaledDelta)
    {
        return new FrameContext(this, phase, FrameIndex, participant.Clock, frame.Time, delta, unscaledDelta);
    }

    private double WaitForNextFrame(double nextFrame, double frameDuration)
    {
        while (!IsStopRequested)
        {
            var remaining = nextFrame - _source.Now;
            if (remaining <= 0) break;

            if (_dispatcher.WaitForWork(TimeSpan.FromSeconds(remaining)) && _ownsDispatcher &&
                _dispatcher.Drain(_catcher))
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

    /// <summary>
    /// What a clock is at the current frame of this loop.
    /// </summary>
    private sealed class ClockFrame
    {
        public double Time { get; set; }

        public double Delta { get; set; }

        public bool Running { get; set; }

        public double FixedStep { get; set; }

        public double Accumulator { get; set; }

        public int FixedSteps { get; set; }

        public bool Used { get; set; }
    }
}
