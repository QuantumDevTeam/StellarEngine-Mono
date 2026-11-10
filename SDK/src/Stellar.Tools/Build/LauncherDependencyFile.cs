using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Stellar.Tools.Build;

/// <summary>
/// An assembly the launcher of an application starts with.
/// </summary>
/// <param name="Name">The simple name of the assembly.</param>
/// <param name="Version">Its version.</param>
public sealed record LauncherAssembly(string Name, string Version);

/// <summary>
/// Generates the <c>.deps.json</c> of the launcher of an application.
/// </summary>
/// <remarks>
/// It lists the launcher, the runtime and the Kernel only: they are what the default context of the process starts
/// with. Everything else (the engine, the modules, the application) is loaded by the runtime, which finds it through
/// the <c>.deps.json</c> of the application. Without this file .NET would put every assembly of the folder in the
/// default context.
/// </remarks>
public static class LauncherDependencyFile
{
    /// <summary>
    /// Generates the file.
    /// </summary>
    /// <param name="targetFrameworkMoniker">The framework, as <c>$(TargetFrameworkMoniker)</c> says it.</param>
    /// <param name="launcher">The launcher.</param>
    /// <param name="assemblies">What the launcher depends on.</param>
    /// <returns>The JSON text.</returns>
    public static string Render(string targetFrameworkMoniker, LauncherAssembly launcher,
        IReadOnlyCollection<LauncherAssembly> assemblies)
    {
        var target = new JObject();
        var libraries = new JObject();

        var dependencies = new JObject();
        foreach (var assembly in assemblies) dependencies[assembly.Name] = assembly.Version;

        Add(launcher, dependencies);
        foreach (var assembly in assemblies) Add(assembly, null);

        var document = new JObject
        {
            ["runtimeTarget"] = new JObject { ["name"] = targetFrameworkMoniker, ["signature"] = string.Empty },
            ["compilationOptions"] = new JObject(),
            ["targets"] = new JObject { [targetFrameworkMoniker] = target },
            ["libraries"] = libraries
        };

        return document.ToString(Formatting.Indented) + "\n";

        void Add(LauncherAssembly assembly, JObject? assemblyDependencies)
        {
            var key = $"{assembly.Name}/{assembly.Version}";
            var entry = new JObject();

            if (assemblyDependencies is { Count: > 0 })
                entry["dependencies"] = assemblyDependencies;

            entry["runtime"] = new JObject { [assembly.Name + ".dll"] = new JObject() };
            target[key] = entry;

            libraries[key] = new JObject { ["type"] = "project", ["serviceable"] = false, ["sha512"] = string.Empty };
        }
    }
}
