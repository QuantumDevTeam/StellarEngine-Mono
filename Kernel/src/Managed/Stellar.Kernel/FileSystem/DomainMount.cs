using System.Collections.Generic;

namespace Stellar.Kernel.FileSystem;

/// <summary>
/// What to mount: the name of the domain, its driver and where the driver finds it.
/// </summary>
/// <remarks>
/// <para>The source is for the driver alone, and the only place where a location of the machine may appear. The
/// runtime resolves it from the configuration (<c>$(StellarRoot)</c>, the project, the folders of the user); a
/// game names domains, never sources.</para>
/// <para>Examples: <c>dir</c> takes a directory; <c>asm</c> an assembly name; <c>zip</c> the path of an archive in
/// another domain (<c>dlc://pack.zip</c>); <c>overlay</c> the domains of its layers, first wins
/// (<c>mods,dlc,base</c>).</para>
/// </remarks>
/// <param name="Name">The name of the domain, in <see cref="FilePath.Domain"/>.</param>
/// <param name="Scheme">The driver that mounts it.</param>
/// <param name="Source">Where the driver finds the domain, in the terms of the driver.</param>
/// <param name="Options">Settings of the driver, such as <c>readonly=true</c>; <c>null</c> for none.</param>
public readonly record struct DomainMount(
    string Name,
    DomainScheme Scheme,
    string Source,
    IReadOnlyDictionary<string, string>? Options = null);
