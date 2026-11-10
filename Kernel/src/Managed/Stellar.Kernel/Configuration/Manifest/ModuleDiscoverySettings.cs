namespace Stellar.Kernel.Configuration.Manifest;

/// <summary>
/// How an installation finds its modules: every folder of <see cref="Directories"/> that holds a
/// <c>&lt;Id&gt;.stellar.module</c> file is a module.
/// </summary>
public sealed class ModuleDiscoverySettings
{
    /// <summary>
    /// Gets or sets a value indicating whether modules are found automatically. Without it, only the modules the
    /// manifest points at exist.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the folders searched, relative to the root of the installation or absolute.
    /// </summary>
    public string[] Directories { get; set; } = { "modules" };

    /// <summary>
    /// Gets or sets how deep below a folder of <see cref="Directories"/> a module may sit: 1 is
    /// <c>modules/&lt;module&gt;</c>, 2 also <c>modules/&lt;group&gt;/&lt;module&gt;</c>.
    /// </summary>
    public int Depth { get; set; } = 2;
}
