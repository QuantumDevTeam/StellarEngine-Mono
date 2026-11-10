using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Stellar.Kernel.Interop;
using Stellar.Kernel.Versioning;

[assembly: DisableRuntimeMarshalling]

namespace Stellar.Core.Native;

/// <summary>
/// The exports of Stellar.Native.Core.Wrapper.PInvoke and the conventions around them.
/// </summary>
/// <remarks>
/// Only blittable types cross the boundary, so no marshalling happens: a <see cref="Guid"/> has the memory layout of
/// the native identifier, and strings travel as UTF-8 views.
/// </remarks>
internal static unsafe partial class Exports
{
    private const string Library = "Stellar.Native.Core.Wrapper.PInvoke";

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct StringView(byte* data, int size)
    {
        private readonly byte* _data = data;
        private readonly nuint _size = (nuint)size;
    }

    /// <summary>Calls a function that answers yes or no.</summary>
    /// <summary>A call that answers yes or no.</summary>
    internal delegate NativeResult Question(int* answer);

    /// <summary>A call that fills a buffer of text.</summary>
    internal delegate NativeResult TextReader(byte* buffer, nuint capacity, nuint* required);

    /// <summary>A call that fills a buffer of identifiers.</summary>
    internal delegate NativeResult IdentifierReader(Guid* buffer, nuint capacity, nuint* required);

    internal static bool Ask(NativeResult result, int answer)
    {
        Check(result);
        return answer != 0;
    }

    internal static string ReadText(Guid* identifier, delegate*<Guid*, byte*, nuint, nuint*, NativeResult> reader)
    {
        Span<byte> stackBuffer = stackalloc byte[128];

        fixed (byte* buffer = stackBuffer)
        {
            nuint required;
            var result = reader(identifier, buffer, (nuint)stackBuffer.Length, &required);

            if (result == NativeResult.Ok)
                return Encoding.UTF8.GetString(buffer, (int)required);

            if (result != NativeResult.BufferTooSmall)
                throw new NativeCoreException(result, LastError());

            var heapBuffer = new byte[(int)required + 1];

            fixed (byte* retry = heapBuffer)
            {
                Check(reader(identifier, retry, (nuint)heapBuffer.Length, &required));
                return Encoding.UTF8.GetString(retry, (int)required);
            }
        }
    }

    internal static string ReadText(TextReader reader)
    {
        Span<byte> stackBuffer = stackalloc byte[128];

        fixed (byte* buffer = stackBuffer)
        {
            nuint required;
            var result = reader(buffer, (nuint)stackBuffer.Length, &required);

            if (result == NativeResult.Ok)
                return Encoding.UTF8.GetString(buffer, (int)required);

            if (result != NativeResult.BufferTooSmall)
                throw new NativeCoreException(result, LastError());

            var heapBuffer = new byte[(int)required + 1];

            fixed (byte* retry = heapBuffer)
            {
                Check(reader(retry, (nuint)heapBuffer.Length, &required));
                return Encoding.UTF8.GetString(retry, (int)required);
            }
        }
    }

    internal static Guid[] ReadIdentifiers(IdentifierReader reader)
    {
        while (true)
        {
            nuint required;
            var probe = reader(null, 0, &required);

            if (probe == NativeResult.Ok)
                return [];

            if (probe != NativeResult.BufferTooSmall)
                throw new NativeCoreException(probe, LastError());

            var result = new Guid[(int)required];

            fixed (Guid* buffer = result)
            {
                var read = reader(buffer, (nuint)result.Length, &required);

                // The set may grow between the two calls: then ask again
                if (read == NativeResult.BufferTooSmall)
                    continue;

                Check(read);
                return result.AsSpan(0, (int)required).ToArray();
            }
        }
    }

    internal static void Check(NativeResult result)
    {
        if (result != NativeResult.Ok)
            throw new NativeCoreException(result, LastError());
    }

    private static string LastError()
    {
        Span<byte> buffer = stackalloc byte[1024];

        fixed (byte* data = buffer)
        {
            nuint required;
            var result = stellar_core_get_last_error(data, (nuint)buffer.Length, &required);

            return result == NativeResult.Ok ? Encoding.UTF8.GetString(data, (int)required) : string.Empty;
        }
    }

    // Library

    [LibraryImport(Library)]
    internal static partial NativeResult stellar_core_get_abi_version(ContractVersion* version);

    [LibraryImport(Library)]
    internal static partial NativeResult stellar_core_get_last_error(byte* buffer, nuint capacity, nuint* required);

    // Contexts

    [LibraryImport(Library)]
    internal static partial NativeResult stellar_core_context_create(nint* context);

    [LibraryImport(Library)]
    internal static partial NativeResult stellar_core_context_destroy(nint context);

