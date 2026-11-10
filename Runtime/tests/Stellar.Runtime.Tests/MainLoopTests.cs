using Stellar.Kernel;
using Stellar.Kernel.EntryPoint;
using Stellar.Kernel.Lifecycle;
using Stellar.Kernel.Quantization;
using Stellar.Kernel.Timing;
using Stellar.Runtime.Diagnostics;
using Stellar.Runtime.Failures;
using Stellar.Runtime.Lifecycle;
using Stellar.Runtime.Loop;
using Stellar.Runtime.Threading;
using Stellar.Runtime.Timing;

namespace Stellar.Runtime.Tests;

public sealed class MainLoopTests : IDisposable
{
    private readonly List<string> _calls = [];
    private readonly StringWriter _output = new();
    private readonly MainThreadDispatcher _dispatcher = new();
    private readonly EngineControl _control = new();
    private readonly ManualTimeSource _source = new();
    private readonly RuntimeCatcher _catcher;
    private readonly TimeSystem _time;
    private readonly MainLoops _loops;

    public MainLoopTests()
    {
        _catcher = new RuntimeCatcher(CatchLevel.MainLoop, new RuntimeLog(fallback: _output));
        _time = new TimeSystem(_source, 0);
        _loops = new MainLoops(new MainLoopOptions { Name = "Main", MaxDeltaTime = 0 }, _time, _dispatcher, _catcher,
            _control);
    }

    private MainLoop Loop => _loops.PrimaryLoop;

    public void Dispose()
    {
        _loops.Stop();
        _dispatcher.Dispose();
        _control.Dispose();
    }

    private void Frames(params double[] times)
    {
        foreach (var time in times)
        {
            _source.Now = time;
            Loop.RunFrame(time);
        }
    }

    [Fact]
    public void AFrame_RunsEveryPhase_InOrder()
    {
        Loop.Add(new Participant("a", _calls));

        Frames(0, 0.5);

        _calls.Should().Equal(
            "a PreUpdate 0 0", "a FixedUpdate 0 0", "a Update 0 0", "a PostUpdate 0 0", "a PreRender 0 0",
            "a Render 0 0", "a PostRender 0 0",
            "a PreUpdate 1 0.5", "a FixedUpdate 1 0.5", "a Update 1 0.5", "a PostUpdate 1 0.5", "a PreRender 1 0.5",
            "a Render 1 0.5", "a PostRender 1 0.5");
        Loop.FrameIndex.Should().Be(2);
    }

    [Fact]
    public void Order_LowerFirst_ThenArrival()
    {
        Loop.Add(new Participant("game", _calls, Phases.Update));
        Loop.Add(new Participant("engine", _calls, Phases.Update), order: -10);
        Loop.Add(new Participant("late", _calls, Phases.Update));

        Frames(0);

        _calls.Should().Equal("engine Update 0 0", "game Update 0 0", "late Update 0 0");
    }

    [Fact]
    public void AParticipantOfNoPhase_IsRejected()
    {
        var action = () => Loop.Add(new NotAParticipant());

        action.Should().Throw<ArgumentException>().WithMessage("*no phase*");
    }

    [Fact]
    public void Changes_TakeEffectNextFrame()
    {
        var second = new Participant("b", _calls, Phases.Update);
        Participant? first = null;
        first = new Participant("a", _calls, Phases.Update)
        {
            OnUpdate = frame =>
            {
                if (frame != 0) return;
                Loop.Remove(first!).Should().BeTrue();
                Loop.Add(second);
            }
        };
        Loop.Add(first);

        Frames(0, 1);

        _calls.Should().Equal("a Update 0 0", "b Update 1 1");
    }

    [Fact]
    public void AFailingParticipant_DoesNotStopTheOthers_NorTheLoop()
    {
        Loop.Add(new Participant("bad", _calls, Phases.Update)
            { OnUpdate = _ => throw new InvalidOperationException("boom") });
        Loop.Add(new Participant("good", _calls, Phases.Update));

        Frames(0, 1);

        _calls.Should().Contain(["good Update 0 0", "good Update 1 1"]);
        _output.ToString().Should().Contain("boom").And.Contain("Update of");
        _control.IsStopRequested.Should().BeFalse();
    }

    [Fact]
    public void APausedLoop_SkipsThePhases_ButRunsPostedWork()
    {
        var ran = 0;
        Loop.Add(new Participant("a", _calls));
        Loop.Pause();
        _dispatcher.Post(() => ran++);

        Frames(0, 1, 2);

        _calls.Should().BeEmpty();
        ran.Should().Be(1);
        Loop.State.Should().Be(EngineState.Paused);
    }

