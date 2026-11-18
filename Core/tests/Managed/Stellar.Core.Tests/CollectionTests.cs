using Stellar.Core.Data.Collections;
using Stellar.Core.Global;
using Stellar.Kernel;
using Stellar.Kernel.Data.Collections;
using Stellar.Kernel.Quantization;

namespace Stellar.Core.Tests;

public class ConcurrentListTests
{
    [Fact]
    public void AList_KeepsOrder_AndTellsEveryChange()
    {
        var list = new ConcurrentList<string>(["a", "c"]);
        var changes = new List<CollectionChange<string>>();
        list.Changed += changes.Add;

        list.Insert(1, "b");
        list.Add("d");
        list.Remove("a").Should().BeTrue();
        list[0] = "B";

        list.Should().Equal("B", "c", "d");
        changes.Select(c => (c.Kind, c.Index)).Should().Equal(
            (ChangeKind.Added, 1), (ChangeKind.Added, 3), (ChangeKind.Removed, 0), (ChangeKind.Replaced, 0));
        changes[^1].OldItem.Should().Be("b");
    }

    [Fact]
    public void Slices_CountFromTheEnd_AsInPython()
    {
        var list = new ConcurrentList<int>([0, 1, 2, 3, 4]);

        list.Slice(-2).Should().Equal(3, 4);
        list.Slice(1, -1).Should().Equal(1, 2, 3);
        list.Slice(4, 2).Should().BeEmpty();
        list.TryPop(out var last).Should().BeTrue();
        last.Should().Be(4);
    }

    [Fact]
    public void AnEnumeration_SeesTheListAsItWas()
    {
        var list = new ConcurrentList<int>([1, 2, 3]);
        var span = list.AsSpan();
        var seen = new List<int>();

        foreach (var item in list)
        {
            seen.Add(item);
            list.Add(item * 10);
        }

        seen.Should().Equal(1, 2, 3);
        span.ToArray().Should().Equal(1, 2, 3);
        list.Count.Should().Be(6);
    }

    [Fact]
    public void ConcurrentWriters_LoseNothing()
    {
        var list = new ConcurrentList<int>();

        Parallel.For(0, 1000, list.Add);

        list.Count.Should().Be(1000);
        list.ToArray().Should().BeEquivalentTo(Enumerable.Range(0, 1000));
    }
}

public class ConcurrentSetTests
{
    [Fact]
    public void ASet_HoldsEachItemOnce()
    {
        var set = new ConcurrentSet<string>(comparer: StringComparer.OrdinalIgnoreCase);
        var changes = new List<CollectionChange<string>>();
        set.Changed += changes.Add;

        set.Add("Player").Should().BeTrue();
        set.Add("PLAYER").Should().BeFalse();
        set.Remove("player").Should().BeTrue();

        set.Should().BeEmpty();
        changes.Select(c => c.Kind).Should().Equal(ChangeKind.Added, ChangeKind.Removed);
    }

    [Fact]
    public void SetOperations_MakeNewSets()
    {
        var set = new ConcurrentSet<int>([1, 2, 3]);

        set.Union([3, 4]).Should().BeEquivalentTo([1, 2, 3, 4]);
        set.Intersect([2, 3, 9]).Should().BeEquivalentTo([2, 3]);
        set.Except([1]).Should().BeEquivalentTo([2, 3]);
        set.Overlaps([9, 3]).Should().BeTrue();
        set.Should().BeEquivalentTo([1, 2, 3]);
    }

    [Fact]
    public void TheSnapshot_IsSharedUntilTheSetChanges()
    {
        var set = new ConcurrentSet<int>([1, 2]);

        var first = set.Snapshot;
        set.Snapshot.Should().BeSameAs(first);

        set.Add(3);
        set.Snapshot.Should().NotBeSameAs(first).And.HaveCount(3);
    }
}

