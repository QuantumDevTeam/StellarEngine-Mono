// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Platform/Contracts/Shell/Interfaces.h
/// @brief COM-like Windows Shell integration interfaces.
///
/// The declarations intentionally use opaque pointers and integer status codes
/// instead of Windows Shell types. Implementations cast the opaque values to
/// their native counterparts (IShellItemArray, IStream, HWND, ...), which keeps
/// this contract free of windows.h and shellapi.h dependencies.
///
/// Methods return a native status code: zero means success, non-zero is a COM
/// HRESULT on Windows. Output parameters follow COM ownership rules unless a
/// method documents otherwise.
#pragma once

#include "Platform/Types.h" // NOLINT

namespace Stellar::Native::Kernel::Platform::Shell
{
    /// @brief Provides file thumbnails to Explorer.
    class API IThumbnailProvider // NOLINT
    {
    public:
        virtual ~IThumbnailProvider() = default;

        /// @brief Renders a thumbnail for the initialized file.
        /// @param width Requested thumbnail width in pixels.
        /// @param height Requested thumbnail height in pixels.
        /// @param bitmap Receives a native HBITMAP; the caller owns the bitmap and must delete it.
        /// @param alpha Receives true when the bitmap contains an alpha channel.
        /// @return Native status code; zero on success.
        /// @note The provider must have been initialized with IInitializeWithFile
        ///       or IInitializeWithStream before this call.
        virtual intptr_t GetThumbnail(uint32_t width, uint32_t height, void** bitmap, bool* alpha) = 0;
    };

    must_be_abstract(IThumbnailProvider)

    /// @brief Hosts a file preview inside an Explorer preview pane.
    class API IPreviewHandler // NOLINT
    {
    public:
        virtual ~IPreviewHandler() = default;

        /// @brief Sets the parent window and the preview rectangle.
        /// @param hwnd Native parent window handle (HWND).
        /// @param rect Native RECT describing the preview area.
        /// @return Native status code; zero on success.
        virtual intptr_t SetWindow(void* hwnd, const void* rect) = 0;

        /// @brief Updates the preview rectangle.
        /// @param rect Native RECT describing the preview area.
        /// @return Native status code; zero on success.
        virtual intptr_t SetRect(const void* rect) = 0;

        /// @brief Renders the preview into the configured window and rectangle.
        /// @return Native status code; zero on success.
        virtual intptr_t DoPreview() = 0;

        /// @brief Releases resources and stops the preview.
        /// @return Native status code; zero on success.
        virtual intptr_t Unload() = 0;

        /// @brief Gives keyboard focus to the preview.
        /// @return Native status code; zero on success.
        virtual intptr_t SetFocus() = 0;

        /// @brief Queries whether the preview currently has focus.
        /// @return Native focus result; its exact representation is defined by
        ///         the backend's COM interface.
        virtual intptr_t QueryFocus() = 0;

        /// @brief Offers a native message to the preview for accelerator handling.
        /// @param msg Native MSG pointer.
        /// @return Native status code; zero when the message was handled.
        virtual intptr_t TranslateAccelerator(const void* msg) = 0;
    };

    must_be_abstract(IPreviewHandler)

    /// @brief Explorer context-menu command.
    ///
    /// The command is initialized with one of the IInitializeWith* interfaces
    /// and then queried by Explorer for its title, icon, state and flags.
    class API IExplorerCommand // NOLINT
    {
    public:
        virtual ~IExplorerCommand() = default;

        /// @brief Returns the command title for the selected items.
        /// @param items Opaque IShellItemArray pointer describing the selection.
        /// @param name Receives a COM-allocated wide string; the caller owns it.
        /// @return Native status code; zero on success.
        virtual intptr_t GetTitle(void* items, void** name) = 0;

        /// @brief Returns the command icon for the selected items.
        /// @param items Opaque IShellItemArray pointer describing the selection.
        /// @param icon Receives a COM-allocated icon resource string; the caller owns it.
        /// @return Native status code; zero on success.
        virtual intptr_t GetIcon(void* items, void** icon) = 0;

        /// @brief Returns the command tooltip for the selected items.
        /// @param items Opaque IShellItemArray pointer describing the selection.
        /// @param toolTip Receives a COM-allocated wide string; the caller owns it.
        /// @return Native status code; zero on success.
        virtual intptr_t GetToolTip(void* items, void** toolTip) = 0;

        /// @brief Returns the canonical command name.
        /// @param items Opaque IShellItemArray pointer describing the selection.
        /// @param name Receives a COM-allocated wide string; the caller owns it.
        /// @return Native status code; zero on success.
        virtual intptr_t GetCanonicalName(void* items, void** name) = 0;

        /// @brief Queries the command's enabled/checked state.
        /// @param items Opaque IShellItemArray pointer describing the selection.
        /// @param okToBeSlow True when Explorer allows a potentially slow query.
        /// @param state Receives native command-state flags.
        /// @return Native status code; zero on success.
        virtual intptr_t GetState(void* items, bool okToBeSlow, uint32_t* state) = 0;

        /// @brief Executes the command for the selected items.
        /// @param items Opaque IShellItemArray pointer describing the selection.
        /// @param bindCtx Opaque IBindCtx pointer for the invocation; may be null.
        /// @return Native status code; zero on success.
        virtual intptr_t Invoke(void* items, void* bindCtx) = 0;

        /// @brief Returns native command flags.
        /// @param flags Receives the native flag mask.
        /// @return Native status code; zero on success.
        virtual intptr_t GetFlags(uint32_t* flags) = 0;

        /// @brief Enumerates subcommands of this command.
        /// @param enumCmd Opaque IEnumExplorerCommand pointer supplied by Explorer.
        /// @return Native status code; zero on success.
        virtual intptr_t EnumSubCommands(void* enumCmd) = 0;
    };

    must_be_abstract(IExplorerCommand)

    /// @brief Initializes a Shell handler from a file path.
    class API IInitializeWithFile // NOLINT
    {
    public:
        virtual ~IInitializeWithFile() = default;

        /// @brief Initializes the handler with a file.
        /// @param filePath Null-terminated UTF-16 file path.
        /// @param mode Native storage mode flags (STGM_READ, ...).
        /// @return Native status code; zero on success.
        /// @note The handler must copy any data it needs; the path string is
        ///       owned by the caller.
        virtual intptr_t Initialize(const wchar_t* filePath, uint32_t mode) = 0;
    };

    must_be_abstract(IInitializeWithFile)

    /// @brief Initializes a Shell handler from a stream.
    class API IInitializeWithStream // NOLINT
    {
    public:
        virtual ~IInitializeWithStream() = default;

        /// @brief Initializes the handler with a stream.
        /// @param stream Opaque IStream pointer.
        /// @param mode Native storage mode flags.
        /// @return Native status code; zero on success.
        /// @note The handler does not take ownership of the stream.
        virtual intptr_t Initialize(void* stream, uint32_t mode) = 0;
    };

    must_be_abstract(IInitializeWithStream)

    /// @brief Initializes a Shell handler from a Shell item.
    class API IInitializeWithItem // NOLINT
    {
    public:
        virtual ~IInitializeWithItem() = default;

        /// @brief Initializes the handler with a Shell item.
        /// @param item Opaque IShellItem pointer.
        /// @param mode Native storage mode flags.
        /// @return Native status code; zero on success.
        /// @note The handler does not take ownership of the item.
        virtual intptr_t Initialize(void* item, uint32_t mode) = 0;
    };

    must_be_abstract(IInitializeWithItem)

    // TODO: Add other Shell extensions
}
