// Tests of the native core and of its P/Invoke exports. The order of includes is the one a consumer uses.
#include "include/Kernel/kernel.h"
#include "include/Core/core.h"
#include "include/Core/core_contracts.h"
#include "include/PInvoke/exports.h"
#include "include/Kernel/memory.h"

#include <atomic>
#include <cstdio>
#include <cstring>
#include <filesystem>
#include <memory>
#include <string>
#include <thread>
#include <vector>

using namespace Stellar::Native::Core;
using namespace Stellar::Native::Core::Data::Collections;
using namespace Stellar::Native::Core::Data::Registry;
using namespace Stellar::Native::Core::Modules;
using Stellar::Native::Core::Memory::Allocator;
using Stellar::Native::Kernel::Memory::NativeList;
using Stellar::Native::Kernel::Memory::NativeListLock;
using Stellar::Native::Kernel::Memory::NativeListReadScope;
using Stellar::Native::Kernel::Memory::NativeListView;
using Stellar::Native::Kernel::Memory::NativeListWriteScope;

namespace
{
    int checks = 0;
    int failures = 0;

    void Check(bool condition, const char* expression, int line)
    {
        ++checks;

        if (!condition)
        {
            ++failures;
            std::printf("FAILED line %d: %s\n", line, expression);
        }
    }

#define CHECK(expression) Check((expression), #expression, __LINE__)

    void TestIdentifier()
    {
        static_assert(sizeof(Identifier) == 16);

        // The same value and the same memory layout as System.Guid
        const auto parsed = Identifier::Parse("00112233-4455-6677-8899-aabbccddeeff");
        CHECK(parsed.has_value());

        constexpr Identifier::Bytes expected = {
            0x33,
            0x22,
            0x11,
            0x00,
            0x55,
            0x44,
            0x77,
            0x66,
            0x88,
            0x99,
            0xAA,
            0xBB,
            0xCC,
            0xDD,
            0xEE,
            0xFF
        };
        CHECK(parsed->GetData() == expected);
        CHECK(parsed->ToString() == "00112233-4455-6677-8899-aabbccddeeff");

        CHECK(Identifier::Parse("00112233-4455-6677-8899-AABBCCDDEEFF") == parsed);
        CHECK(!Identifier::Parse("not a guid").has_value());
        CHECK(!Identifier::Parse("00112233x4455-6677-8899-aabbccddeeff").has_value());
        CHECK(!Identifier::Parse("0011223z-4455-6677-8899-aabbccddeeff").has_value());

        const auto first = Identifier::Create();
        const auto second = Identifier::Create();
        CHECK(first != second);
        CHECK(!first.IsNull());
        CHECK(Identifier().IsNull());
        CHECK(Identifier::Parse(first.ToString()) == first);
        CHECK(std::hash<Identifier>{}(first) == first.GetHashCode());
        CHECK(Identifier(first.ToAbi()) == first);
    }

    void TestConcurrentMap()
    {
        ConcurrentUnorderedMap<int, int> map;

        CHECK(map.TryAdd(1, 10));
        CHECK(!map.TryAdd(1, 20));
        CHECK(map.TryGet(1) == 10);
        CHECK(!map.TryGet(2).has_value());

        int removed = 0;
        CHECK(map.TryRemove(1, removed));
        CHECK(removed == 10);
        CHECK(!map.TryRemove(1, removed));

        std::vector<std::jthread> threads;
        for (int thread = 0; thread < 8; ++thread)
            threads.emplace_back([&map, thread]
            {
                for (int i = 0; i < 1000; ++i)
                    map.TryAdd(thread * 1000 + i, i);
            });
        threads.clear();

        CHECK(map.size() == 8000);

        std::size_t keys = 0;
        for ([[maybe_unused]] const int& key : map.Keys())
            ++keys;
        CHECK(keys == 8000);

        std::size_t even = 0;
        for ([[maybe_unused]] int value : map.ForEachReadOnly<int>(
                 [](const int& key, const int&) -> std::optional<int>
                 {
                     return key % 2 == 0 ? std::optional(key) : std::nullopt;
                 }))
            ++even;
        CHECK(even == 4000);

        std::size_t taken = 0;
        for ([[maybe_unused]] const auto& pair : map.ForEachPredicateRemove(
                 [](const int& key, const int&) { return key < 1000; }))
            ++taken;
        CHECK(taken == 1000);
        CHECK(map.size() == 7000);

        map.Clear();
        CHECK(map.size() == 0);
    }

