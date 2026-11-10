namespace Stellar.Kernel.Modules;

/// <summary>
/// How projects reference a module.
/// </summary>
public enum ModuleLink
{
    /// <summary>As the project decides: sources in Debug, packages otherwise (<c>StellarPrecompiled</c>).</summary>
    Default = 0,

    /// <summary>Always the sources, through a project reference.</summary>
    Sources = 1,

    /// <summary>Always the package of the local feed, even in Debug.</summary>
    Package = 2
}