    [Fact]
    public void APausedClock_StopsItsParticipants_AndNobodyElse()
    {
        var simulation = _time.CreateClock("Simulation", scale: 0.5);
        Loop.Add(new Participant("world", _calls, Phases.Update), simulation);
        Loop.Add(new Participant("hud", _calls, Phases.Update));

        Frames(0, 1);
        simulation.Pause();
        Frames(2);
        simulation.Resume();
        Frames(3);

        _calls.Should().Equal(
            "world Update 0 0", "hud Update 0 0",
            "world Update 1 0.5", "hud Update 1 1",
            "hud Update 2 1",
            "world Update 3 0.5", "hud Update 3 1");
        simulation.Time.Should().Be(1);
    }

    [Fact]
    public void FixedSteps_FollowTheClock_AndAreCapped()
    {
        _time.EngineClock.FixedStep = 0.125;
        var steps = new List<double>();
        Loop.Add(new Participant("physics", _calls, Phases.Fixed) { OnFixed = delta => steps.Add(delta) });

        Frames(0);
        steps.Should().BeEmpty();

        Frames(0.25);
        steps.Should().Equal(0.125, 0.125);

        steps.Clear();
        Frames(10.25);
        steps.Should().HaveCount(8, "a frame runs no more than MaxFixedSteps");
    }

    [Fact]
    public void Timers_RunWhenTheirClockIsDue_AndAPausedClockHoldsThem()
    {
        var simulation = _time.CreateClock("Simulation");
        var once = 0;
        var repeated = 0;
        Loop.Schedule(1, () => once++);
        var timer = Loop.Schedule(1, () => repeated++, simulation, 1);

        Frames(0, 0.5);
        once.Should().Be(0);

        Frames(1);
        once.Should().Be(1);
        repeated.Should().Be(1);

        simulation.Pause();
        Frames(5);
        repeated.Should().Be(1);

        simulation.Resume();
        Frames(6);
        repeated.Should().Be(2);
        timer.Runs.Should().Be(2);

        timer.Cancel();
        Frames(10);
        repeated.Should().Be(2);
        once.Should().Be(1);
    }

    [Fact]
    public void AnAttachedLoop_RunsAFrameAtTheEndOfEachFrameOfItsHost()
    {
        var tools = _loops.Create(new MainLoopOptions { Name = "Tools", MaxDeltaTime = 0 });
        tools.Add(new Participant("tool", _calls, Phases.Update));
        Loop.Add(new Participant("main", _calls, Phases.Update));

        Frames(0, 1);

        _calls.Should().Equal("main Update 0 0", "tool Update 0 0", "main Update 1 1", "tool Update 1 1");
        tools.Clock.Parent.Should().BeSameAs(_time.Engine);

        _loops.Remove(tools).Should().BeTrue();
        _time.Find("Tools").Should().BeNull();
        Frames(2);
        _calls.Should().EndWith("main Update 2 1");
    }

    [Fact]
    public void ADedicatedLoop_RunsOnItsOwnThread_UntilTheLoopsStop()
    {
        var threads = new HashSet<int>();
        var loops = new MainLoops(new MainLoopOptions { Name = "Main" }, new TimeSystem(), _dispatcher, _catcher,
            _control);
        var worker = loops.Create(new MainLoopOptions
            { Name = "Streaming", Threading = MainLoopThreading.Dedicated, TargetFrameRate = 200 });
        worker.Add(new Participant("stream", [], Phases.Update)
        {
            OnUpdate = _ =>
            {
                lock (threads)
                {
                    threads.Add(Environment.CurrentManagedThreadId);
                }
            }
        });

        loops.Start();
        SpinWait.SpinUntil(() => worker.FrameIndex > 3, TimeSpan.FromSeconds(5)).Should().BeTrue();
        loops.Stop();

        threads.Should().ContainSingle().Which.Should().NotBe(Environment.CurrentManagedThreadId);
        worker.State.Should().Be(EngineState.Stopped);
    }

    [Fact]
    public void Run_StopsWhenAnotherThreadStopsTheEngine()
    {
        var loops = new MainLoops(new MainLoopOptions { Name = "Main" }, new TimeSystem(), _dispatcher, _catcher,
            _control);
        loops.PrimaryLoop.Add(new Participant("a", _calls, Phases.Update)
        {
            OnUpdate = frame =>
            {
                if (frame == 5) Task.Run(() => _control.RequestStop(StopReason.Regular)).Wait();
            }
        });

        loops.PrimaryLoop.Run();

        loops.PrimaryLoop.FrameIndex.Should().BeInRange(6, 7);
        loops.PrimaryLoop.State.Should().Be(EngineState.Stopped);
    }

