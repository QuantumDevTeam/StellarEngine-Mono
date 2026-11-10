using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Stellar.Tools.Configuration.Project;

namespace Stellar.Tools.Configuration;

/// <summary>
/// Represents data of a .stellar.project file.
/// </summary>
public class StellarConfigurationFile
{
    /// <summary>
    /// Represents the .stellar.project[Project] field: what the build needs.
    /// </summary>
    public ProjectConfigurationObject? Project { get; set; }

    /// <summary>
    /// Represents the .stellar.project[Runtime] field: free-form values passed to the runtime configuration.
    /// </summary>
    public Dictionary<string, JToken>? Runtime { get; set; }
}
