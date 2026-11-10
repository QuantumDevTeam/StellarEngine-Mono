using Stellar.Core.Global;
using Stellar.Core.Data.Context;
using Stellar.Core.Data.Registry;
using Stellar.Core.Quantization;
using Stellar.Core.Services;
using Stellar.Kernel;
using Stellar.Kernel.Data.Context;
using Stellar.Kernel.Quantization;

namespace Stellar.Core.Tests;

public class QuantTests
{
    private sealed class PlayerMeta : MetaQuant;

    private sealed class SharedHandlerMeta : MetaQuant;

    private sealed class Player(PlayerMeta meta) : Quant<PlayerMeta>(meta);

    private sealed class Gun(MetaQuant meta) : Quant<MetaQuant>(meta);

    private sealed class Service()
        : RegistrableQuant<Service, MetaQuant>(new MetaQuant());

    private sealed class SharedMeta(IIdentifier id)
        : RegistrableMetaQuant<SharedMeta>(id);

    [Fact]
    public void Quant_TakesIdentityFromPrimaryMeta()
    {
        var meta = new PlayerMeta();
        var player = new Player(meta);

        player.UID.Should().BeSameAs(meta.UID);
        player.Meta.Should().BeSameAs(meta);
        player.MetaQuant.Should().BeSameAs(meta);
    }

    [Fact]
    public void OneMeta_CanServeSeveralQuants()
    {
        var handler = new SharedHandlerMeta();
        var player = new Player(new PlayerMeta());
        var gun = new Gun(new MetaQuant());

        player.Attach(handler).Should().BeTrue();
        gun.Attach(handler).Should().BeTrue();

        player.FindMeta<SharedHandlerMeta>().Should().BeSameAs(handler);
        gun.FindMeta<SharedHandlerMeta>().Should().BeSameAs(handler);
    }

    [Fact]
    public void Attach_IgnoresDuplicates()
    {
        var handler = new SharedHandlerMeta();
        var player = new Player(new PlayerMeta());

        player.Attach(handler);

        player.Attach(handler).Should().BeFalse();
        player.Metas.Should().HaveCount(2);
    }

    [Fact]
    public void Metas_StartWithPrimary()
    {
        var meta = new PlayerMeta();
        var player = new Player(meta);
        player.Attach(new SharedHandlerMeta());

        player.Metas[0].Should().BeSameAs(meta);
    }

    [Fact]
    public void Detach_KeepsPrimary()
    {
        var meta = new PlayerMeta();
        var handler = new SharedHandlerMeta();
        var player = new Player(meta);
        player.Attach(handler);

        player.Detach(meta).Should().BeFalse();
        player.Detach(handler).Should().BeTrue();

        player.Metas.Should().ContainSingle().Which.Should().BeSameAs(meta);
        player.FindMeta<SharedHandlerMeta>().Should().BeNull();
    }

    [Fact]
    public void RegistrableQuant_ComesAndGoesFromItsRegistry()
    {
        var service = new Service();

        CoreEngine.Current.Registry.Quants<Service>().Get(service.UID).Should().BeSameAs(service);

        service.Dispose();

        CoreEngine.Current.Registry.Quants<Service>().Exists(service.UID).Should().BeFalse();
    }

    [Fact]
    public void RegistrableMetaQuant_ComesAndGoesFromItsRegistry()
    {
        var meta = new SharedMeta(Identifier.Create());

        CoreEngine.Current.Registry.MetaQuants<SharedMeta>().Get(meta.UID).Should().BeSameAs(meta);

        meta.Dispose();

        CoreEngine.Current.Registry.MetaQuants<SharedMeta>().Exists(meta.UID).Should().BeFalse();
    }

    [Fact]
    public void TimedMetaQuant_Expires()
    {
        var meta = new TimedMetaQuant(TimeSpan.FromHours(1));
        meta.IsExpired.Should().BeFalse();

        meta.SetLifetime(TimeSpan.FromSeconds(1), DateTime.UtcNow.AddHours(-1));
        meta.IsExpired.Should().BeTrue();

        meta.ResetLifetime();
        meta.IsExpired.Should().BeFalse();

        new TimedMetaQuant().IsExpired.Should().BeFalse();
    }
}

public class ContextTests
{
    private struct Payload : IContextData
    {
        public int Value;
    }

    private static int ReadThroughContract<TContext>(ref readonly TContext context)
        where TContext : IContext<Payload>, allows ref struct
    {
        return context.Data.Value;
    }

    [Fact]
    public void TypedContext_ExposesDataByReference()
    {
        var payload = new Payload { Value = 1 };
        var context = new Context<Payload>(null, in payload);

        payload.Value = 2;

        ReadThroughContract(in context).Should().Be(2);
    }
}

public class ServiceRegistryTests
{
    private interface IService;

    private sealed class Service : IService;

    [Fact]
    public void Services_ArePublishedByContract()
    {
        var registry = new ServiceRegistry();
        var service = new Service();

        registry.Find<IService>().Should().BeNull();

        registry.Set<IService>(service);

        registry.Get<IService>().Should().BeSameAs(service);
        registry.Find<Service>().Should().BeNull();
    }

    [Fact]
    public void Get_FailsForUnpublishedService()
    {
        var action = () => new ServiceRegistry().Get<IService>();

        action.Should().Throw<InvalidOperationException>().WithMessage("*IService*");
    }

    [Fact]
    public void Remove_UnpublishesService()
    {
        var registry = new ServiceRegistry();
        registry.Set<IService>(new Service());

        registry.Remove<IService>().Should().BeTrue();
        registry.Remove<IService>().Should().BeFalse();
    }
}
