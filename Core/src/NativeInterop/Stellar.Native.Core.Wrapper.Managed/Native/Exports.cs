using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Stellar.Kernel;
using Stellar.Kernel.Interop;
using Stellar.Kernel.Versioning;

[assembly: DisableRuntimeMarshalling]

namespace Stellar.Core.Native;

/// <summary>
/// The exports of Stellar.Native.Core.Wrapper.PInvoke and the conventions around them.
/// </summary>
/// <remarks>
/// Only blittable types cross the boundary, so no marshalling happens: a <see cref="Identifier"/> has the memory layout of
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


    internal static bool Ask(NativeResult result, int answer)
    {
        Check(result);
        return answer != 0;
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
    internal static partial NativeResult stellar_identifier_create(Identifier* identifier);

    [LibraryImport(Library)]
    internal static partial NativeResult stellar_identifier_parse(StringView text, Identifier* identifier, int* parsed);

    [LibraryImport(Library)]
    internal static partial NativeResult stellar_identifier_format(Identifier* identifier, byte* buffer, nuint capacity,
        nuint* required);

    // Memory

    [LibraryImport(Library)]
    internal static partial NativeResult stellar_memory_get_api(MemoryFunctions.Table** api);

    [LibraryImport(Library)]
    internal static partial NativeResult stellar_core_context_get_allocator(nint context, nint* allocator);

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
