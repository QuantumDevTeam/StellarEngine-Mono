using Stellar.Core.Data.Context;
using Stellar.Core.EntryPoints;
using Stellar.Core.Failures;
using Stellar.Core.Failures.BaseLevels;
using Stellar.Core.Failures.Handlers;
using Stellar.Core.Global;
using Stellar.Core.Quantization;
using Stellar.Kernel.Configuration;
using Stellar.Kernel.EntryPoint;
using Stellar.Kernel.Failures;
using Stellar.Kernel.Failures.Handlers;
using Stellar.Kernel.Lifecycle;
using Stellar.Kernel.LoggingSystem;
using Stellar.Kernel.Quantization;

namespace Stellar.Core.Tests;

public class FailureTests
{
    private sealed class RecordingLogger()
        : Quant<MetaQuant>(new MetaQuant()), ILogger
    {
        public List<string> Messages { get; } = [];

        public void Log(LogLevel level, string message)
        {
            Messages.Add(message);
        }

        public void LogWithoutFormat(string message)
        {
            Messages.Add(message);
        }

        public void Register(IQuantumObject registry)
        {
        }

        public void Unregister(IQuantumObject registry)
        {
        }

        public void Dispose()
        {
        }
    }

    private sealed class RecordingEngineControl : IEngineControl
    {
        public EngineState State => EngineState.Running;

        public StopReason? Requested { get; private set; }

        public void RequestStop(StopReason reason, IFailure? failure = null)
        {
            Requested = reason;
        }
    }

    private sealed class CountingHandler()
        : RegistrableQuant<CountingHandler, MetaQuant>(new MetaQuant()), IFailureHandler
    {
        public int Handled { get; private set; }

        public ILogger? Logger => null;

        public bool Handle<TContext>(ref readonly TContext context)
            where TContext : IFailureContext, allows ref struct
        {
            Handled++;
            return false;
        }
    }

    private static Failure MakeFailure(IFailureLevel level, FailureType type = FailureType.Core)
    {
        return new Failure
        {
            Message = "boom",
            InnerException = null,
            Type = type,
            Level = level,
            Source = "test"
        };
    }

    private static bool Handle(FailureHandler handler, IFailureLevel level)
    {
        var context = new FailureContext(null, MakeFailure(level));
        return handler.Handle(in context);
    }

    [Fact]
    public void Handler_AppliesTheLevel()
    {
        var meta = new FailureDispatcherMeta();
        var logger = new RecordingLogger();
        var engine = new RecordingEngineControl();
        var handler = new FailureHandler(logger, engine);

        Handle(handler, new NonCritical(meta)).Should().BeFalse();
        logger.Messages.Should().BeEmpty();

        Handle(handler, new Warning(meta)).Should().BeFalse();
        logger.Messages.Should().HaveCount(1);

        Handle(handler, new Error(meta)).Should().BeTrue();
        engine.Requested.Should().BeNull();

        Handle(handler, new CriticalError(meta)).Should().BeTrue();
        engine.Requested.Should().Be(StopReason.CriticalError);
    }

    [Fact]
    public void Handler_IgnoresDisabledLevel()
    {
        var level = new Error(new FailureDispatcherMeta()) { IsEnabled = false };

        Handle(new FailureHandler(), level).Should().BeFalse();
    }

    [Fact]
    public void TwoDispatchers_MayHaveLevelsWithTheSameName()
    {
        // Regression: a level used to take the global label name and the second one failed
        var action = () =>
        {
            _ = new Error(new FailureDispatcherMeta());
            _ = new Error(new FailureDispatcherMeta());
        };

        action.Should().NotThrow();
    }

    [Fact]
    public void Dispatcher_RoutesByTypeAndLevel()
    {
        var meta = new FailureDispatcherMeta();
        var provider = (FailureHandlerProvider)meta.FailureHandlerProvider;
        var error = new Error(meta);
        var warning = new Warning(meta);

        var coreErrors = new CountingHandler();
        var everything = new CountingHandler();
        provider.RegisterHandler(new FailureBinding(FailureType.Core, error), coreErrors);
        provider.RegisterDefaultHandler(everything);

        var dispatcher = new FailureDispatcher(meta);

        var coreError = new FailureContext(null, MakeFailure(error));
        var coreWarning = new FailureContext(null, MakeFailure(warning));
        var otherError = new FailureContext(null, MakeFailure(error, FailureType.Logging));

        dispatcher.Dispatch(in coreError);
        dispatcher.Dispatch(in coreWarning);
        dispatcher.Dispatch(in otherError);

        coreErrors.Handled.Should().Be(1);
        everything.Handled.Should().Be(3);
    }

