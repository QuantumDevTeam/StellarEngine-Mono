using Stellar.Kernel.Lifecycle;
using Stellar.Kernel.Profiling;

namespace Stellar.Core.Tests;

internal sealed class RecordingSession(ProfilerDetail detail = ProfilerDetail.Frames)
    : IProfilerSession
{
    private readonly List<ProfilerEvent> _events = [];

    public ProfilerDetail Detail { get; } = detail;

    public IReadOnlyList<ProfilerEvent> Events
    {
        get
        {
            lock (_events) return _events.ToArray();
        }
    }

    public void Write(in ProfilerEvent profilerEvent)
    {
        lock (_events) _events.Add(profilerEvent);
    }
}

public class ProfilerTests
{
    private static readonly ProfilerMarker Step = new("Tests.Physics.Step", ProfilerCategory.Physics);
    private static readonly ProfilerCounter Bodies = new("Tests.Physics.Bodies", ProfilerCategory.Physics);

    [Fact]
    public void AName_HasOneNumber()
    {
        new ProfilerMarker("Tests.Physics.Step").Id.Should().Be(Step.Id);
        Step.Id.Should().BePositive();
        Step.Name.Should().Be("Tests.Physics.Step");
        Step.Category.Should().Be(ProfilerCategory.Physics);
        ProfilerMarkers.All().Should().Contain((Step.Id, "Tests.Physics.Step", ProfilerCategory.Physics));
        default(ProfilerMarker).ToString().Should().Be("(no marker)");
    }

    [Fact]
    public void AnEngineNobodyRecords_MarksNothing()
    {
        var engine = new ScopeEngine("Quiet");

        using (EngineScope.Enter(engine))
        {
            Profiler.Current.Should().BeNull();
            using (Step.Auto())
            {
            }
        }
    }

    [Fact]
    public void ARecordedEngine_GetsItsSpans_InstantsAndCounters()
    {
        var engine = new ScopeEngine("Recorded");
        var other = new ScopeEngine("Not recorded");
        var session = new RecordingSession();

        using (Profiler.Attach(engine, session))
        {
            using (EngineScope.Enter(engine))
            {
                Profiler.Current.Should().BeSameAs(session);
                using (Step.Auto())
                    Step.Instant(7);
                Bodies.Set(42);
            }

            using (EngineScope.Enter(other))
            using (Step.Auto())
                Profiler.Current.Should().BeNull();

            var again = () => Profiler.Attach(engine, new RecordingSession());
            again.Should().Throw<InvalidOperationException>();
        }

        session.Events.Select(e => (e.Marker, e.Kind, e.Value)).Should().Equal(
            (Step.Id, ProfilerEventKind.Begin, 0L),
            (Step.Id, ProfilerEventKind.Instant, 7L),
            (Step.Id, ProfilerEventKind.End, 0L),
            (Bodies.Id, ProfilerEventKind.Counter, 42L));
        session.Events.Should().BeInAscendingOrder(e => e.Timestamp);

        using (EngineScope.Enter(engine))
            Profiler.Current.Should().BeNull();
        Profiler.SessionOf(engine).Should().BeNull();
    }

    [Fact]
    public void AnEvent_HasTheLayoutOfTheNativeOne()
    {
        System.Runtime.CompilerServices.Unsafe.SizeOf<ProfilerEvent>().Should().Be(24);
    }
}