    // Identifiers

    [LibraryImport(Library)]
    internal static partial NativeResult stellar_identifier_create(Guid* identifier);

    [LibraryImport(Library)]
    internal static partial NativeResult stellar_identifier_parse(StringView text, Guid* identifier, int* parsed);

    [LibraryImport(Library)]
    internal static partial NativeResult stellar_identifier_format(Guid* identifier, byte* buffer, nuint capacity,
        nuint* required);

    [LibraryImport(Library)]
    internal static partial NativeResult stellar_identifier_register(nint context, Guid* identifier, int* added);

    [LibraryImport(Library)]
    internal static partial NativeResult stellar_identifier_unregister(nint context, Guid* identifier, int* removed);

    [LibraryImport(Library)]
    internal static partial NativeResult stellar_identifier_contains(nint context, Guid* identifier, int* contained);

    [LibraryImport(Library)]
    internal static partial NativeResult stellar_identifier_count(nint context, nuint* count);

    [LibraryImport(Library)]
    internal static partial NativeResult stellar_identifier_snapshot(nint context, Guid* buffer, nuint capacity,
        nuint* required);

    // Labels

    [LibraryImport(Library)]
    internal static partial NativeResult stellar_label_register(nint context, StringView name, Guid* identifier,
        int* added);

    [LibraryImport(Library)]
    internal static partial NativeResult
        stellar_label_find(nint context, StringView name, Guid* identifier, int* found);

    [LibraryImport(Library)]
    internal static partial NativeResult stellar_label_get_name(nint context, Guid* identifier, byte* buffer,
        nuint capacity, nuint* required);

    [LibraryImport(Library)]
    internal static partial NativeResult stellar_label_unregister(nint context, Guid* identifier, int* removed);

    [LibraryImport(Library)]
    internal static partial NativeResult stellar_label_contains(nint context, Guid* identifier, int* contained);

    [LibraryImport(Library)]
    internal static partial NativeResult stellar_label_count(nint context, nuint* count);

    // Containers

    [LibraryImport(Library)]
    internal static partial NativeResult stellar_container_create(nint context, Guid* container, int* created);

    [LibraryImport(Library)]
    internal static partial NativeResult stellar_container_destroy(nint context, Guid* container, int* destroyed);

    [LibraryImport(Library)]
    internal static partial NativeResult stellar_container_exists(nint context, Guid* container, int* exists);

    [LibraryImport(Library)]
    internal static partial NativeResult stellar_container_seal(nint context, Guid* container);

    [LibraryImport(Library)]
    internal static partial NativeResult stellar_container_is_sealed(nint context, Guid* container, int* isSealed);

    [LibraryImport(Library)]
    internal static partial NativeResult stellar_container_add(nint context, Guid* container, Guid* item, int* added);

    [LibraryImport(Library)]
    internal static partial NativeResult stellar_container_remove(nint context, Guid* container, Guid* item,
        int* removed);

    [LibraryImport(Library)]
    internal static partial NativeResult stellar_container_contains(nint context, Guid* container, Guid* item,
        int* contained);

    [LibraryImport(Library)]
    internal static partial NativeResult stellar_container_count(nint context, Guid* container, nuint* count);

    [LibraryImport(Library)]
    internal static partial NativeResult stellar_container_items(nint context, Guid* container, Guid* buffer,
        nuint capacity, nuint* required);

    [LibraryImport(Library)]
    internal static partial NativeResult stellar_container_clear(nint context, Guid* container, int* cleared);

    [LibraryImport(Library)]
    internal static partial NativeResult stellar_container_add_branch(nint context, Guid* container, Guid* branch,
        int* added);

    [LibraryImport(Library)]
    internal static partial NativeResult stellar_container_remove_branch(nint context, Guid* container, Guid* branch,
        int* removed);

    [LibraryImport(Library)]
    internal static partial NativeResult stellar_container_contains_branch(nint context, Guid* container, Guid* branch,
        int* contained);

    [LibraryImport(Library)]
    internal static partial NativeResult stellar_container_branches(nint context, Guid* container, Guid* buffer,
        nuint capacity, nuint* required);

    // Modules

    [LibraryImport(Library)]
    internal static partial NativeResult stellar_module_load(nint context, StringView path, NativeHandle* module);

    [LibraryImport(Library)]
    internal static partial NativeResult stellar_module_unload(nint context, NativeHandle module, int* unloaded);

    [LibraryImport(Library)]
    internal static partial NativeResult stellar_module_get_name(nint context, NativeHandle module, byte* buffer,
        nuint capacity, nuint* required);

    [LibraryImport(Library)]
    internal static partial NativeResult stellar_module_count(nint context, nuint* count);
}
