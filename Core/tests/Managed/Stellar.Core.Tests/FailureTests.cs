using Stellar.Core.EntryPoints;
using Stellar.Core.Failures;
using Stellar.Core.Global;
using Stellar.Kernel.Configuration;
using Stellar.Kernel.EntryPoint;
using Stellar.Kernel.Failures;
using Stellar.Kernel.FileSystem;
using Stellar.Kernel.Lifecycle;
using Stellar.Kernel.LoggingSystem;
using Stellar.Kernel.Quantization;
using Stellar.Kernel.Services;

namespace Stellar.Core.Tests;

public class FailureSystemTests
{
    private sealed class RecordingLogger : ILogger
    {
        public List<(LogLevel Level, string Message)> Lines { get; } = [];

        public Kernel.Identifier UID { get; } = Kernel.Identifier.New();

        public IMetaQuant Meta => throw new NotSupportedException();

        public IReadOnlyList<IMetaQuant> Metas => [];

        public Kernel.Data.Registry.IRegistrations Registrations => throw new NotSupportedException();

        public TMeta? FindMeta<TMeta>() where TMeta : class, IMetaQuant => null;

        public void Log(LogLevel level, string message) => Lines.Add((level, message));

        public void LogWithoutFormat(string message) => Lines.Add((LogLevel.Info, message));

        public void Dispose()
        {
        }
    }

    private sealed class RecordingControl : IEngineControl
    {
        public EngineState State => EngineState.Running;

        public Failure? Stopped { get; private set; }

        public void RequestStop(StopReason reason, Failure? failure = null) => Stopped = failure;
    }

    private sealed class Recorder : IFailureHandler
    {
        public List<Failure> Heard { get; } = [];

        public void Handle(Failure failure) => Heard.Add(failure);
    }

    [Theory]
    [InlineData(FailureLevel.NonCritical, FailureReaction.None)]
    [InlineData(FailureLevel.Warning, FailureReaction.Log)]
    [InlineData(FailureLevel.Error, FailureReaction.Log | FailureReaction.StopExecution)]
    [InlineData(FailureLevel.CriticalError, FailureReaction.Log | FailureReaction.StopExecution | FailureReaction.StopEngine)]
    public void Levels_HaveTheirReactionsByDefault(FailureLevel level, FailureReaction reaction)
    {
        new FailureSystem().ReactionTo(new Failure(FailureKind.Engine, level, "x")).Should().Be(reaction);
    }

    [Fact]
    public void AReactionOfAKind_WinsOverTheOneOfTheLevel()
    {
        var system = new FailureSystem();
        var audio = new FailureKind("Audio");
        system.KindReactions.Set((audio, FailureLevel.Error), FailureReaction.Log);

        system.ReactionTo(new Failure(audio, FailureLevel.Error, "no device")).Should().Be(FailureReaction.Log);
        system.ReactionTo(new Failure(FailureKind.Engine, FailureLevel.Error, "x")).Stops().Should().BeTrue();
        audio.Name.Should().Be("audio");
    }

    [Fact]
    public void Report_Logs_TellsTheHandlers_AndStopsTheEngine()
    {
        var logger = new RecordingLogger();
        var control = new RecordingControl();
        var system = new FailureSystem(() => logger, () => control);
        var all = new Recorder();
        var fileSystemOnly = new Recorder();
        system.AddHandler(all);
        using (system.AddHandler(fileSystemOnly, new FailureFilter(FailureLevel.Warning, FailureKind.FileSystem)))
        {
            var critical = new Failure(FailureKind.Engine, FailureLevel.CriticalError, "the world ended");
            system.Report(critical).Should().HaveFlag(FailureReaction.StopEngine);

            control.Stopped.Should().BeSameAs(critical);
            logger.Lines.Should().ContainSingle(line => line.Level == LogLevel.Exception);
            all.Heard.Should().Equal(critical);
            fileSystemOnly.Heard.Should().BeEmpty();

            system.Report(new Failure(FailureKind.FileSystem, FailureLevel.Warning, "slow disk"));
            fileSystemOnly.Heard.Should().ContainSingle();
        }

        system.Report(new Failure(FailureKind.FileSystem, FailureLevel.Warning, "slow again"));
        fileSystemOnly.Heard.Should().ContainSingle();
        all.Heard.Should().HaveCount(3);
    }

