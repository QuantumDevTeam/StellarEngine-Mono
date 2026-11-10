#include "pch.h"
#include "Label.h"

namespace Stellar::Native::Core
{
    Label Label::CreateBound(std::string_view name)
    {
        return Label{name, Identifier::Create()};
    }

    std::string Label::ToString() const
    {
        return std::format("Label#{}", _name);
    }
}
