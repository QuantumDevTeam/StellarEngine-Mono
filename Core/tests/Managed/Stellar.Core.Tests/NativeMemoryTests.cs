using System.Runtime.InteropServices;
using Stellar.Core.Global;
using Stellar.Core.Memory;
using Stellar.Core.Native;
using Stellar.Kernel.Lifecycle;

namespace Stellar.Core.Tests;

public class NativeAllocatorTests
{
    [NativeFact]
    public void AnAllocator_CountsItsBlocks()
    {
        var allocator = new NativeAllocator();

        var block = allocator.Allocate(100, 16);
        (block % 16).Should().Be(0);
        allocator.Stats.LiveAllocations.Should().Be(1);
        allocator.Stats.AllocatedBytes.Should().Be(100);

        block = allocator.Reallocate(block, 100, 300, 16);
        allocator.Stats.AllocatedBytes.Should().Be(300);
        allocator.Stats.PeakBytes.Should().Be(300);

        allocator.Free(block, 300);
        allocator.Stats.LiveAllocations.Should().Be(0);
        allocator.Stats.TotalAllocations.Should().Be(1);
        allocator.Allocate(0, 16).Should().Be(0);
    }

    [NativeFact]
    public void AnAlignment_IsAPowerOfTwo()
    {
        var allocator = new NativeAllocator();

        allocator.Invoking(a => a.Allocate(8, 3)).Should().Throw<ArgumentException>();
    }

    [NativeFact]
    public void EveryEngine_HasAnAllocatorOfItsOwn()
    {
        var first = new ScopeEngine("Memory A");
        var second = new ScopeEngine("Memory B");

        var a = CoreEngine.For(first).Native.Allocator;
        var b = CoreEngine.For(second).Native.Allocator;

        a.Handle.Should().NotBe(b.Handle);
        CoreEngine.For(first).Native.Allocator.Should().BeSameAs(a);
        NativeAllocator.Default.Handle.Should().NotBe(a.Handle);

        CoreEngine.Release(first);
        CoreEngine.Release(second);
    }
}

public class NativeListTests
{
    private struct Particle
    {
        public float X;
        public float Life;
    }

    [NativeFact]
    public void AList_GrowsAndKeepsItsElements()
    {
        using var list = new NativeList<int>(allocator: new NativeAllocator());

        for (var i = 0; i < 100; i++) list.Add(i);
        list.Insert(0, -1);
        list.RemoveAt(1).Should().Be(0);
        list.RemoveAtSwapBack(0).Should().Be(-1);

        list.Count.Should().Be(99);
        list[0].Should().Be(99);
        list.Capacity.Should().BeGreaterThanOrEqualTo(100);
        list.TryPop(out var last).Should().BeTrue();
        last.Should().Be(98);
        list.AsSpan().ToArray().Should().HaveCount(98);
    }

    [NativeFact]
    public void Elements_AreChangedInPlace()
    {
        using var list = new NativeList<Particle>(allocator: new NativeAllocator());
        list.AddRange([new Particle { Life = 1 }, new Particle { Life = 2 }]);

        foreach (ref var particle in list) particle.Life -= 0.5f;
        list[0].X = 10;

        list.ToArray().Select(p => p.Life).Should().Equal(0.5f, 1.5f);
        list[0].X.Should().Be(10);
    }

    [NativeFact]
    public void AnEnumeration_ThatSeesTheListGrow_Throws()
    {
        using var list = new NativeList<int>(1, new NativeAllocator());
        list.Add(1);

        var grow = () =>
        {
            foreach (ref var item in list) list.Add(item);
        };

        grow.Should().Throw<InvalidOperationException>();
    }

    [NativeFact]
    public void Dispose_GivesTheMemoryBack()
    {
        var allocator = new NativeAllocator();
        var list = new NativeList<long>(64, allocator);
        allocator.Stats.LiveAllocations.Should().Be(2);

        list.Dispose();
        list.Dispose();

        allocator.Stats.LiveAllocations.Should().Be(0);
        list.IsDisposed.Should().BeTrue();
        list.Invoking(l => l.Add(1)).Should().Throw<ObjectDisposedException>();
    }

