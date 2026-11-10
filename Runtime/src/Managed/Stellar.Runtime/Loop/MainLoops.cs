using Stellar.Kernel.Lifecycle;
using Stellar.Runtime.Failures;
using Stellar.Runtime.Lifecycle;
using Stellar.Runtime.Threading;
using Stellar.Runtime.Timing;

namespace Stellar.Runtime.Loop;

/// <summary>
/// The loops of one engine: the primary one on the main thread, and the ones modules create.
/// </summary>
/// <remarks>
/// An attached loop runs inside the frames of its host. A dedicated loop runs on a thread of its own, started with the
/// engine (or at once if the engine runs already) and joined when the engine stops.
/// </remarks>
public sealed class MainLoops
    : IMainLoops
{
    private readonly object _gate = new();
    private readonly TimeSystem _time;
    private readonly RuntimeCatcher _catcher;
    private readonly EngineControl _control;
    private readonly List<MainLoop> _loops = [];
    private readonly Dictionary<MainLoop, Thread> _threads = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<MainLoop> _ownClocks = new(ReferenceEqualityComparer.Instance);
    private bool _started;
    private bool _stopped;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainLoops"/> class, with the primary loop.
    /// </summary>
    /// <param name="primary">How the primary loop runs. Its clock is the engine clock unless it names another.</param>
    /// <param name="time">The clocks of the engine.</param>
    /// <param name="dispatcher">The dispatcher of the main thread.</param>
    /// <param name="catcher">Catches what the participants of every loop throw.</param>
    /// <param name="control">The control of the engine.</param>
    public MainLoops(MainLoopOptions primary, TimeSystem time, MainThreadDispatcher dispatcher, RuntimeCatcher catcher,
        EngineControl control)
    {
        _time = time;
        _catcher = catcher;
        _control = control;

        PrimaryLoop = new MainLoop(primary with { Threading = MainLoopThreading.Dedicated },
            primary.Clock ?? time.Engine, time.Source, dispatcher, true, catcher, control, true);
        _loops.Add(PrimaryLoop);
    }

    /// <summary>
    /// Gets the primary loop.
    /// </summary>
    public MainLoop PrimaryLoop { get; }

    /// <inheritdoc/>
    public IMainLoop Primary => PrimaryLoop;

    /// <inheritdoc/>
    public IReadOnlyList<IMainLoop> Loops
    {
        get
        {
            lock (_gate)
            {
                return _loops.ToList();
            }
        }
    }

    /// <inheritdoc/>
    public IMainLoop Create(MainLoopOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrEmpty(options.Name, nameof(options));

        lock (_gate)
        {
            if (_stopped) throw new InvalidOperationException("The engine has stopped");
            if (_loops.Exists(loop => loop.Name == options.Name))
                throw new ArgumentException($"There is a loop named {options.Name} already", nameof(options));

            var clock = options.Clock ?? _time.CreateClock(options.Name);
            MainLoop loop;

            if (options.Threading == MainLoopThreading.Attached)
            {
                var host = options.Host as MainLoop ?? PrimaryLoop;
                if (!_loops.Contains(host))
                    throw new ArgumentException("The host loop belongs to another engine", nameof(options));

                loop = new MainLoop(options, clock, _time.Source, (MainThreadDispatcher)host.Dispatcher,
                    false, _catcher, _control);
                host.Attach(loop);
            }
            else
            {
                loop = new MainLoop(options, clock, _time.Source, new MainThreadDispatcher(),
                    true, _catcher, _control);
                if (_started) StartThread(loop);
            }

            if (options.Clock is null) _ownClocks.Add(loop);
            _loops.Add(loop);
            return loop;
        }
    }

    /// <summary>
    /// Changes how the primary loop runs, before the engine runs it.
    /// </summary>
    /// <param name="options">How.</param>
    public void ConfigurePrimary(MainLoopOptions options)
    {
        PrimaryLoop.Configure(options with { Threading = MainLoopThreading.Dedicated });
    }

    /// <inheritdoc/>
    public IMainLoop? Find(string name)
    {
        lock (_gate)
        {
            return _loops.FirstOrDefault(loop => loop.Name == name);
        }
    }

    /// <inheritdoc/>
    public bool Remove(IMainLoop loop)
    {
        Thread? thread;

        lock (_gate)
        {
            if (loop is not MainLoop removed || ReferenceEquals(removed, PrimaryLoop) || !_loops.Remove(removed))
                return false;

            removed.RequestStop();
            foreach (var host in _loops) host.Detach(removed);

            _threads.Remove(removed, out thread);
            if (_ownClocks.Remove(removed)) _time.Remove(removed.Clock);
        }

        if (thread is not null && thread != Thread.CurrentThread) thread.Join();
        return true;
    }

    /// <summary>
    /// Starts the dedicated loops: the engine is running.
    /// </summary>
    public void Start()
    {
        lock (_gate)
        {
            _started = true;

            foreach (var loop in _loops.Where(loop => !ReferenceEquals(loop, PrimaryLoop) &&
                                                      loop.Options.Threading == MainLoopThreading.Dedicated &&
                                                      !_threads.ContainsKey(loop)))
                StartThread(loop);
        }
    }

    /// <summary>
    /// Stops every loop and waits for the dedicated ones to end.
    /// </summary>
    public void Stop()
    {
        List<Thread> threads;

        lock (_gate)
        {
            _stopped = true;
            foreach (var loop in _loops)
                if (!loop.IsPrimary)
                    loop.RequestStop();
            threads = _threads.Values.ToList();
            _threads.Clear();
        }

        foreach (var thread in threads) thread.Join();
        foreach (var loop in Loops.OfType<MainLoop>()) loop.MarkStopped();
    }

    private void StartThread(MainLoop loop)
    {
        var thread = new Thread(() => RunDedicated(loop))
        {
            Name = $"Stellar loop: {loop.Name}",
            IsBackground = true
        };

        _threads[loop] = thread;
        thread.Start();
    }

    private void RunDedicated(MainLoop loop)
    {
        var dispatcher = (MainThreadDispatcher)loop.Dispatcher;
        dispatcher.Bind();
        SynchronizationContext.SetSynchronizationContext(new MainThreadSynchronizationContext(dispatcher));

        var result = _catcher.Run(loop.Run, $"the loop {loop.Name}");
        if (result.Failed) _control.RequestStop(result.Exception!);
    }
}
