using Stellar.Core.Data.Registry;
using Stellar.Core.Global;
using Stellar.Core.Quantization;
using Stellar.Kernel;
using Stellar.Kernel.Data.Registry;
using Stellar.Kernel.Lifecycle;
using Stellar.Kernel.Quantization;

namespace Stellar.Core.Tests;

public class RegistryTests
{
    [StellarRegistry]
    public interface IDamageable
        : IRegistrableQuant;

    private class Actor()
        : RegistrableQuant<Actor, MetaQuant>(new MetaQuant());

    [StellarRegistry]
    private sealed class Player
        : Actor, IDamageable;

    private sealed class Prop()
        : RegistrableQuant<Prop, MetaQuant>(new MetaQuant())
    {
        protected override bool RegistersImplicitly => false;
    }

    [Fact]
    public void ADerivedQuant_IsInTheRegistryOfItsBase_AndFoundAsWhatItIs()
    {
        using var player = new Player();
        var actors = CoreEngine.Current.Registry.Of<Actor>();

        actors.Get(player.UID).Should().BeSameAs(player);
        actors.Get<Player>(player.UID).Should().BeSameAs(player);
    }

    [Fact]
    public void AGetOfTheWrongType_IsNull_NotAGuess()
    {
        using var actor = new Actor();

        CoreEngine.Current.Registry.Of<Actor>().Get<Player>(actor.UID).Should().BeNull();
    }

    [Fact]
    public void MarkedTypes_HaveRegistriesOfTheirOwn()
    {
        using var player = new Player();
        var registries = CoreEngine.Current.Registry;

        registries.Of<Player>().Get(player.UID).Should().BeSameAs(player);
        registries.Of<IDamageable>().Get(player.UID).Should().BeSameAs(player);
        player.Registrations.Should().HaveCount(3)
            .And.Contain([registries.Of<Actor>(), registries.Of<Player>(), registries.Of<IDamageable>()]);
    }

    [Fact]
    public void KeysAndValues_FollowTheRegistry()
    {
        var registry = CoreEngine.Current.Registry.Of<Crate>();
        var first = new Crate();
        var values = registry.Values;
        registry.Values.Should().BeSameAs(values);

        using var second = new Crate();
        first.Dispose();

        registry.Values.Should().NotBeSameAs(values).And.Contain(second).And.NotContain(first);
        registry.Keys.Should().Contain(second.UID).And.NotContain(first.UID);
    }

    private sealed class Crate()
        : RegistrableQuant<Crate, MetaQuant>(new MetaQuant());

    [Fact]
    public void Dispose_LeavesEveryRegistry()
    {
        var player = new Player();
        var registries = CoreEngine.Current.Registry;

        player.Dispose();

        registries.Of<Actor>().Exists(player.UID).Should().BeFalse();
        registries.Of<Player>().Exists(player.UID).Should().BeFalse();
        registries.Of<IDamageable>().Exists(player.UID).Should().BeFalse();
        player.Registrations.Should().BeEmpty();
    }

    [Fact]
    public void AnyRegistry_CanTakeAnObject_AndTheObjectKnows()
    {
        using var prop = new Prop();
        var mine = new Registry<Prop>();

        prop.Registrations.Should().BeEmpty();
        CoreEngine.Current.Registry.Of<Prop>().Exists(prop.UID).Should().BeFalse();

        mine.Register(prop).Should().BeTrue();
        mine.Register(prop).Should().BeFalse();
        prop.Registrations.Should().ContainSingle().Which.Should().BeSameAs(mine);

        mine.Pop(prop.UID).Should().BeSameAs(prop);
        prop.Registrations.Should().BeEmpty();
    }

    [Fact]
    public void Dispose_LeavesARegistryMadeByHand()
    {
        var prop = new Prop();
        var mine = new Registry<Prop>();
        mine.Register(prop);

        prop.Dispose();

        mine.Exists(prop.UID).Should().BeFalse();
    }

    [Fact]
    public void ATypeWithoutRegistrableObjects_HasNoRegistry()
    {
        FluentActions.Invoking(() => CoreEngine.Current.Registry.Of(typeof(string))).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RegistriesOfTwoEngines_AreApart()
    {
        var first = new ScopeEngine("First");
        var second = new ScopeEngine("Second");

        Actor actor;
        using (EngineScope.Enter(first)) actor = new Actor();

        CoreEngine.For(first).Registry.Of<Actor>().Exists(actor.UID).Should().BeTrue();
        CoreEngine.For(second).Registry.Of<Actor>().Exists(actor.UID).Should().BeFalse();

        // Disposed anywhere, it leaves the registries it is really in
        using (EngineScope.Enter(second)) actor.Dispose();
        CoreEngine.For(first).Registry.Of<Actor>().Exists(actor.UID).Should().BeFalse();

        CoreEngine.Release(first);
        CoreEngine.Release(second);
    }
}

[Collection(DetachedPolicyCollection.Name)]
public class DetachedPolicyTests
{
    [Fact]
    public void Outside_AnyEngine_TheDefaultIsUsed_UnlessThePolicySaysOtherwise()
    {
        using (EngineScope.Enter(null))
        {
            EngineScope.Resolve().Should().BeNull();
            CoreEngine.Current.Should().BeSameAs(CoreEngine.Default);

            var previous = EngineScope.Detached;
            EngineScope.Detached = DetachedPolicy.Throw;
            try
            {
                FluentActions.Invoking(() => CoreEngine.Current).Should().Throw<InvalidOperationException>();

                var engine = new ScopeEngine("Strict");
                using (EngineScope.Enter(engine)) CoreEngine.Current.Instance.Should().BeSameAs(engine);
                CoreEngine.Release(engine);
            }
            finally
            {
                EngineScope.Detached = previous;
            }
        }
    }
}

/// <summary>
/// The policy is one for the process: the test that changes it runs alone.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class DetachedPolicyCollection
{
    public const string Name = "The detached policy of the process";
}

internal sealed class ScopeEngine(string name)
    : IEngineInstance
{
    public string Name { get; } = name;

    public IEngineControl Control => throw new NotSupportedException();

    public Kernel.Timing.ITimeSystem Time => throw new NotSupportedException();

    public IMainLoops Loops => throw new NotSupportedException();

    public Kernel.Threading.IMainThreadDispatcher Dispatcher => throw new NotSupportedException();

    public Kernel.Services.IServiceCatalog? Services => CoreEngine.For(this).Services;
}
