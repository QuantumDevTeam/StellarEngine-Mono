using Stellar.Core.Data.Collections;
using Stellar.Core.EntryPoints;
using Stellar.Core.Global;
using Stellar.Core.Native;
using Stellar.Core.Quantization;
using Stellar.Kernel;
using Stellar.Kernel.Configuration;
using Stellar.Kernel.EntryPoint;
using Stellar.Kernel.Lifecycle;
using Stellar.Kernel.Services;
using Stellar.Kernel.Threading;
using Stellar.Kernel.Timing;

namespace Stellar.Core.Tests;

/// <summary>
/// Two engines in one process share nothing of Stellar.Core: services, failure levels, registries, native ones too.
/// </summary>
public class EngineInstanceTests
{
    [Fact]
    public void TwoEngines_HaveTheirOwnServicesAndRegistries()
    {
        var editor = new FakeEngine("Editor");
        var game = new FakeEngine("Game");
        Table<Service> table;

        using (EngineScope.Enter(editor))
        {
            table = new Table<Service>();
            Engine.Services.Set(new Marker("editor"));
        }

        using (EngineScope.Enter(game))
        {
            Engine.Services.Find<Marker>().Should().BeNull();
            Engine.Registry.DataContainers.Exists(table.UID).Should().BeFalse();
        }

        CoreEngine.For(editor).Services.Find<Marker>()!.Name.Should().Be("editor");
        CoreEngine.For(editor).Native.Should().NotBeSameAs(CoreEngine.For(game).Native);
        CoreEngine.For(editor).Registry.DataContainers.Exists(table.UID).Should().BeTrue();
        CoreEngine.Default.Registry.DataContainers.Exists(table.UID).Should().BeFalse();

        CoreEngine.Release(editor).Should().BeTrue();
        CoreEngine.Release(game).Should().BeTrue();
    }

    [Fact]
    public void Outside_AnyEngine_TheDefaultIsUsed()
    {
        using (EngineScope.Enter(null))
        {
            CoreEngine.Current.Should().BeSameAs(CoreEngine.Default);
            CoreContext.Current.Should().BeSameAs(CoreContext.Default);
            CoreContext.Default.IsDefault.Should().BeTrue();
        }
    }

    [Fact]
    public async Task TheScope_FollowsTheWork_IntoAwaitAndOtherThreads()
    {
        var engine = new FakeEngine("Flowing");

        using (EngineScope.Enter(engine))
        {
            await Task.Yield();
            CoreEngine.Current.Instance.Should().BeSameAs(engine);

            var seen = await Task.Run(() => CoreEngine.Current.Instance);
            seen.Should().BeSameAs(engine);

            IEngineInstance? onThread = null;
            var thread = new Thread(() => onThread = EngineScope.Current);
            thread.Start();
            thread.Join();
            onThread.Should().BeSameAs(engine);
        }

        CoreEngine.Release(engine);
    }

    [Fact]
    public void AQuant_LeavesTheRegistryOfTheEngineItWasMadeIn()
    {
        var first = new FakeEngine("First");
        var second = new FakeEngine("Second");

        Service service;
        using (EngineScope.Enter(first))
        {
            service = new Service();
        }

        CoreEngine.For(first).Registry.Of<Service>().Exists(service.UID).Should().BeTrue();
        CoreEngine.For(second).Registry.Of<Service>().Exists(service.UID).Should().BeFalse();

        using (EngineScope.Enter(second))
        {
            service.Dispose();
        }

        CoreEngine.For(first).Registry.Of<Service>().Exists(service.UID).Should().BeFalse();

        CoreEngine.Release(first);
        CoreEngine.Release(second);
    }

    [Fact]
    public void Releasing_AnEngine_DisposesItsNativeRegistries()
    {
        var engine = new FakeEngine("Short");
        var native = CoreEngine.For(engine).Native;

        CoreEngine.Release(engine).Should().BeTrue();

        native.IsDisposed.Should().BeTrue();
        ((Action)(() => _ = native.Modules.Count)).Should().Throw<ObjectDisposedException>();
        CoreEngine.Release(engine).Should().BeFalse();
    }

    [Fact]
    public void TheCoreModule_RunsForTheEngineOfItsContext()
    {
        var engine = new FakeEngine("Hosted");
        var entry = new EntryPoint(new RuntimeConfiguration());
        var context = new RunContext(null, engine: engine);

        entry.Run(in context).Should().Be(0);

        entry.Core.Instance.Should().BeSameAs(engine);
        CoreEngine.For(engine).Failures.Should().NotBeNull();
        CoreEngine.Default.Failures.Should().BeNull();

        entry.Dispose();

        entry.Core.IsDisposed.Should().BeTrue();
    }

    private sealed record Marker(string Name);

    private sealed class Service()
        : RegistrableQuant<Service, MetaQuant>(new MetaQuant());

    private sealed class FakeEngine(string name)
        : IEngineInstance
    {
        public string Name { get; } = name;

        public IEngineControl Control => throw new NotSupportedException();

        public ITimeSystem Time => throw new NotSupportedException();

        public IMainLoops Loops => throw new NotSupportedException();

        public IMainThreadDispatcher Dispatcher => throw new NotSupportedException();

        public IServiceCatalog? Services => CoreEngine.For(this).Services;
    }
}
