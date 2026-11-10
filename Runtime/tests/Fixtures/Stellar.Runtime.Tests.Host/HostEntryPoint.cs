using Stellar.Kernel.Configuration;
using Stellar.Kernel.EntryPoint;
using Stellar.Kernel.Failures;
using Stellar.Kernel.Lifecycle;
using Stellar.Kernel.Quantization;
using Stellar.Kernel.Services;

namespace Stellar.Runtime.Tests.Host;

/// <summary>
/// Owns the services, like Stellar.Core, and reports failures by writing them down.
/// </summary>
[StellarEntryPoint]
public sealed class HostEntryPoint(RuntimeConfiguration configuration)
    : StellarEntryPoint("Tests/Host", configuration), IServiceHost
{
    public IServiceRegistry Services { get; } = new Registry();

    public override int Run<TContext>(ref readonly TContext context)
    {
        Events.Add($"host:run control={Services.Find<IEngineControl>() is not null}");
        Services.Set<IFailureReporter>(new Reporter());
        return 0;
    }

    public override void RequestStop<TContext>(ref readonly TContext context)
    {
        Events.Add($"host:stop {context.Reason}");
    }

    public override void Dispose()
    {
        Services.Remove<IFailureReporter>();
        Events.Add("host:dispose");
    }

    private sealed class Reporter : IFailureReporter
    {
        public bool Report(Exception exception, FailureType type, bool critical, IQuantumObject? sender = null)
        {
            Events.Add($"report:{type}:{exception.Message}");
            return critical;
        }
    }

    private sealed class Registry : IServiceRegistry
    {
        private readonly Dictionary<Type, object> _services = new();

        public void Set<TService>(TService service) where TService : class
        {
            lock (_services)
            {
                _services[typeof(TService)] = service;
            }
        }

        public TService? Find<TService>() where TService : class
        {
            lock (_services)
            {
                return _services.GetValueOrDefault(typeof(TService)) as TService;
            }
        }

        public TService Get<TService>() where TService : class
        {
            return Find<TService>() ?? throw new InvalidOperationException(typeof(TService).Name);
        }

        public bool Remove<TService>() where TService : class
        {
            lock (_services)
            {
                return _services.Remove(typeof(TService));
            }
        }
    }
}

/// <summary>
/// What the fixtures did, for the tests to read: a list the test puts in the data of the process.
/// </summary>
public static class Events
{
    public const string Key = "Stellar.Runtime.Tests.Events";

    public static void Add(string message)
    {
        if (AppContext.GetData(Key) is List<string> events)
            lock (events)
            {
                events.Add(message);
            }
    }
}
