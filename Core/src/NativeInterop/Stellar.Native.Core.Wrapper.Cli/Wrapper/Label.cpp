#include "Wrapper/Label.cli.h"
#include "Wrapper/Data/Registry/LabelRegistry.cli.h"
#include "Wrapper/Native/CoreContext.cli.h"

using namespace Stellar::Kernel::Data::Registry;

namespace Stellar::Core
{
    namespace
    {
        IRegistry<Label^>^ RegistryOf(IQuantumObject^ registry)
        {
            Object^ target = registry == nullptr ? Native::CoreContext::Current->Labels : registry;
            auto typed = dynamic_cast<IRegistry<Label^>^>(target);

            if (typed == nullptr)
                throw gcnew ArgumentException(target->GetType()->Name + " cannot store labels", "registry");

            return typed;
        }
    }

    Label::Label(Identifier^ identifier, String^ name) :
        _uid(identifier),
        _name(name) {}

    Label^ Label::FromNative(Guid identifier, String^ name)
    {
        return gcnew Label(gcnew Identifier(identifier), name);
    }

    IIdentifier^ Label::UID::get()
    {
        return _uid;
    }

    String^ Label::Name::get()
    {
        return _name;
    }

    void Label::Register()
    {
        Register(nullptr);
    }

    void Label::Register(IQuantumObject^ registry)
    {
        RegistryOf(registry)->Register(this);
    }

    void Label::Unregister()
    {
        Unregister(nullptr);
    }

    void Label::Unregister(IQuantumObject^ registry)
    {
        RegistryOf(registry)->Pop(_uid);
    }

    Label::~Label()
    {
        Unregister();
    }

    bool Label::Equals(Label^ other)
    {
        return other != nullptr && _uid->UID == other->_uid->UID && _name == other->_name;
    }

    bool Label::Equals(Object^ obj)
    {
        return Equals(dynamic_cast<Label^>(obj));
    }

    int Label::GetHashCode()
    {
        return HashCode::Combine(_uid->UID, _name);
    }

    String^ Label::ToString()
    {
        return "Label#" + _name;
    }

    Label^ Label::Create(String^ name)
    {
        return Create(name, nullptr, true);
    }

    Label^ Label::Create(String^ name, IIdentifier^ identifier)
    {
        return Create(name, identifier, true);
    }

    Label^ Label::Create(String^ name, bool shouldRegister)
    {
        return Create(name, nullptr, shouldRegister);
    }

    Label^ Label::Create(String^ name, IIdentifier^ identifier, bool shouldRegister)
    {
        if (String::IsNullOrWhiteSpace(name))
            return nullptr;

        auto label = gcnew Label(identifier == nullptr ? Identifier::Create() : gcnew Identifier(identifier->UID), name);

        if (shouldRegister && !Native::CoreContext::Current->Labels->Register(label))
            return nullptr;

        return label;
    }

    Label^ Label::Get(IIdentifier^ identifier)
    {
        return Native::CoreContext::Current->Labels->Get(identifier);
    }

    Label^ Label::Get(String^ name)
    {
        return Native::CoreContext::Current->Labels->GetByName(name);
    }

    Label::operator Label^(String^ name)
    {
        Label^ existing = Get(name);
        return existing != nullptr ? existing : Create(name, nullptr, true);
    }
}