public class ConcurrentMapTests
{
    [Fact]
    public void AMap_TellsWhatWasReplaced()
    {
        var map = new ConcurrentMap<string, int>();
        var changes = new List<MapChange<string, int>>();
        map.Changed += changes.Add;

        map.Set("hp", 10);
        map.Set("hp", 7);
        map.GetOrAdd("mp", _ => 5).Should().Be(5);
        map.GetOrAdd("mp", _ => 99).Should().Be(5);
        map.TryRemove("hp", out var removed).Should().BeTrue();

        removed.Should().Be(7);
        changes.Select(c => c.Kind).Should().Equal(ChangeKind.Added, ChangeKind.Replaced, ChangeKind.Added, ChangeKind.Removed);
        changes[1].OldValue.Should().Be(10);
        map.Invoking(m => m.Get("hp")).Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void ConcurrentWriters_LoseNothing()
    {
        var map = new ConcurrentMap<int, int>();

        Parallel.For(0, 2000, i =>
        {
            map.Set(i, i);
            if (i % 3 == 0) map.Remove(i);
        });

        map.Count.Should().Be(2000 - (2000 + 2) / 3);
        map.Snapshot.Should().HaveCount(map.Count);
    }

    [Fact]
    public void KeysAndValues_AreSharedUntilTheMapChanges()
    {
        var map = new ConcurrentMap<string, int>();
        map.Set("a", 1);
        map.Set("b", 2);

        var keys = map.Keys;
        var values = map.Values;
        map.Keys.Should().BeSameAs(keys);
        map.Values.Should().BeSameAs(values);
        keys.Should().BeEquivalentTo(["a", "b"]);

        map.Set("b", 3);
        map.Keys.Should().NotBeSameAs(keys);
        map.Values.Should().BeEquivalentTo([1, 3]);
        map.Select(pair => pair.Value).Should().BeEquivalentTo([1, 3]);
    }

    [Fact]
    public void AKeyThatCannotBeHeld_HasNoValue()
    {
        var plain = new ConcurrentMap<string, int>();
        plain.ContainsKey(null!).Should().BeFalse();

        var names = new LowerCaseMap();
        names.Set("Hero", 1);

        names.TryGet("HERO", out var value).Should().BeTrue();
        value.Should().Be(1);
        names.ContainsKey("").Should().BeFalse();
        names.Invoking(map => map.Set("", 2)).Should().Throw<ArgumentException>();
    }

    private sealed class LowerCaseMap
        : ConcurrentMap<string, int>
    {
        protected override string NormalizeKey(string key)
        {
            return string.IsNullOrEmpty(key) ? throw new ArgumentException("No name", nameof(key)) : key.ToLowerInvariant();
        }
    }
}

public class DataContainerTests
{
    private class Actor : IIdentifiableQuantumObject
    {
        public Identifier UID { get; } = Identifier.New();
    }

    private sealed class Player : Actor;

    [Fact]
    public void AContainer_IsRegistered_AndDisposeTakesItOut()
    {
        var table = new Table<Actor>();

        CoreEngine.Current.Registry.DataContainers.Get<Table<Actor>>(table.UID).Should().BeSameAs(table);

        table.Dispose();

        CoreEngine.Current.Registry.DataContainers.Exists(table.UID).Should().BeFalse();
    }

    [Fact]
    public void ATable_HoldsObjectsByIdentifier_AndFindsThemAsWhatTheyAre()
    {
        using var table = new Table<Actor>();
        var player = new Player();
        var actor = new Actor();

        table.Add(player).Should().BeTrue();
        table.Add(player).Should().BeFalse();
        table.Add(actor);

        table.Get(player.UID).Should().BeSameAs(player);
        table.Get<Player>(player.UID).Should().BeSameAs(player);
        table.Get<Player>(actor.UID).Should().BeNull();
        table.Remove(actor.UID).Should().BeSameAs(actor);
        table.Count.Should().Be(1);
        table.Invoking(t => t.Get(actor.UID)).Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void ASealedTable_NeverChanges()
    {
        var player = new Player();
        using var table = new Table<Actor>([player]);

        table.Seal();

        table.IsSealed.Should().BeTrue();
        table.Invoking(t => t.Add(new Actor())).Should().Throw<InvalidOperationException>();
        table.Invoking(t => t.Remove(player.UID)).Should().Throw<InvalidOperationException>();
        table.Invoking(t => t.Clear()).Should().Throw<InvalidOperationException>();
        table.Should().ContainSingle().Which.Should().BeSameAs(player);
    }

    [Fact]
    public void ARoster_FindsThroughItsBranches_AndNeverHoldsItself()
    {
        using var players = new Roster<Actor>();
        var team = players.NewBranch();
        var squad = team.NewBranch();
        var player = new Player();
        squad.Add(player);

        players.Find(player.UID).Should().BeSameAs(player);
        players.GetBranch(team.UID).Should().BeSameAs(team);
        players.TryGetBranch(squad.UID, out _).Should().BeFalse();

        squad.Invoking(s => s.AddBranch(players)).Should().Throw<ArgumentException>();
        players.Invoking(p => p.AddBranch(players)).Should().Throw<ArgumentException>();

        players.RemoveBranch(team.UID).Should().BeSameAs(team);
        players.Find(player.UID).Should().BeNull();

        team.Dispose();
        squad.Dispose();
    }

    [Fact]
    public void TheWayFromARosterToAPlayer_GoesThroughTheRegistry()
    {
        using var roster = new Roster<Actor>();
        var player = new Player();
        roster.Add(player);

        var found = Engine.Registry.DataContainers.Get<Roster<Actor>>(roster.UID)!.Get<Player>(player.UID);

        found.Should().BeSameAs(player);
    }

    [Fact]
    public void ADataTable_HoldsRowsByName()
    {
        using var units = new DataTable<UnitStats>([new("archer", new UnitStats(10, 3))]);

        units.Add("knight", new UnitStats(30, 1)).Should().BeTrue();
        units.Get("archer").Speed.Should().Be(3);
        units.RowType.Should().Be(typeof(UnitStats));
        units.Invoking(u => u.Add(" ", default)).Should().Throw<ArgumentException>();
        units.Keys.Should().BeEquivalentTo("archer", "knight");
        units.Should().BeAssignableTo<IDataContainer<string, UnitStats>>();
    }

    private readonly record struct UnitStats(int Health, int Speed);
}
