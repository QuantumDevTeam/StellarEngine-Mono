// ReSharper disable UnusedType.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

using Microsoft.Build.Framework;
using Stellar.Tools.Build;
using Stellar.Tools.Environment;

namespace Stellar.Sdk.Tasks;

/// <summary>
/// Generates the source of the launcher of an executable module.
/// </summary>
public sealed class GenerateStellarLauncher
    : StellarTask
{
    /// <summary>Gets or sets the simple name of the assembly of the application.</summary>
    [Required]
    public string Application { get; set; } = string.Empty;

    /// <summary>Gets or sets the file to write.</summary>
    [Required]
    public string OutputFile { get; set; } = string.Empty;

    /// <summary>Gets or sets the title of the application.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the company.</summary>
    public string Company { get; set; } = string.Empty;

    /// <summary>Gets or sets the product.</summary>
    public string Product { get; set; } = string.Empty;

    /// <summary>Gets or sets the copyright.</summary>
    public string Copyright { get; set; } = string.Empty;

    /// <summary>Gets or sets the version, as numbers.</summary>
    public string FileVersion { get; set; } = string.Empty;

    /// <summary>Gets or sets the version, as people read it.</summary>
    public string InformationalVersion { get; set; } = string.Empty;

    /// <inheritdoc/>
    protected override void Run()
    {
        FileWriter.WriteAllText(OutputFile, LauncherGenerator.Generate(new LauncherInfo
        {
            Application = Application,
            Title = Title,
            Company = Company,
            Product = Product,
            Copyright = Copyright,
            FileVersion = FileVersion,
            InformationalVersion = InformationalVersion
        }));
    }
}