    [NativeFact]
    public unsafe void TheHeader_IsWhatNativeCodeReads()
    {
        using var list = new NativeList<double>(allocator: new NativeAllocator());
        list.AddRange([1.0, 2.0, 3.0]);

        // StellarNativeList: data, count, capacity, element size, lock, version, alignment, allocator
        var header = (byte*)list.Pointer;
        var data = *(double**)header;
        (*(int*)(header + 8)).Should().Be(3);
        (*(int*)(header + 16)).Should().Be(sizeof(double));
        (*(nint*)(header + 32)).Should().Be(list.Allocator.Handle);
        data[2].Should().Be(3.0);
    }

    [NativeFact]
    public void InAnEngine_TheListTakesTheMemoryOfTheEngine()
    {
        var engine = new ScopeEngine("Lists");
        var allocator = CoreEngine.For(engine).Native.Allocator;
        NativeList<int> list;

        using (EngineScope.Enter(engine))
            list = new NativeList<int>(8);

        list.Allocator.Should().BeSameAs(allocator);
        allocator.Stats.LiveAllocations.Should().Be(2);

        // The allocator outlives its engine while the list holds memory
        CoreEngine.Release(engine);
        list.Add(5);
        list.Dispose();
        allocator.Stats.LiveAllocations.Should().Be(0);
    }
}

public class ConcurrentNativeListTests
{
    [NativeFact]
    public void ManyWriters_LoseNothing()
    {
        using var list = new ConcurrentNativeList<int>(allocator: new NativeAllocator());

        Parallel.For(0, 8, worker =>
        {
            for (var i = 0; i < 10_000; i++) list.Add(worker * 10_000 + i);
        });

        list.Count.Should().Be(80_000);
        list.ToArray().Distinct().Should().HaveCount(80_000);
    }

    [NativeFact]
    public void Readers_SeeWholeChanges()
    {
        using var list = new ConcurrentNativeList<long>(allocator: new NativeAllocator());
        var torn = 0;

        // A writer always adds pairs; a reader must never see half of one
        var writer = Task.Run(() =>
        {
            for (var i = 0; i < 20_000; i++)
            {
                using var scope = list.Write();
                scope.Add(i);
                scope.Add(-i);
            }
        });

        var reader = Task.Run(() =>
        {
            while (!writer.IsCompleted)
            {
                using var scope = list.Read();
                if (scope.Count % 2 != 0) Interlocked.Increment(ref torn);
            }
        });

        Task.WaitAll(writer, reader);
        torn.Should().Be(0);
        list.Count.Should().Be(40_000);
    }

    [NativeFact]
    public void AWriteScope_IsTheListAsOneOwnerHasIt()
    {
        using var list = new ConcurrentNativeList<int>(allocator: new NativeAllocator());
        list.AddRange([1, 2, 3]);

        using (var scope = list.Write())
        {
            scope.Items[0] = 10;
            scope.RemoveAtSwapBack(1).Should().Be(2);
            scope.Insert(0, 0);
        }

        list.ToArray().Should().Equal(0, 10, 3);
        list.TryGet(1, out var second).Should().BeTrue();
        second.Should().Be(10);
        list.TrySet(5, 1).Should().BeFalse();
        list.TryPop(out var last).Should().BeTrue();
        last.Should().Be(3);
    }

    [NativeFact]
    public void Dispose_WaitsForTheReaders()
    {
        var allocator = new NativeAllocator();
        var list = new ConcurrentNativeList<int>(4, allocator);
        list.Add(1);

        var scope = list.Read();
        var disposing = Task.Run(list.Dispose);
        disposing.Wait(100).Should().BeFalse();

        scope.Dispose();
        disposing.Wait(5000).Should().BeTrue();
        allocator.Stats.LiveAllocations.Should().Be(0);
    }

    [Fact]
    public void TheHeader_HasTheLayoutOfTheNativeOne()
    {
        // StellarNativeList is 40 bytes on 64-bit
        Marshal.SizeOf(typeof(NativeList<int>).Assembly.GetType("Stellar.Core.Memory.NativeListHeader")!).Should()
            .Be(40);
    }
}
