// Tests of the native core and of its P/Invoke exports. The order of includes is the one a consumer uses.
#include "include/Kernel/kernel.h"
#include "include/Core/core.h"
#include "include/Core/core_contracts.h"
#include "include/PInvoke/exports.h"

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

    void TestIdentifierRegistry()
    {
        CoreContext context;
        auto& registry = context.Identifiers();
        const auto id = Identifier::Create();

        CHECK(registry.Register(id));
        CHECK(!registry.Register(id));
        CHECK(registry.Contains(id));
        CHECK(registry.Unregister(id));
        CHECK(!registry.Unregister(id));
        CHECK(!registry.Contains(id));
    }

    void TestLabelRegistry()
    {
        CoreContext context;
        auto& registry = context.Labels();
        const auto id = Identifier::Create();

        // The registry must keep its own copy: this text dies before the label is read
        auto text = std::make_unique<std::string>("S.N.C/Test");
        const auto label = registry.Register(*text, id);
        CHECK(label.has_value());
        text.reset();

        CHECK(label->GetName() == "S.N.C/Test");
        CHECK(registry.Get("S.N.C/Test")->GetUID() == id);
        CHECK(registry.Get(id)->GetName() == "S.N.C/Test");
        CHECK(!registry.Register("S.N.C/Test", Identifier::Create()).has_value());
        CHECK(!registry.Register("S.N.C/Other", id).has_value());
        CHECK(!registry.Register("", Identifier::Create()).has_value());

        CHECK(registry.Unregister(id));
        CHECK(!registry.Get("S.N.C/Test").has_value());
        CHECK(!registry.Contains(id));

        const auto byName = Identifier::Create();
        CHECK(registry.Register("S.N.C/ByName", byName).has_value());
        CHECK(registry.Unregister("S.N.C/ByName"));
        CHECK(!registry.Contains(byName));
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

    void TestContainerRegistry()
    {
        CoreContext context;
        auto& registry = context.Containers();
        const auto container = Identifier::Create();
        const auto branch = Identifier::Create();
        const auto item = Identifier::Create();

        CHECK(registry.Create(container));
        CHECK(!registry.Create(container));
        CHECK(registry.Exists(container));

        CHECK(registry.Add(container, item));
        CHECK(!registry.Add(container, item));
        CHECK(registry.Contains(container, item));
        CHECK(registry.Count(container) == 1);
        CHECK(registry.Items(container) == std::vector{item});

        // A branch is a container too, and a container is not its own branch
        CHECK(registry.Create(branch));
        CHECK(registry.AddBranch(container, branch));
        CHECK(!registry.AddBranch(container, branch));
        CHECK(!registry.AddBranch(container, container));
        CHECK(registry.ContainsBranch(container, branch));
        CHECK(registry.Branches(container) == std::vector{branch});
        CHECK(registry.RemoveBranch(container, branch));

        // Sealed: the contents are fixed
        registry.Seal(container);
        CHECK(registry.IsSealed(container));
        CHECK(!registry.Add(container, Identifier::Create()));
        CHECK(!registry.Remove(container, item));
        CHECK(!registry.Clear(container));
        CHECK(registry.Count(container) == 1);

        CHECK(registry.Destroy(container));
        CHECK(registry.Destroy(branch));
        CHECK(!registry.Destroy(container));
        CHECK(!registry.Exists(container));

        // An unknown container is a failure, not an empty one
        bool notFound = false;
        try
        {
            (void)registry.Count(container);
        }
        catch (const Failures::NativeException& exception)
        {
            notFound = exception.GetResult() == STELLAR_RESULT_NOT_FOUND;
        }
        CHECK(notFound);
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

        CHECK(stellar_identifier_register(context, &uid, &answer) == STELLAR_RESULT_OK && answer == 1);
        CHECK(stellar_identifier_register(context, &uid, &answer) == STELLAR_RESULT_OK && answer == 0);
        CHECK(stellar_identifier_contains(context, &uid, &answer) == STELLAR_RESULT_OK && answer == 1);
        CHECK(stellar_identifier_unregister(context, &uid, &answer) == STELLAR_RESULT_OK && answer == 1);
        CHECK(stellar_identifier_unregister(context, &uid, &answer) == STELLAR_RESULT_OK && answer == 0);

        const StellarStringView name = {"S.N.C/Abi", 9};
        StellarUid found{};
        CHECK(stellar_label_register(context, name, &uid, &answer) == STELLAR_RESULT_OK && answer == 1);
        CHECK(stellar_label_find(context, name, &found, &answer) == STELLAR_RESULT_OK && answer == 1);
        CHECK(std::memcmp(&uid, &found, sizeof(uid)) == 0);
        CHECK(stellar_label_get_name(context, &uid, &buffer[0], sizeof(buffer), &required) == STELLAR_RESULT_OK);
        CHECK(std::string(buffer) == "S.N.C/Abi");
        CHECK(stellar_label_unregister(context, &uid, &answer) == STELLAR_RESULT_OK && answer == 1);
        CHECK(stellar_label_find(context, name, &found, &answer) == STELLAR_RESULT_OK && answer == 0);

        // Containers through the exports, and a failure: an unknown container
        StellarUid container{};
        size_t count = 0;
        CHECK(stellar_identifier_create(&container) == STELLAR_RESULT_OK);
        CHECK(stellar_container_create(context, &container, &answer) == STELLAR_RESULT_OK && answer == 1);
        CHECK(stellar_container_add(context, &container, &uid, &answer) == STELLAR_RESULT_OK && answer == 1);
        CHECK(stellar_container_count(context, &container, &count) == STELLAR_RESULT_OK && count == 1);

        StellarUid items[1]{};
        CHECK(stellar_container_items(context, &container, &items[0], 1, &required) == STELLAR_RESULT_OK && required == 1);
        CHECK(std::memcmp(&items[0], &uid, sizeof(uid)) == 0);
        CHECK(stellar_container_destroy(context, &container, &answer) == STELLAR_RESULT_OK && answer == 1);
        CHECK(stellar_container_count(context, &container, &count) == STELLAR_RESULT_NOT_FOUND);

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
        StellarUid probe{};
        CHECK(stellar_identifier_create(&probe) == STELLAR_RESULT_OK);
        int32_t inDefault = -1;
        CHECK(stellar_identifier_register(context, &probe, &inDefault) == STELLAR_RESULT_OK && inDefault == 1);
        CHECK(stellar_identifier_contains(nullptr, &probe, &inDefault) == STELLAR_RESULT_OK && inDefault == 0);
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

        // Two engines know nothing of each other: the same identifier, the same label name in both
        const auto id = Identifier::Create();
        CHECK(editor.Identifiers().Register(id));
        CHECK(game.Identifiers().Register(id));
        CHECK(game.Identifiers().Unregister(id));
        CHECK(editor.Identifiers().Contains(id));
        CHECK(!CoreContext::Default().Identifiers().Contains(id));

        CHECK(editor.Labels().Register("Player", Identifier::Create()).has_value());
        CHECK(game.Labels().Register("Player", Identifier::Create()).has_value());
        CHECK(editor.Labels().Get("Player")->GetUID() != game.Labels().Get("Player")->GetUID());

        CHECK(editor.Containers().Create(id));
        CHECK(!game.Containers().Exists(id));

        CHECK(&CoreContext::Default() == &CoreContext::Default());
        CHECK(&editor.Identifiers() != &game.Identifiers());

        // A context that goes takes its registries with it
        {
            CoreContext temporary;
            CHECK(temporary.Identifiers().Register(id));
        }
        CHECK(editor.Identifiers().Contains(id));
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

int main(int, char* argv[])
{
    const auto directory = std::filesystem::absolute(argv[0]).parent_path();

    TestIdentifier();
    TestIdentifierRegistry();
    TestLabelRegistry();
    TestConcurrentMap();
    TestPriorityQueue();
    TestConcurrentVector();
    TestDoubleBufferedMap();
    TestDataContainers();
    TestHandlePool();
    TestContainerRegistry();
    TestExports((directory / "Stellar.Native.TestModule.dll").string());
    TestModuleContract();
    TestContexts();

    std::printf("%d checks, %d failed\n", checks, failures);
    return failures == 0 ? 0 : 1;
}
