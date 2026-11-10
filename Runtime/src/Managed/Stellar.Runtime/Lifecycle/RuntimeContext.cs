using Stellar.Kernel.Configuration;
using Stellar.Kernel.Failures;
using Stellar.Kernel.Lifecycle;
using Stellar.Kernel.LoggingSystem;
using Stellar.Kernel.Quantization;
using Stellar.Kernel.Services;
using Stellar.Runtime.Configuration;
using Stellar.Runtime.Diagnostics;
using Stellar.Runtime.Failures;
using Stellar.Runtime.Hosting;
using Stellar.Runtime.Loop;
using Stellar.Runtime.Modules;
using Stellar.Runtime.Modules.Loading;
using Stellar.Runtime.Threading;
using Stellar.Runtime.Timing;

namespace Stellar.Runtime.Lifecycle;

/// <summary>
/// Everything the stages of the runtime share: what each one sets up, the next ones use.
/// </summary>
public sealed class RuntimeContext
    : IQuantumObject
{
    private readonly object _gate = new();
    private readonly List<LoadedModule> _started = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="RuntimeContext"/> class.
    /// </summary>
    /// <param name="launch">What is run.</param>
    /// <param name="log">The log of the runtime.</param>
    /// <param name="dispatcher">The dispatcher of the main thread.</param>
    /// <param name="control">The control of the engine.</param>
    public RuntimeContext(RuntimeLaunch launch, RuntimeLog log, MainThreadDispatcher dispatcher, EngineControl control)
    {
        Launch = launch;
        Log = log;
        Dispatcher = dispatcher;
        Control = control;
        Settings = RuntimeSettings.Create(null, launch.Arguments);
        Time = new TimeSystem();
        Catchers = new RuntimeCatchers(log,
            () => Services?.Find<IFailureReporter>(),
            () => Services?.Find<ILogger>() is not null);
        Loops = new MainLoops(new MainLoopOptions { Name = "Main" }, Time, dispatcher, Catchers.MainLoop, control);
        Engine = new EngineInstance(launch.ApplicationName, control, Time, Loops, dispatcher, () => Services);
    }

    /// <summary>
    /// Gets what is run.
    /// </summary>
    public RuntimeLaunch Launch { get; }

    /// <summary>
    /// Gets the log of the runtime.
    /// </summary>
    public RuntimeLog Log { get; }

    /// <summary>
    /// Gets the dispatcher of the main thread.
    /// </summary>
    public MainThreadDispatcher Dispatcher { get; }

    /// <summary>
    /// Gets the control of the engine.
    /// </summary>
    public EngineControl Control { get; }

    /// <summary>
    /// Gets the catchers of the runtime.
    /// </summary>
    public RuntimeCatchers Catchers { get; }

    /// <summary>
    /// Gets or sets what reads runtime configurations.
    /// </summary>
    public RuntimeConfigurationLocator ConfigurationLocator { get; set; } = new();

    /// <summary>
    /// Gets or sets where the assemblies of modules live.
    /// </summary>
    public IAssemblyPlacementPolicy PlacementPolicy { get; set; } = new DefaultAssemblyPlacementPolicy();

    /// <summary>
    /// Gets or sets the runtime configuration of the application.
    /// </summary>
    public RuntimeConfiguration? Configuration { get; set; }

    /// <summary>
    /// Gets or sets the settings of the runtime.
    /// </summary>
    public RuntimeSettings Settings { get; set; }

    /// <summary>
    /// Gets or sets the modules of the application.
    /// </summary>
    public ModulePlan? Plan { get; set; }

    /// <summary>
    /// Gets or sets the contexts the assemblies are loaded in.
    /// </summary>
    public ModuleContexts? Contexts { get; set; }

    /// <summary>
    /// Gets the loaded modules, in load order, without the application.
    /// </summary>
    public List<LoadedModule> Modules { get; } = [];

    /// <summary>
    /// Gets or sets the application.
    /// </summary>
    public LoadedModule? Application { get; set; }

    /// <summary>
    /// Gets or sets the services of the engine, once the module that owns them is started.
    /// </summary>
    public IServiceRegistry? Services { get; set; }

    /// <summary>
    /// Gets or sets the clocks of the engine.
    /// </summary>
    public TimeSystem Time { get; set; }

    /// <summary>
    /// Gets or sets the loops of the engine.
    /// </summary>
    public MainLoops? Loops { get; set; }

    /// <summary>
    /// Gets the primary loop, once the engine starts.
    /// </summary>
    public MainLoop? MainLoop => Loops?.PrimaryLoop;

    /// <summary>
    /// Gets or sets the engine: the runtime runs everything in its scope (<see cref="Kernel.Lifecycle.EngineScope"/>).
    /// </summary>
    public EngineInstance? Engine { get; set; }

    /// <summary>
    /// Gets or sets the exit code an entry point asked for.
    /// </summary>
    public int? RequestedExitCode { get; set; }

    /// <summary>
    /// Gets the modules started so far, in the order they started.
    /// </summary>
    public IReadOnlyList<LoadedModule> Started
    {
        get
        {
            lock (_gate)
            {
                return _started.ToList();
            }
        }
    }

    /// <summary>
    /// Records that a module started. Safe from any thread.
    /// </summary>
    /// <param name="module">The module.</param>
    public void MarkStarted(LoadedModule module)
    {
        lock (_gate)
        {
            _started.Add(module);
        }
    }

    /// <summary>
    /// Records that a module stopped. Safe from any thread.
    /// </summary>
    /// <param name="module">The module.</param>
    public void MarkStopped(LoadedModule module)
    {
        lock (_gate)
        {
            _started.Remove(module);
        }
    }
}