    [Fact]
    public void Dispatcher_ReportsWhenAnyHandlerStops()
    {
        var meta = new FailureDispatcherMeta();
        ((FailureHandlerProvider)meta.FailureHandlerProvider).RegisterDefaultHandler(new FailureHandler());

        var dispatcher = new FailureDispatcher(meta);
        var stopping = new FailureContext(null, MakeFailure(new Error(meta)));
        var passing = new FailureContext(null, MakeFailure(new Warning(meta)));

        dispatcher.Dispatch(in stopping).Should().BeTrue();
        dispatcher.Dispatch(in passing).Should().BeFalse();
    }

    [Fact]
    public void Composite_JoinsProviders()
    {
        var composite = new CompositeFailureHandlerProvider();
        var first = new FailureHandlerProvider(new HandlerProviderMeta());
        var second = new FailureHandlerProvider(new HandlerProviderMeta());
        first.RegisterDefaultHandler(new CountingHandler());
        second.RegisterDefaultHandler(new CountingHandler());

        composite.AddProvider(first);
        composite.AddProvider(second);

        var failure = MakeFailure(new Error(new FailureDispatcherMeta()));
        composite.GetHandlers(failure).Should().HaveCount(2);

        composite.RemoveProvider(first);
        composite.GetHandlers(failure).Should().HaveCount(1);
    }

    [Fact]
    public void Catch_RunsBlocksInOrder()
    {
        var log = new List<string>();

        new FailureCatch()
            .Try(_ =>
            {
                log.Add("try");
                throw new InvalidOperationException();
            })
            .Except<ArgumentException>((_, _) => log.Add("wrong"))
            .Except<InvalidOperationException>((_, _) => log.Add("except"))
            .Finally(_ => log.Add("finally"))
            .Handle();

        log.Should().Equal("try", "except", "finally");
    }

    [Fact]
    public void Catch_DoesNothingUntilHandle()
    {
        var ran = false;

        new FailureCatch().Try(_ => ran = true);

        ran.Should().BeFalse();
    }

    [Fact]
    public void Catch_ExposesCaughtException()
    {
        var exception = new InvalidOperationException();

        var failureCatch = new FailureCatch()
            .Try(_ => throw exception)
            .Except<Exception>((_, _) => { })
            .Handle();

        failureCatch.Caught.Should().BeSameAs(exception);
    }

    [Fact]
    public void Catch_RethrowsUnclaimedException_WhenThereIsNoDispatcher()
    {
        var finallyRan = false;

        var action = () => new FailureCatch()
            .Try(_ => throw new InvalidOperationException())
            .Finally(_ => finallyRan = true)
            .Handle();

        action.Should().Throw<InvalidOperationException>();
        finallyRan.Should().BeTrue();
    }

    [Fact]
    public void Catch_SendsUnclaimedExceptionToDispatcher()
    {
        var meta = new FailureDispatcherMeta();
        var handler = new CountingHandler();
        ((FailureHandlerProvider)meta.FailureHandlerProvider).RegisterDefaultHandler(handler);

        new FailureCatch(new FailureDispatcher(meta), new Error(meta), FailureType.Core)
            .Try(_ => throw new InvalidOperationException())
            .Handle();

        handler.Handled.Should().Be(1);
    }
}

public class EntryPointTests
{
    [Fact]
    public void EntryPoint_IsQuantAndDisposable()
    {
        typeof(EntryPoint).Should().BeAssignableTo<StellarEntryPoint>();
        typeof(EntryPoint).Should().BeAssignableTo<IQuant>();
        typeof(EntryPoint).Should().BeAssignableTo<IDisposable>();
    }

    [Fact]
    public void Constructor_WithNullConfiguration_Throws()
    {
        var action = () => new EntryPoint(null!);

        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Run_BringsUpFailurePipeline_AndDisposeTakesItDown()
    {
        var entry = new EntryPoint(new RuntimeConfiguration());
        var context = new RunContext(null);

        entry.Run(in context).Should().Be(0);

        Engine.FailureDispatcher.Should().NotBeNull();
        Engine.FailureLevels.Error.Should().NotBeNull();

        var stopContext = new StopContext(null, StopReason.Regular);
        entry.RequestStop(in stopContext);
        entry.Dispose();

        Engine.FailureDispatcher.Should().BeNull();
        Engine.FailureLevels.Error.Should().BeNull();
    }

    [Fact]
    public void EntryPoint_HasLabel()
    {
        using var entry = new EntryPoint(new RuntimeConfiguration());

        entry.Label.Name.Should().Be("S.C/MainEntry");
        entry.Meta.Should().BeSameAs(entry.Label);
        entry.Metas.Should().ContainSingle();
    }
}
