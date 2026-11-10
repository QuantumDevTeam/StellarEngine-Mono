using System.Text;
using Stellar.Core.Native;
using Stellar.Kernel.Interop;

namespace Stellar.Core.Modules;

/// <summary>
/// The native modules a <see cref="CoreContext"/> loaded. Disposing the context unloads them.
/// </summary>
public sealed unsafe class ModuleRegistry
{
    private readonly CoreContext _context;

    internal ModuleRegistry(CoreContext context)
    {
        _context = context;
    }

    /// <summary>Gets the number of loaded modules.</summary>
    public int Count
    {
        get
        {
            nuint count;
            Exports.Check(Exports.stellar_module_count(_context.Handle, &count));
            return (int)count;
        }
    }

    /// <summary>Loads a native module.</summary>
    /// <param name="path">The library.</param>
    /// <returns>The handle of the module.</returns>
    public NativeHandle Load(string path)
    {
        var bytes = Encoding.UTF8.GetBytes(path);

        fixed (byte* data = bytes)
        {
            NativeHandle module;
            Exports.Check(Exports.stellar_module_load(_context.Handle, new Exports.StringView(data, bytes.Length),
                &module));
            return module;
        }
    }

    /// <summary>Unloads a module. <returns>False if the handle is not a loaded module.</returns></summary>
    public bool Unload(NativeHandle module)
    {
        int unloaded;
        Exports.Check(Exports.stellar_module_unload(_context.Handle, module, &unloaded));
        return unloaded != 0;
    }

    /// <summary>Gets the name of a module.</summary>
    public string GetName(NativeHandle module)
    {
        var context = _context.Handle;
        return Exports.ReadText((buffer, capacity, required) =>
            Exports.stellar_module_get_name(context, module, buffer, capacity, required));
    }
}