    void TestPriorityQueue()
    {
        ConcurrentPriorityQueue<int> queue;

        CHECK(!queue.TryPop().has_value());

        for (const int value : {3, 1, 5, 2, 4})
            queue.Push(value);

        CHECK(queue.size() == 5);
        for (const int expected : {5, 4, 3, 2, 1})
            CHECK(queue.TryPop() == expected);

        std::stop_source stop;
        std::atomic<bool> finished = false;
        std::optional<int> received = 1;

        std::jthread waiter([&]
        {
            received = queue.PopWait(stop.get_token());
            finished = true;
        });

        stop.request_stop();
        waiter.join();
        CHECK(finished);
        CHECK(!received.has_value());

        std::jthread consumer([&]
        {
            std::stop_source never;
            received = queue.PopWait(never.get_token());
        });

        queue.Push(42);
        consumer.join();
        CHECK(received == 42);
    }

    void TestConcurrentVector()
    {
        ConcurrentVector<int> vector;

        for (const int value : {3, 1, 2})
            vector.PushBack(value);

        vector.Sort();

        int value = 0;
        CHECK(vector.TryGet(0, value) && value == 1);
        CHECK(vector.TryRemove(1, value) && value == 2);
        CHECK(vector.size() == 2);
        CHECK(!vector.TryGet(5, value));

        vector.Clear();
        CHECK(vector.size() == 0);
    }

    void TestDoubleBufferedMap()
    {
        DoubleBufferedSnapshotMap<int, int> map;

        CHECK(map.TryAdd(1, 10));
        CHECK(!map.TryAdd(1, 11));
        CHECK(!map.Contains(1));

        const auto first = map.TakeSnapshot();
        CHECK(map.TryGet(1) == 10);
        CHECK(first->version == 1);

        // Regression: a snapshot used to move the data out, so the next one lost everything before it
        CHECK(map.TryAdd(2, 20));
        const auto second = map.TakeSnapshot();
        CHECK(second->map.size() == 2);
        CHECK(second->version == 2);
        CHECK(first->map.size() == 1);

        CHECK(map.TryAdd(3, 30, true));
        CHECK(!map.Contains(3));
        CHECK(map.Contains(3, true));
        CHECK(map.size(false) == 2);
        CHECK(map.size(true) == 3);

        int removed = 0;
        CHECK(map.TryRemove(1, removed, true));
        CHECK(map.Contains(1));
        CHECK(!map.Contains(1, true));
        CHECK(map.size(true) == 2);

        std::size_t keys = 0;
        for ([[maybe_unused]] const int& key : map.Keys(true))
            ++keys;
        CHECK(keys == 2);

        map.TakeSnapshot();
        CHECK(!map.Contains(1));
        CHECK(map.Contains(3));

        // Regression: Clear used to take the same lock twice and hang
        map.Clear();
        CHECK(map.size() == 0);
    }

    void TestDataContainers()
    {
        DataContainer<int> writable;
        const auto id = Identifier::Create();

        CHECK(!writable.GetUID().IsNull());
        CHECK(writable.TryAdd(id, 1));
        CHECK(writable.Contains(id));

        ConstantDataContainer<int> constant{{id, 7}};
        int removed = 0;

        CHECK(constant.TryGet(id) == 7);
        CHECK(!constant.TryAdd(Identifier::Create(), 1));
        CHECK(!constant.TryRemove(id, removed));
    }

    void TestHandlePool()
    {
        HandlePool<std::string> pool;

        const auto first = pool.Create("first");
        CHECK(first.generation != 0);
        CHECK(pool.IsValid(first));
        CHECK(pool.size() == 1);

        std::string seen;
        CHECK(pool.With(first, [&](const std::string& value) { seen = value; }));
        CHECK(seen == "first");

        CHECK(pool.Destroy(first));
        CHECK(!pool.IsValid(first));
        CHECK(!pool.Destroy(first));

        // The slot is reused, but the old handle must not reach the new object
        const auto second = pool.Create("second");
        CHECK(second.index == first.index);
        CHECK(second.generation != first.generation);
        CHECK(!pool.IsValid(first));
        CHECK(!pool.With(first, [](const std::string&) {}));

        CHECK(pool.Take(second) == "second");
        CHECK(pool.size() == 0);

        CHECK(!pool.IsValid(StellarHandle{}));
        CHECK(!pool.IsValid(StellarHandle{1000, 1}));
    }

