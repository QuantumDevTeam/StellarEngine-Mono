using System.Diagnostics.CodeAnalysis;
using Stellar.Kernel.Configuration;
using Stellar.Kernel.Data.Catalog;
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
    public IServiceCatalog Services { get; } = new ServiceCatalog();

    public override int Run<TContext>(ref readonly TContext context)
    {
        Events.Add($"host:run control={Services.Find<IEngineControl>() is not null}");
        Services.Set<IFailureSystem>(new Recorder());
        return 0;
    }

    public override void RequestStop<TContext>(ref readonly TContext context)
    {
        Events.Add($"host:stop {context.Reason}");
    }

    public override void Dispose()
    {
        Services.Remove<IFailureSystem>();
        Events.Add("host:dispose");
    }

    // Writes failures down; only a critical one stops the engine, so a bad frame lets the loop go on
    private sealed class Recorder : IFailureSystem
    {
        public ICatalog<FailureLevel, FailureReaction> Reactions { get; } = new SimpleCatalog<FailureLevel, FailureReaction>();

        public ICatalog<(FailureKind Kind, FailureLevel Level), FailureReaction> KindReactions { get; } =
            new SimpleCatalog<(FailureKind Kind, FailureLevel Level), FailureReaction>();

        public FailureReaction ReactionTo(Failure failure) =>
            failure.Level == FailureLevel.CriticalError ? FailureReactions.Default(failure.Level) : FailureReaction.None;

        public FailureReaction Report(Failure failure)
        {
            Events.Add($"report:{failure.Level}:{failure.Exception?.Message ?? failure.Message}");
            failure.MarkReported(ReactionTo(failure));
            return failure.Reaction!.Value;
        }

        public IDisposable AddHandler(IFailureHandler handler, FailureFilter filter = default) => throw new NotSupportedException();
    }

    // The Kernel only: catalogs of its own, as Stellar.Core would have
    private sealed class ServiceCatalog : SimpleCatalog<Type, object>, IServiceCatalog;

    private class SimpleCatalog<TKey, TValue> : ICatalog<TKey, TValue>
        where TKey : notnull
    {
        private readonly Dictionary<TKey, TValue> _items = new();

        public int Count { get { lock (_items) return _items.Count; } }

        public IReadOnlyCollection<TKey> Keys { get { lock (_items) return _items.Keys.ToArray(); } }

        public IReadOnlyCollection<TValue> Values { get { lock (_items) return _items.Values.ToArray(); } }

        public bool Contains(TKey key) { lock (_items) return _items.ContainsKey(key); }

        public bool TryGet(TKey key, [MaybeNullWhen(false)] out TValue value) { lock (_items) return _items.TryGetValue(key, out value); }

        public TValue Get(TKey key) => TryGet(key, out var value) ? value : throw new KeyNotFoundException(key.ToString());

        public void Set(TKey key, TValue value) { lock (_items) _items[key] = value; }

        public bool TryAdd(TKey key, TValue value) { lock (_items) return _items.TryAdd(key, value); }

        public bool Remove(TKey key) { lock (_items) return _items.Remove(key); }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() { lock (_items) return _items.ToList().GetEnumerator(); }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
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
