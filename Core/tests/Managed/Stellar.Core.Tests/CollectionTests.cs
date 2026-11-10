using Stellar.Core.Global;
using Stellar.Core.Data.Collections;
using Stellar.Core.Data.Registry;
using Stellar.Core.Quantization;
using Stellar.Kernel;
using Stellar.Kernel.Quantization;

namespace Stellar.Core.Tests;

public class FastConcurrentIdentifierMapTests
{
    [Fact]
    public void InitialData_IsVisibleInSnapshot()
    {
        // Regression: the snapshot stayed empty until the first refresh
        var key = Identifier.Create();
        var map = new FastConcurrentIdentifierMap<string>(
            [new KeyValuePair<IIdentifier, string>(key, "value")]);

        map.GetSnapshot().Should().Equal("value");
        map.Should().Equal("value");
    }

    [Fact]
    public void TryRemove_RemovesAndRefreshes()
    {
        // Regression: this used to throw LockRecursionException
        var map = new FastConcurrentIdentifierMap<string>();
        var key = Identifier.Create();
        map.TryAdd(key, "value");
        map.RefreshSnapshot();

        map.TryRemove(key, out var removed).Should().BeTrue();

        removed.Should().Be("value");
        map.GetSnapshot().Should().BeEmpty();
    }

    [Fact]
    public void Clear_EmptiesEverything()
    {
        // Regression: this used to throw LockRecursionException
        var map = new FastConcurrentIdentifierMap<string>();
        map.TryAdd(Identifier.Create(), "value", true);

        map.Clear();

        map.Count.Should().Be(0);
        map.GetSnapshot().Should().BeEmpty();

        var seen = new List<string>();
        map.ForEachReadOnly(seen.Add);
        seen.Should().BeEmpty();
    }

    [Fact]
    public void ImmediateAdd_IsVisibleBeforeRefresh()
    {
        var map = new FastConcurrentIdentifierMap<string>();

        map.TryAdd(Identifier.Create(), "hot", true);
        map.TryAdd(Identifier.Create(), "cold");

        var seen = new List<string>();
        map.ForEachReadOnly(seen.Add);

        seen.Should().Equal("hot");
    }

    [Fact]
    public void Refresh_MovesHotItemsToSnapshot_WithoutDuplicates()
    {
        var map = new FastConcurrentIdentifierMap<string>();
        map.TryAdd(Identifier.Create(), "hot", true);

        map.RefreshSnapshot();

        var seen = new List<string>();
        map.ForEachReadOnly(seen.Add);
        seen.Should().Equal("hot");
    }

    [Fact]
    public void RemovedHotItem_IsNotVisibleAnymore()
    {
        var map = new FastConcurrentIdentifierMap<string>();
        var key = Identifier.Create();
        map.TryAdd(key, "hot", true);

        map.TryRemove(key, out _);

        var seen = new List<string>();
        map.ForEachReadOnly(seen.Add);
        seen.Should().BeEmpty();
    }

    [Fact]
    public void Keys_AreComparedByValue()
    {
        var map = new FastConcurrentIdentifierMap<string>();
        var uid = Guid.NewGuid();

        map.TryAdd(new Identifier(uid), "value");

        map.ContainsKey(new Identifier(uid)).Should().BeTrue();
        map.TryAdd(new Identifier(uid), "other").Should().BeFalse();
    }

    [Fact]
    public void ConcurrentWritersAndReaders_DoNotInterfere()
    {
        var map = new FastConcurrentIdentifierMap<int>();

        Parallel.For(0, 2000, index =>
        {
            var key = Identifier.Create();

            map.TryAdd(key, index, index % 2 == 0);
            map.ForEachUntil(_ => false);

            if (index % 3 == 0)
                map.TryRemove(key, out _);

            if (index % 100 == 0)
                map.RefreshSnapshot();
        });

        map.Count.Should().Be(2000 - (2000 + 2) / 3);
    }
}

public class DataContainerTests
{
    private sealed class Item : IIdentifiableQuantumObject
    {
        public IIdentifier UID { get; } = Identifier.Create();
    }

    [Fact]
    public void Container_RegistersItself_AndDisposeRemovesIt()
    {
        var table = new WritableTable<Item>();

        CoreEngine.Current.Registry.DataContainers.Exists(table.UID).Should().BeTrue();
        DataContainer<Item>.GetContainer(table.UID).Should().BeSameAs(table);

        table.Dispose();

        CoreEngine.Current.Registry.DataContainers.Exists(table.UID).Should().BeFalse();
    }

    [Fact]
    public void WritableTable_StoresAndPopsItems()
    {
        var table = new WritableTable<Item>();
        var item = new Item();

        table.Set(item).Should().BeTrue();
        table.Set(item).Should().BeFalse();
        table.Contains(item).Should().BeTrue();
        table[item.UID].Should().BeSameAs(item);

        table.Pop(item.UID).Should().BeSameAs(item);
        table.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void WritableTable_NewItemIsVisibleInIteration()
    {
        var table = new WritableTable<Item>();
        var item = new Item();
        table.Set(item);

        var seen = new List<Item>();
        table.ForEachReadOnly(seen.Add);

        seen.Should().ContainSingle().Which.Should().BeSameAs(item);
    }

    [Fact]
    public void ConstantTable_RejectsChanges()
    {
        var item = new Item();
        var table = new ConstantTable<Item>(new MetaQuant(), new Dictionary<IIdentifier, Item> { [item.UID] = item });

        table.Set(new Item()).Should().BeFalse();
        table.Pop(item.UID).Should().BeNull();
        table.Count.Should().Be(1);
        table.Should().ContainSingle().Which.Should().BeSameAs(item);
    }

    [Fact]
    public void Indexer_ThrowsForMissingKey()
    {
        var table = new WritableTable<Item>();

        var action = () => table[Identifier.Create()];

        action.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void Roster_ManagesBranches()
    {
        var roster = new Roster<Item>();
        var branch = roster.NewBranch(new MetaQuant());

        roster.ContainsBranch(branch.UID).Should().BeTrue();
        roster.GetBranch(branch.UID).Should().BeSameAs(branch);

        roster.PopBranch(branch.UID).Should().BeSameAs(branch);
        roster.ContainsBranch(branch.UID).Should().BeFalse();

        var action = () => roster.GetBranch(branch.UID);
        action.Should().Throw<KeyNotFoundException>();
    }
}
