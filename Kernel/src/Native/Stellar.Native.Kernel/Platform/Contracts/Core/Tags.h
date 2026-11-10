// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Platform/Contracts/Core/Tags.h
/// @brief Empty tag types and fallback category mapping for platform handles.
///
/// The tags are compile-time labels. They carry no data and exist only to
/// parameterize handles and to document which service or object a handle
/// refers to. TagCategory maps each concrete tag to a category base, and
/// TagOf<Category>::Is<Tag> validates that mapping.
#pragma once

/// @brief Compile-time tags and category traits for platform handles.
namespace Stellar::Native::Kernel::Platform::Tags
{
    /// @brief Tag for a native window handle.
    struct Window {};

    /// @brief Tag for a registered window-class handle.
    struct WindowClass {};

    /// @brief Tag for a window-manager handle.
    struct WindowManager {};

    /// @brief Tag for an event-manager handle.
    struct EventManager {};

    /// @brief Tag for a native event-procedure handle.
    struct EventProc {};

    /// @brief Tag for an event-translator handle.
    struct EventTranslator {};

    /// @brief Tag for the platform service itself.
    struct Platform {};

    // Trait : Base

    /// @brief Root category for window-related tags.
    struct WindowTagBase {};

    /// @brief Root category for event-related tags.
    struct EventTagBase {};

    /// @brief Root category for manager-related tags.
    struct ManagerTagBase {};

    // binding to category

    /// @brief Maps a concrete tag to its category base.
    ///
    /// The primary template maps unknown tags to `void`, so
    /// TagOf<Category>::Is<Tag> evaluates to false for unrecognized tags.
    template <typename> struct TagCategory
    {
        using type = void; ///< Unknown tags have no category.
    };

    /// @brief Window tags belong to the window category.
    template <> struct TagCategory<Window>
    {
        using type = WindowTagBase; ///< Category base for this tag.
    };

    /// @brief Window-class tags belong to the window category.
    template <> struct TagCategory<WindowClass>
    {
        using type = WindowTagBase; ///< Category base for this tag.
    };

    /// @brief Window-manager tags belong to the manager category.
    template <> struct TagCategory<WindowManager>
    {
        using type = ManagerTagBase; ///< Category base for this tag.
    };

    /// @brief Event-manager tags belong to the manager category.
    template <> struct TagCategory<EventManager>
    {
        using type = ManagerTagBase; ///< Category base for this tag.
    };

    /// @brief Event-procedure tags belong to the event category.
    template <> struct TagCategory<EventProc>
    {
        using type = EventTagBase; ///< Category base for this tag.
    };

    /// @brief Event-translator tags belong to the event category.
    template <> struct TagCategory<EventTranslator>
    {
        using type = EventTagBase; ///< Category base for this tag.
    };

    // validation

    /// @brief Compile-time category validator for platform tags.
    ///
    /// Example: `Tags::TagOf<Tags::WindowTagBase>::Is<Tags::Window>` is true
    /// because TagCategory<Window>::type derives from WindowTagBase.
    ///
    /// @tparam Category Category base to test against.
    template <typename Category>
    struct TagOf
    {
        /// @brief True when @p Tag belongs to @p Category.
        /// @tparam Tag Concrete platform tag to test.
        template <typename Tag>
        static constexpr bool Is = std::is_base_of_v<
            Category, typename TagCategory<Tag>::type>;
    };
}