    [Fact]
    public void AFailure_IsReportedOnce()
    {
        var system = new FailureSystem();
        var recorder = new Recorder();
        system.AddHandler(recorder);
        var failure = new Failure(FailureKind.Engine, FailureLevel.Warning, "once");

        system.Report(failure);
        system.Report(failure);

        recorder.Heard.Should().ContainSingle();
        failure.Reaction.Should().Be(FailureReaction.Log);
    }
}

public class CatcherTests
{
    [Fact]
    public void AFailureThatLetsTheWorkGoOn_GivesTheFallback()
    {
        using var catcher = new Catcher(FailureKind.FileSystem, FailureLevel.Warning);

        var save = catcher.Try<string>(() => throw new IOException("no save"), fallback: "empty");
        var next = catcher.Run(() => { });

        save.Should().Be("empty");
        next.Should().BeTrue();
        catcher.Failures.Should().ContainSingle().Which.Kind.Should().Be(FailureKind.FileSystem);
        catcher.Failures[0].Reaction.Should().Be(FailureReaction.Log);
    }

    [Fact]
    public void AFailureThatStopsTheWork_IsThrownOn_AndTheWholeTryStops()
    {
        var reached = false;

        var block = () =>
        {
            using var catcher = new Catcher(FailureKind.FileSystem);
            catcher.Run(() => throw new IOException("disk gone"));
            reached = true;
        };

        block.Should().Throw<FailureException>().WithInnerException<IOException>();
        reached.Should().BeFalse();
    }

    [Fact]
    public void CatchBlocks_HandleTheirExceptions_AndAreNotReported()
    {
        var firstRun = false;
        using var catcher = new Catcher(FailureKind.FileSystem)
            .Catch<FileNotFoundException>(_ => firstRun = true)
            .Catch<IOException>(FailureLevel.Warning);

        catcher.Run(() => throw new FileNotFoundException("no settings")).Should().BeFalse();
        catcher.Run(() => throw new IOException("busy")).Should().BeFalse();

        firstRun.Should().BeTrue();
        catcher.Failures.Should().ContainSingle().Which.Level.Should().Be(FailureLevel.Warning);
    }

    [Fact]
    public void ErrorsOfTheEngine_KeepTheirFailure()
    {
        using var catcher = new Catcher(FailureKind.Application, FailureLevel.Warning);

        var block = () => catcher.Run(() => throw new DomainNotFoundException("assets"));

        var thrown = block.Should().Throw<DomainNotFoundException>().Which;
        thrown.Failure.Kind.Should().Be(FailureKind.FileSystem);
        catcher.Failures.Should().ContainSingle().Which.Should().BeSameAs(thrown.Failure);
    }

    [Fact]
    public void AFailureCaughtAgain_KeepsItsFirstReaction()
    {
        var system = new FailureSystem();
        var recorder = new FailureSystemRecorder();
        system.AddHandler(recorder);

        using var outer = new Catcher(system, FailureKind.Engine, FailureLevel.Warning);

        // A stop goes up through every catcher, as if none were there, until a plain try; it is reported once
        var block = () => outer.Run(() =>
        {
            using var inner = new Catcher(system, FailureKind.Engine);
            inner.Run(() => throw new InvalidOperationException("deep"));
        });

        block.Should().Throw<FailureException>().WithInnerException<InvalidOperationException>();
        recorder.Count.Should().Be(1);
        outer.Failures.Should().ContainSingle().Which.Level.Should().Be(FailureLevel.Error);
    }

