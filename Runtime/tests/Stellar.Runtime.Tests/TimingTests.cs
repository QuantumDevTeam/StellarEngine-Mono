using Stellar.Kernel.Timing;
using Stellar.Runtime.Timing;

namespace Stellar.Runtime.Tests;

public class TimingTests
{
    private readonly Source _source = new();
    private readonly TimeSystem _time;

    public TimingTests()
    {
        _time = new TimeSystem(_source);
    }

    [Fact]
    public void AClock_RunsAtItsScale_TimesItsParents()
    {
        var game = _time.CreateClock("Game", scale: 2);
        var slowMotion = _time.CreateClock("SlowMotion", game, 0.25);

        _source.Now = 4;

        _time.System.Time.Should().Be(4);
        _time.Engine.Time.Should().Be(4);
        game.Time.Should().Be(8);
        slowMotion.Time.Should().Be(2);
        slowMotion.Rate.Should().Be(0.5);
    }

    [Fact]
    public void Pausing_StopsTheClockAndEveryClockBelow_WithoutAJump()
    {
        var game = _time.CreateClock("Game");
        var world = _time.CreateClock("World", game, 2);

        _source.Now = 1;
        game.Pause();
        _source.Now = 10;

        game.Time.Should().Be(1);
        world.Time.Should().Be(2);
        world.IsPaused.Should().BeFalse();
        world.IsRunning.Should().BeFalse();
        _time.Engine.Time.Should().Be(10, "the clocks above go on");

        game.Resume();
        _source.Now = 11;

        game.Time.Should().Be(2);
        world.Time.Should().Be(4);
    }

    [Fact]
    public void ChangingTheScale_KeepsTheTimeReached()
    {
        var game = _time.CreateClock("Game");

        _source.Now = 2;
        game.Scale = 3;
        _source.Now = 3;

        game.Time.Should().Be(5);
    }

    [Fact]
    public void TheSystemClock_IsRealTime()
    {
        ((Action)(() => _time.System.Pause())).Should().Throw<InvalidOperationException>();
        ((Action)(() => _time.System.Scale = 2)).Should().Throw<InvalidOperationException>();
        _time.Engine.Parent.Should().BeSameAs(_time.System);
    }

    [Fact]
    public void TheFixedStep_IsTheNearestAbove()
    {
        var time = new TimeSystem(_source, 0.02);
        var game = time.CreateClock("Game");
        var physics = time.CreateClock("Physics", game);

        physics.EffectiveFixedStep.Should().Be(0.02);

        game.FixedStep = 0.01;
        physics.EffectiveFixedStep.Should().Be(0.01);
    }

    [Fact]
    public void Clocks_AreNamed_AndRemovedWithWhatIsBelow()
    {
        var game = _time.CreateClock("Game");
        _time.CreateClock("World", game);

        ((Action)(() => _time.CreateClock("Game"))).Should().Throw<ArgumentException>();
        ((Action)(() => _time.CreateClock("Other", new TimeSystem().Engine))).Should().Throw<ArgumentException>();

        _time.Remove(game).Should().BeTrue();
        _time.Find("World").Should().BeNull();
        _time.Remove(_time.Engine).Should().BeFalse();
        _time.Clocks.Select(clock => clock.Name).Should()
            .BeEquivalentTo(ITimeSystem.SystemClockName, ITimeSystem.EngineClockName);
    }

    [Fact]
    public void TwoTimeSystems_AreApart()
    {
        var editor = new TimeSystem(_source);

        _time.Engine.Pause();
        _source.Now = 5;

        editor.Engine.Time.Should().Be(5);
        _time.Engine.Time.Should().Be(0);
    }

    private sealed class Source : ITimeSource
    {
        public double Now { get; set; }
    }
}
