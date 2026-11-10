// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Platform/Contracts/Core/Enums/KeyCode.h
/// @brief Layout-independent keyboard key codes.
#pragma once
// ReSharper disable CppClangTidyPerformanceEnumSize

namespace Stellar::Native::Kernel::Platform
{
    /// @brief Logical keyboard key.
    ///
    /// Values describe physical/logical keys rather than characters; text
    /// production is reported separately through TextInputPayload. Backends
    /// translate native scan codes/virtual keys to this enumeration.
    enum class API KeyCode : uint16_t
    {
        Unknown = 0, ///< Unrecognized key.

        // Letters
        A, ///< Letter A key.
        B, ///< Letter B key.
        C, ///< Letter C key.
        D, ///< Letter D key.
        E, ///< Letter E key.
        F, ///< Letter F key.
        G, ///< Letter G key.
        H, ///< Letter H key.
        I, ///< Letter I key.
        J, ///< Letter J key.
        K, ///< Letter K key.
        L, ///< Letter L key.
        M, ///< Letter M key.
        N, ///< Letter N key.
        O, ///< Letter O key.
        P, ///< Letter P key.
        Q, ///< Letter Q key.
        R, ///< Letter R key.
        S, ///< Letter S key.
        T, ///< Letter T key.
        U, ///< Letter U key.
        V, ///< Letter V key.
        W, ///< Letter W key.
        X, ///< Letter X key.
        Y, ///< Letter Y key.
        Z, ///< Letter Z key.

        // Digits
        Num0, ///< Top-row digit key 0.
        Num1, ///< Top-row digit key 1.
        Num2, ///< Top-row digit key 2.
        Num3, ///< Top-row digit key 3.
        Num4, ///< Top-row digit key 4.
        Num5, ///< Top-row digit key 5.
        Num6, ///< Top-row digit key 6.
        Num7, ///< Top-row digit key 7.
        Num8, ///< Top-row digit key 8.
        Num9, ///< Top-row digit key 9.

        // Function
        F1,  ///< Function key F1.
        F2,  ///< Function key F2.
        F3,  ///< Function key F3.
        F4,  ///< Function key F4.
        F5,  ///< Function key F5.
        F6,  ///< Function key F6.
        F7,  ///< Function key F7.
        F8,  ///< Function key F8.
        F9,  ///< Function key F9.
        F10, ///< Function key F10.
        F11, ///< Function key F11.
        F12, ///< Function key F12.
        F13, ///< Function key F13.
        F14, ///< Function key F14.
        F15, ///< Function key F15.
        F16, ///< Function key F16.
        F17, ///< Function key F17.
        F18, ///< Function key F18.
        F19, ///< Function key F19.
        F20, ///< Function key F20.
        F21, ///< Function key F21.
        F22, ///< Function key F22.
        F23, ///< Function key F23.
        F24, ///< Function key F24.

        // Control
        Escape,    ///< Escape key.
        Enter,     ///< Main Enter/Return key.
        Tab,       ///< Tab key.
        Backspace, ///< Backspace key.
        Space,     ///< Space bar.
        Delete,    ///< Delete key.
        Insert,    ///< Insert key.
        Home,      ///< Home key.
        End,       ///< End key.
        PageUp,    ///< Page Up key.
        PageDown,  ///< Page Down key.
        Left,      ///< Left arrow.
        Right,     ///< Right arrow.
        Up,        ///< Up arrow.
        Down,      ///< Down arrow.

        // Modifiers
        LeftShift,   ///< Left Shift key.
        RightShift,  ///< Right Shift key.
        LeftCtrl,    ///< Left Control key.
        RightCtrl,   ///< Right Control key.
        LeftAlt,     ///< Left Alt key.
        RightAlt,    ///< Right Alt/AltGr key.
        LeftSuper,   ///< Left Super/Windows/Command key.
        RightSuper,  ///< Right Super/Windows/Command key.

        // Locks
        CapsLock,   ///< Caps Lock toggle.
        NumLock,    ///< Num Lock toggle.
        ScrollLock, ///< Scroll Lock toggle.

        // Numpad
        Numpad0, ///< Numeric keypad digit 0.
        Numpad1, ///< Numeric keypad digit 1.
        Numpad2, ///< Numeric keypad digit 2.
        Numpad3, ///< Numeric keypad digit 3.
        Numpad4, ///< Numeric keypad digit 4.
        Numpad5, ///< Numeric keypad digit 5.
        Numpad6, ///< Numeric keypad digit 6.
        Numpad7, ///< Numeric keypad digit 7.
        Numpad8, ///< Numeric keypad digit 8.
        Numpad9, ///< Numeric keypad digit 9.
        NumpadAdd,      ///< Numeric keypad plus.
        NumpadSubtract, ///< Numeric keypad minus.
        NumpadMultiply, ///< Numeric keypad multiply.
        NumpadDivide,   ///< Numeric keypad divide.
        NumpadDecimal,  ///< Numeric keypad decimal separator.
        NumpadEnter,    ///< Numeric keypad Enter.

        // Symbols
        Minus,        ///< '-' / '_' key.
        Equal,        ///< '=' / '+' key.
        LeftBracket,  ///< '[' / '{' key.
        RightBracket, ///< ']' / '}' key.
        Semicolon,    ///< ';' / ':' key.
        Apostrophe,   ///< ' / " key.
        Grave,        ///< '`' / '~' key.
        Backslash,    ///< '\\' / '|' key.
        Comma,        ///< ',' / '<' key.
        Period,       ///< '.' / '>' key.
        Slash,        ///< '/' / '?' key.

        Count, ///< Number of key codes; not a real key.
    };
}
