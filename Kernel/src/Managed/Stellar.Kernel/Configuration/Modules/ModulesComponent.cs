using System;

namespace Stellar.Kernel.Configuration.Modules;

/// <summary>
/// The modules of an application, in the order they are loaded and initialized.
/// </summary>
/// <remarks>
/// Resolved at build time from the dependencies of the application and the catalog of the installation: the runtime
/// does not discover modules, it checks that the ones the application was built with are there.
/// </remarks>
public class ModulesComponent
    : ConfigurationComponent
{
    /// <summary>
    /// The value of <see cref="Kind"/> for this component.
    /// </summary>
    public const string ComponentKind = "Modules";

    /// <inheritdoc/>
    public override string Kind => ComponentKind;

    /// <inheritdoc/>
    public override ConfigurationComponentBuildType ComponentBuildType =>
        ConfigurationComponentBuildType.EngineComponent;

    /// <summary>
    /// Gets or sets the modules, every module after the ones it depends on.
    /// </summary>
    public RuntimeModuleData[] Modules { get; set; } = Array.Empty<RuntimeModuleData>();
}