    [Fact]
    public void TargetFrameRate_WaitsBetweenFrames_AndRunsPostedWorkMeanwhile()
    {
        var loops = new MainLoops(new MainLoopOptions { Name = "Main", TargetFrameRate = 50 }, new TimeSystem(),
            _dispatcher, _catcher, _control);
        loops.PrimaryLoop.MaxFrames = 5;
        var posted = 0;
        loops.PrimaryLoop.Add(new Participant("a", _calls, Phases.Update)
        {
            OnUpdate = frame =>
            {
                if (frame == 1) Task.Run(() => _dispatcher.Post(() => posted++));
            }
        });

        var watch = System.Diagnostics.Stopwatch.StartNew();
        loops.PrimaryLoop.Run();

        // Four waits of 20 ms between five frames
        watch.Elapsed.TotalMilliseconds.Should().BeGreaterThan(70);
        posted.Should().Be(1);
        _control.StopReason.Should().Be(StopReason.Regular);
    }

    [Fact]
    public async Task Dispatcher_InvokeAsync_FromAnotherThread_RunsOnTheMainThread()
    {
        var mainThread = Environment.CurrentManagedThreadId;
        var ranOn = 0;

        var invoked = Task.Run(() => _dispatcher.InvokeAsync(() => ranOn = Environment.CurrentManagedThreadId));

        while (!invoked.IsCompleted)
        {
            _dispatcher.Drain(_catcher);
            Thread.Sleep(1);
        }

        await invoked;
        ranOn.Should().Be(mainThread);
    }

    [Fact]
    public void Pump_BringsAwaitBackToTheMainThread()
    {
        var mainThread = Environment.CurrentManagedThreadId;

        var after = MainThreadPump.Run(_dispatcher, _catcher, async () =>
        {
            await Task.Delay(10);
            await Task.Run(() => { });
            return Environment.CurrentManagedThreadId;
        });

        after.Should().Be(mainThread);
    }

    [Fact]
    public void Control_FirstStopWins()
    {
        _control.RequestStop(StopReason.ModuleUnloading);
        _control.RequestStop(StopReason.CriticalError);

        _control.StopReason.Should().Be(StopReason.ModuleUnloading);
        _control.StopToken.IsCancellationRequested.Should().BeTrue();
    }

    [Flags]
    private enum Phases
    {
        Update = 1,
        Fixed = 2,
        Render = 4,
        All = Update | Fixed | Render
    }

    private sealed class ManualTimeSource : ITimeSource
    {
        public double Now { get; set; }
    }

    private sealed class NotAParticipant : IQuant
    {
        public IIdentifier UID => throw new NotSupportedException();

        public IMetaQuant Meta => throw new NotSupportedException();

        public IReadOnlyList<IMetaQuant> Metas => [];

        public TMeta? FindMeta<TMeta>() where TMeta : class, IMetaQuant
        {
            return null;
        }
    }

    private sealed class Participant(string name, List<string> calls, Phases phases = Phases.All)
        : IUpdatable, IFixedUpdatable, IRenderable
    {
        public Action<ulong>? OnUpdate { get; set; }

        public Action<double>? OnFixed { get; set; }

        public IIdentifier UID => throw new NotSupportedException();

        public IMetaQuant Meta => throw new NotSupportedException();

        public IReadOnlyList<IMetaQuant> Metas => [];

        public TMeta? FindMeta<TMeta>() where TMeta : class, IMetaQuant
        {
            return null;
        }

        public void PreUpdate<TContext>(ref readonly TContext context)
            where TContext : IFrameContext, allows ref struct
        {
            Record(Phases.Update, in context);
        }

        public void FixedUpdate<TContext>(ref readonly TContext context)
            where TContext : IFrameContext, allows ref struct
        {
            Record(Phases.Fixed, in context);
            OnFixed?.Invoke(context.DeltaTime);
        }

        public void Update<TContext>(ref readonly TContext context)
            where TContext : IFrameContext, allows ref struct
        {
            Record(Phases.Update, in context);
            OnUpdate?.Invoke(context.FrameIndex);
        }

        public void PostUpdate<TContext>(ref readonly TContext context)
            where TContext : IFrameContext, allows ref struct
        {
            Record(Phases.Update, in context);
        }

        public void PreRender<TContext>(ref readonly TContext context)
            where TContext : IFrameContext, allows ref struct
        {
            Record(Phases.Render, in context);
        }

        public void Render<TContext>(ref readonly TContext context)
            where TContext : IFrameContext, allows ref struct
        {
            Record(Phases.Render, in context);
        }

        public void PostRender<TContext>(ref readonly TContext context)
            where TContext : IFrameContext, allows ref struct
        {
            Record(Phases.Render, in context);
        }

        // Participants of the update group only record Update itself, to keep the expectations short
        private void Record<TContext>(Phases group, ref readonly TContext context)
            where TContext : IFrameContext, allows ref struct
        {
            if ((phases & group) == 0) return;
            if (group == Phases.Update && context.Phase != FramePhase.Update && phases != Phases.All) return;

            calls.Add($"{name} {context.Phase} {context.FrameIndex} {context.DeltaTime:0.###}");
        }
    }
}
