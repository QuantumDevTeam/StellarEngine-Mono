using System.Collections.Concurrent;
using Stellar.Kernel.Lifecycle;
using Stellar.Kernel.Services;
using Stellar.Kernel.Threading;
using Stellar.Kernel.Timing;
using Stellar.Runtime.Loop;
using Stellar.Runtime.Threading;
using Stellar.Runtime.Timing;

namespace Stellar.Runtime.Lifecycle;

/// <summary>
/// One engine run by a <see cref="Hosting.RuntimeHost"/>: its control, clocks, loops and services belong to it alone.
/// </summary>
public sealed class EngineInstance
    : IEngineInstance, IDisposable
{
    private static readonly ConcurrentDictionary<string, EngineInstance> Running = new(StringComparer.Ordinal);

    private readonly Func<IServiceRegistry?> _services;

    /// <summary>
    /// Initializes a new instance of the <see cref="EngineInstance"/> class.
    /// </summary>
    /// <param name="name">The name; made unique within the process if it is taken (<c>Demo#2</c>).</param>
    /// <param name="control">The control of the engine.</param>
    /// <param name="time">The clocks of the engine.</param>
    /// <param name="loops">The loops of the engine.</param>
    /// <param name="dispatcher">The dispatcher of the main thread.</param>
    /// <param name="services">The services of the engine, once there are any.</param>
    public EngineInstance(string name, EngineControl control, TimeSystem time, MainLoops loops,
        MainThreadDispatcher dispatcher, Func<IServiceRegistry?> services)
    {
        Control = control;
        Time = time;
        Loops = loops;
        Dispatcher = dispatcher;
        _services = services;
        Name = Register(name);
    }

    /// <summary>
    /// Gets the engines of the process that are not disposed.
    /// </summary>
    public static IReadOnlyCollection<IEngineInstance> All => Running.Values.ToList();

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public IEngineControl Control { get; }

    /// <inheritdoc/>
    public ITimeSystem Time { get; }

    /// <inheritdoc/>
    public IMainLoops Loops { get; }

    /// <inheritdoc/>
    public IMainThreadDispatcher Dispatcher { get; }

    /// <inheritdoc/>
    public IServiceRegistry? Services => _services();

    /// <inheritdoc/>
    public void Dispose()
    {
        Running.TryRemove(new KeyValuePair<string, EngineInstance>(Name, this));
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"Engine {Name}";
    }

    private string Register(string name)
    {
        var candidate = string.IsNullOrEmpty(name) ? "Engine" : name;

        for (var index = 2; !Running.TryAdd(candidate, this); index++)
            candidate = $"{name}#{index}";

        return candidate;
    }
}