    void TestExports(const std::string& modulePath)
    {
        Ptr<StellarCoreContext> context;
        CHECK(stellar_core_context_create(&context) == STELLAR_RESULT_OK && context);
        CHECK(stellar_core_context_create(nullptr) == STELLAR_RESULT_INVALID_ARGUMENT);
        CHECK(stellar_core_context_destroy(nullptr) == STELLAR_RESULT_INVALID_ARGUMENT);

        StellarContractVersion version{};
        CHECK(stellar_core_get_abi_version(&version) == STELLAR_RESULT_OK);
        CHECK(version.major == STELLAR_PINVOKE_ABI_MAJOR);

        StellarUid uid{};
        CHECK(stellar_identifier_create(&uid) == STELLAR_RESULT_OK);
        CHECK(stellar_identifier_create(nullptr) == STELLAR_RESULT_INVALID_ARGUMENT);

        char buffer[64];
        size_t required = 0;

        CHECK(stellar_identifier_format(&uid, &buffer[0], 4, &required) == STELLAR_RESULT_BUFFER_TOO_SMALL);
        CHECK(required == 36);
        CHECK(stellar_identifier_format(&uid, &buffer[0], sizeof(buffer), &required) == STELLAR_RESULT_OK);
        CHECK(std::strlen(buffer) == 36);

        // Yes and no are answers, not failures: they come back through an out parameter
        int32_t answer = -1;
        StellarUid parsed{};
        CHECK(stellar_identifier_parse({buffer, required}, &parsed, &answer) == STELLAR_RESULT_OK && answer == 1);
        CHECK(std::memcmp(&uid, &parsed, sizeof(uid)) == 0);
        CHECK(stellar_identifier_parse({"bad", 3}, &parsed, &answer) == STELLAR_RESULT_OK && answer == 0);

        size_t count = 0;

        // Errors are data: a failure must come back as a code and a message
        StellarHandle module{};
        CHECK(stellar_module_load(context, {"no-such-module.dll", 18}, &module) == STELLAR_RESULT_NOT_FOUND);
        CHECK(stellar_core_get_last_error(&buffer[0], sizeof(buffer), &required) == STELLAR_RESULT_OK
            || required >= sizeof(buffer));
        CHECK(required != 0);

        CHECK(stellar_module_load(context, {modulePath.data(), modulePath.size()}, &module) == STELLAR_RESULT_OK);
        CHECK(stellar_module_get_name(context, module, &buffer[0], sizeof(buffer), &required) == STELLAR_RESULT_OK);
        CHECK(std::string(buffer) == "Stellar.Native.TestModule");

        CHECK(stellar_module_count(context, &count) == STELLAR_RESULT_OK && count == 1);
        CHECK(stellar_module_unload(context, module, &answer) == STELLAR_RESULT_OK && answer == 1);
        CHECK(stellar_module_unload(context, module, &answer) == STELLAR_RESULT_OK && answer == 0);
        CHECK(stellar_module_get_name(context, module, &buffer[0], sizeof(buffer), &required) == STELLAR_RESULT_INVALID_HANDLE);
        CHECK(stellar_module_count(context, &count) == STELLAR_RESULT_OK && count == 0);

        // The default context never sees what another context holds
        CHECK(stellar_module_load(context, {modulePath.data(), modulePath.size()}, &module) == STELLAR_RESULT_OK);
        size_t inDefault = 1;
        CHECK(stellar_module_count(nullptr, &inDefault) == STELLAR_RESULT_OK && inDefault == 0);
        CHECK(stellar_core_context_destroy(context) == STELLAR_RESULT_OK);
    }

    StellarResult STELLAR_CALL FailingInitialize()
    {
        return STELLAR_RESULT_UNKNOWN;
    }