    [Fact]
    public void ACatcherThatKeepsAFew_KeepsTheLastOnes_AndCountsThemAll()
    {
        using var catcher = new Catcher(FailureKind.Engine, FailureLevel.Warning) { Keeps = 2 };
        using var forgetful = new Catcher(FailureKind.Engine, FailureLevel.Warning) { Keeps = 0 };

        for (var i = 0; i < 5; i++)
        {
            catcher.Report($"failure {i}");
            forgetful.Report($"failure {i}");
        }

        catcher.FailureCount.Should().Be(5);
        catcher.Failures.Select(failure => failure.Message).Should().Equal("failure 3", "failure 4");
        forgetful.HasFailed.Should().BeTrue();
        forgetful.Failures.Should().BeEmpty();
        var negative = () => new Catcher { Keeps = -1 };
        negative.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Guard_NeverThrows_AndGivesTheFailure()
    {
        using var catcher = new Catcher(FailureKind.Engine, FailureLevel.CriticalError);

        var failure = catcher.Guard(() => throw new InvalidOperationException("boom"), "the frame", phase: FramePhase.Update);

        failure.Should().NotBeNull();
        failure!.Message.Should().Be("the frame: boom");
        failure.Phase.Should().Be(FramePhase.Update);
        failure.Reaction!.Value.Should().HaveFlag(FailureReaction.StopEngine);
        catcher.Guard(() => { }).Should().BeNull();
    }

    [Fact]
    public async Task AsyncCalls_AreCaughtToo()
    {
        using var catcher = new Catcher(FailureKind.Engine, FailureLevel.Warning);

        (await catcher.RunAsync(async () =>
        {
            await Task.Yield();
            throw new InvalidOperationException("later");
        })).Should().BeFalse();

        (await catcher.TryAsync(() => ValueTask.FromResult(5), 0)).Should().Be(5);
        catcher.HasFailed.Should().BeTrue();
    }

    [Fact]
    public void Cancellation_IsNotAFailure()
    {
        using var catcher = new Catcher();

        catcher.Invoking(c => c.Run(() => throw new OperationCanceledException())).Should().Throw<OperationCanceledException>();
        catcher.HasFailed.Should().BeFalse();
    }

    [Fact]
    public void ThrowIfFailed_ThrowsTheWorst()
    {
        using var catcher = new Catcher(FailureKind.Engine, FailureLevel.Warning);
        catcher.Report("a placeholder was used");
        catcher.Report(new InvalidOperationException("worse"), level: FailureLevel.NonCritical);
        catcher.Report("the worst", FailureLevel.Warning);

        catcher.Invoking(c => c.ThrowIfFailed()).Should().Throw<FailureException>().WithMessage("a placeholder was used");
    }

    [Fact]
    public void InAnEngine_TheCatcherReportsToItsSystem()
    {
        var engine = new ScopeEngine("Failing");
        var system = new FailureSystem();
        var recorder = new FailureSystemRecorder();
        system.AddHandler(recorder);
        CoreEngine.For(engine).Services.Set<IFailureSystem>(system);

        using (EngineScope.Enter(engine))
        using (var catcher = new Catcher(FailureKind.Engine, FailureLevel.Warning))
            catcher.Run(() => throw new InvalidOperationException("in the engine"));

        recorder.Count.Should().Be(1);
        CoreEngine.Release(engine);
    }

    private sealed class FailureSystemRecorder : IFailureHandler
    {
        public int Count { get; private set; }

        public void Handle(Failure failure) => Count++;
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
    public void Run_PublishesTheFailureSystem_AndDisposeWithdrawsIt()
    {
        var engine = new ScopeEngine("Entry");
        var entry = new EntryPoint(new RuntimeConfiguration());
        var context = new RunContext(null, engine: engine);

        entry.Run(in context).Should().Be(0);
        CoreEngine.For(engine).Failures.Should().NotBeNull();

        var stopContext = new StopContext(null, StopReason.Regular);
        entry.RequestStop(in stopContext);
        entry.Dispose();

        CoreEngine.For(engine).Failures.Should().BeNull();
    }

    [Fact]
    public void EntryPoint_HasLabel()
    {
        using var entry = new EntryPoint(new RuntimeConfiguration());

        entry.Label.Name.Should().Be("S.C/MainEntry");
        entry.Meta.UID.Should().Be(entry.Label.UID);
        entry.UID.Should().Be(entry.Label.UID);
        entry.Metas.Should().ContainSingle();
    }
}
