#include "pch.h"
#include "Modules/ModuleRegistry.h"

#include "Data/Collections/HandlePool.h"
#include "Failures/NativeException.h"
#include "Modules/SharedLibrary.h"

namespace Stellar::Native::Core::Modules
{
    namespace
    {
        constexpr StellarContractVersion kContract = {STELLAR_MODULE_CONTRACT_MAJOR, STELLAR_MODULE_CONTRACT_MINOR};

        [[nodiscard]] std::string ToString(StellarStringView view)
        {
            return view.data == nullptr ? std::string{} : std::string(view.data, view.size);
        }

        struct Module
        {
            std::string name;
            std::string version;
            void (STELLAR_CALL*shutdown)(void) = nullptr;
            SharedLibrary library;
        };
    }

    struct ModuleRegistry::Impl
    {
        Data::Collections::HandlePool<Module> modules;

        [[nodiscard]] StellarHandle Install(Link<const StellarModuleInfo> info, SharedLibrary library)
        {
            // The check of structSize comes first: the fields after it may not exist in an older module
            if (info.structSize < sizeof(StellarModuleInfo))
                throw Failures::NativeException("The module description is too small", STELLAR_RESULT_VERSION_MISMATCH);

            const auto name = ToString(info.name);

            if (!IsContractSatisfied(kContract, info.contract))
                throw Failures::NativeException(
                    std::format(
                        "{} needs module contract {}.{}, the engine provides {}.{}",
                        name, info.contract.major, info.contract.minor, kContract.major, kContract.minor),
                    STELLAR_RESULT_VERSION_MISMATCH);

            if (info.initialize != nullptr)
            {
                if (const StellarResult result = info.initialize(); result != STELLAR_RESULT_OK)
                    throw Failures::NativeException(std::format("{} failed to initialize", name), result);
            }

            return modules.Create(Module{name, ToString(info.version), info.shutdown, std::move(library)});
        }
    };

    ModuleRegistry::ModuleRegistry() :
        _impl(std::make_unique<Impl>()) {}

    ModuleRegistry::~ModuleRegistry() = default;


    StellarHandle ModuleRegistry::Load(std::string_view path)
    {
        auto library = SharedLibrary::Open(path);

        const auto entry = reinterpret_cast<StellarModuleEntry>(library.FindSymbol(STELLAR_MODULE_ENTRY_NAME).get());

        if (entry == nullptr)
            throw Failures::NativeException(
                std::format("{} does not export {}", path, STELLAR_MODULE_ENTRY_NAME), STELLAR_RESULT_INVALID_ARGUMENT);

        const Ptr<const StellarModuleInfo> info = entry();

        if (!info)
            throw Failures::NativeException(
                std::format("{} returned no module description", path), STELLAR_RESULT_INVALID_ARGUMENT);

        return _impl->Install(*info, std::move(library));
    }

    StellarHandle ModuleRegistry::Add(Link<const StellarModuleInfo> info)
    {
        return _impl->Install(info, SharedLibrary{});
    }

    bool ModuleRegistry::Unload(StellarHandle module)
    {
        auto taken = _impl->modules.Take(module);
        if (!taken.has_value())
            return false;

        if (taken->shutdown != nullptr)
            taken->shutdown();

        return true;
    }

    std::optional<std::string> ModuleRegistry::GetName(StellarHandle module) const
    {
        std::optional<std::string> name;

        _impl->modules.With(module, [&name](Link<const Module> loaded) { name = loaded.name; });

        return name;
    }

    std::size_t ModuleRegistry::size() const
    {
        return _impl->modules.size();
    }
}