    void TestContexts()
    {
        CoreContext editor;
        CoreContext game;

        // Two engines know nothing of each other: a module of one is not in the other
        constexpr char name[] = "Context";
        const StellarModuleInfo info = {
            sizeof(StellarModuleInfo),
            {STELLAR_MODULE_CONTRACT_MAJOR, STELLAR_MODULE_CONTRACT_MINOR},
            {name, 7},
            {name, 7},
            nullptr,
            nullptr
        };

        (void)editor.Modules().Add(info);
        CHECK(editor.Modules().size() == 1);
        CHECK(game.Modules().size() == 0);

        CHECK(&CoreContext::Default() == &CoreContext::Default());
        CHECK(&editor.Modules() != &game.Modules());

        // A context that goes takes its registries with it
        {
            CoreContext temporary;
            (void)temporary.Modules().Add(info);
        }
        CHECK(editor.Modules().size() == 1);
    }

    void TestModuleContract()
    {
        CoreContext context;
        auto& registry = context.Modules();

        constexpr char name[] = "Test";
        StellarModuleInfo info = {
            sizeof(StellarModuleInfo),
            {STELLAR_MODULE_CONTRACT_MAJOR, STELLAR_MODULE_CONTRACT_MINOR},
            {name, 4},
            {name, 4},
            nullptr,
            nullptr
        };

        const auto handle = registry.Add(info);
        CHECK(registry.GetName(handle) == "Test");
        CHECK(registry.Unload(handle));

        const auto rejected = [&registry](const StellarModuleInfo& candidate, StellarResult expected)
        {
            try
            {
                (void)registry.Add(candidate);
                return false;
            }
            catch (const Failures::NativeException& exception)
            {
                return exception.GetResult() == expected;
            }
        };

        auto newerMajor = info;
        newerMajor.contract.major += 1;
        CHECK(rejected(newerMajor, STELLAR_RESULT_VERSION_MISMATCH));

        auto newerMinor = info;
        newerMinor.contract.minor += 1;
        CHECK(rejected(newerMinor, STELLAR_RESULT_VERSION_MISMATCH));

        auto truncated = info;
        truncated.structSize = 4;
        CHECK(rejected(truncated, STELLAR_RESULT_VERSION_MISMATCH));

        auto failing = info;
        failing.initialize = FailingInitialize;
        CHECK(rejected(failing, STELLAR_RESULT_UNKNOWN));

        CHECK(registry.size() == 0);
    }
}

namespace
{
    void TestAllocator()
    {
        const auto allocator = Allocator::Create();
        CHECK(allocator && allocator->References() == 1);

        auto block = allocator->Allocate(100, 64);
        CHECK(block && reinterpret_cast<uintptr_t>(block.get()) % 64 == 0);
        CHECK(allocator->References() == 2);
        CHECK(allocator->GetStats().allocatedBytes == 100 && allocator->GetStats().liveAllocations == 1);

        block = allocator->Reallocate(block, 100, 1000, 64);
        CHECK(block && allocator->GetStats().allocatedBytes == 1000 && allocator->GetStats().peakBytes == 1000);

        CHECK(!allocator->Allocate(0, 16));
        CHECK(!allocator->Allocate(16, 3));

        // A block keeps its allocator alive after its maker lets it go
        allocator->Release();
        CHECK(allocator->References() == 1);
        allocator->Free(block, 1000);

        // The same through the table of the ABI
        Link<const StellarMemoryApi> api = Allocator::Api();
        CHECK(api.structSize == sizeof(StellarMemoryApi));
        CHECK(api.contract.major == STELLAR_MEMORY_CONTRACT_MAJOR);

        StellarAllocator* abi = api.create();
        void* memory = api.allocate(abi, 32, 16);
        StellarMemoryStats stats{};
        api.getStats(abi, &stats);
        CHECK(memory && stats.liveAllocations == 1 && stats.totalAllocations == 1);
        api.free(abi, memory, 32);
        api.getStats(abi, &stats);
        CHECK(stats.liveAllocations == 0 && stats.allocatedBytes == 0);
        api.release(abi);

        // A context has its allocator, apart from the others
        CoreContext first;
        CoreContext second;
        CHECK(&first.Allocator() != &second.Allocator());
        CHECK(&first.Allocator() != &CoreContext::Default().Allocator());
    }

    struct Particle
    {
        float x;
        float life;
    };

    void TestNativeList()
    {
        const auto allocator = Allocator::Create();
        {
            NativeList<Particle> list(allocator->ToAbi(), Allocator::Api());
            auto view = list.View();
            CHECK(NativeListView<Particle>::IsOf(list.Header()));
            CHECK(!NativeListView<int32_t>::IsOf(list.Header()));

            for (int i = 0; i < 100; ++i)
                CHECK(view.Add(Particle{static_cast<float>(i), 1.0f}));

            CHECK(view.Count() == 100 && view.Capacity() >= 100);
            CHECK(reinterpret_cast<uintptr_t>(list.Header().data) % 16 == 0);

            view.RemoveAtSwapBack(0);
            CHECK(view.Count() == 99 && view.Items()[0].x == 99.0f);

            for (auto& particle : view.Items())
                particle.life -= 0.5f;
            CHECK(view.Items()[10].life == 0.5f);

            view.Clear();
            CHECK(view.Count() == 0);
            CHECK(allocator->GetStats().liveAllocations == 1);
        }

        CHECK(allocator->GetStats().liveAllocations == 0);
        CHECK(allocator->References() == 1);
        allocator->Release();
    }

    void TestNativeListLock()
    {
        const auto allocator = Allocator::Create();
        NativeList<int64_t> list(allocator->ToAbi(), Allocator::Api());
        std::atomic<int> torn{0};
        std::atomic<bool> done{false};

        // Pairs go in under the write lock; a reader must never see half of one
        std::thread reader([&]
        {
            while (!done.load())
            {
                NativeListReadScope scope(list.Header());
                if (list.Header().count % 2 != 0)
                    ++torn;
            }
        });

        std::vector<std::thread> writers;
        for (int w = 0; w < 4; ++w)
            writers.emplace_back([&]
            {
                for (int i = 0; i < 5000; ++i)
                {
                    NativeListWriteScope scope(list.Header());
                    auto view = list.View();
                    (void)view.Add(i);
                    (void)view.Add(-i);
                }
            });

        for (auto& writer : writers)
            writer.join();
        done = true;
        reader.join();

        CHECK(torn.load() == 0);
        CHECK(list.Header().count == 40000);
        CHECK(list.Header().lock == 0);
        allocator->Release();
    }

    void TestMemoryExports()
    {
        Ptr<const StellarMemoryApi> api;
        CHECK(stellar_memory_get_api(&api) == STELLAR_RESULT_OK && api.get() == &Allocator::Api());
        CHECK(stellar_memory_get_api(nullptr) == STELLAR_RESULT_INVALID_ARGUMENT);

        Ptr<StellarCoreContext> context;
        CHECK(stellar_core_context_create(&context) == STELLAR_RESULT_OK);

        Ptr<StellarAllocator> allocator;
        CHECK(stellar_core_context_get_allocator(context, &allocator) == STELLAR_RESULT_OK && allocator);

        Ptr<StellarAllocator> fallback;
        CHECK(stellar_core_context_get_allocator(nullptr, &fallback) == STELLAR_RESULT_OK);
        CHECK(fallback.get() == CoreContext::Default().Allocator().ToAbi().get());

        // A block outlives the context it came from: the allocator waits for it
        void* block = api->allocate(allocator.get(), 64, 16);
        CHECK(stellar_core_context_destroy(context) == STELLAR_RESULT_OK);
        StellarMemoryStats stats{};
        api->getStats(allocator.get(), &stats);
        CHECK(stats.liveAllocations == 1);
        api->free(allocator.get(), block, 64);
    }
}

int main(int, char* argv[])
{
    const auto directory = std::filesystem::absolute(argv[0]).parent_path();

    TestIdentifier();
    TestConcurrentMap();
    TestPriorityQueue();
    TestConcurrentVector();
    TestDoubleBufferedMap();
    TestDataContainers();
    TestHandlePool();
    TestExports((directory / "Stellar.Native.TestModule.dll").string());
    TestModuleContract();
    TestContexts();
    TestAllocator();
    TestNativeList();
    TestNativeListLock();
    TestMemoryExports();

    std::printf("%d checks, %d failed\n", checks, failures);
    return failures == 0 ? 0 : 1;
}
