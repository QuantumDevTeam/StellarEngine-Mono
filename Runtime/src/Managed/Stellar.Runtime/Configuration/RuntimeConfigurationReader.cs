using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using Stellar.Kernel.Configuration;
using Stellar.Kernel.Configuration.Modules;
using Stellar.Kernel.Configuration.Project.Assets;
using Stellar.Kernel.Configuration.Project.Localization;

namespace Stellar.Runtime.Configuration;

/// <summary>
/// Reads the runtime configuration generated at build time.
/// </summary>
public static class RuntimeConfigurationReader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly ConcurrentDictionary<string, Type> ComponentTypes = new(StringComparer.Ordinal)
    {
        [AssetsComponent.ComponentKind] = typeof(AssetsComponent),
        [LocalizationComponent.ComponentKind] = typeof(LocalizationComponent),
        [ModulesComponent.ComponentKind] = typeof(ModulesComponent)
    };

    /// <summary>
    /// Registers the type of a configuration component, so that modules can have components of their own.
    /// </summary>
    /// <param name="kind">The value of <see cref="ConfigurationComponent.Kind"/>.</param>
    /// <typeparam name="TComponent">The component type.</typeparam>
    public static void RegisterComponent<TComponent>(string kind)
        where TComponent : ConfigurationComponent
    {
        ComponentTypes[kind] = typeof(TComponent);
    }

    /// <summary>
    /// Reads a configuration file.
    /// </summary>
    /// <param name="path">The file.</param>
    /// <returns>The configuration.</returns>
    public static RuntimeConfiguration ReadFile(string path)
    {
        return Read(File.ReadAllText(path));
    }

    /// <summary>
    /// Reads a configuration.
    /// </summary>
    /// <param name="stream">The JSON text.</param>
    /// <returns>The configuration.</returns>
    public static RuntimeConfiguration Read(Stream stream)
    {
        using var reader = new StreamReader(stream);
        return Read(reader.ReadToEnd());
    }

    /// <summary>
    /// Reads a configuration.
    /// </summary>
    /// <param name="json">The JSON text.</param>
    /// <returns>The configuration. Components of unknown kinds are skipped.</returns>
    /// <exception cref="InvalidDataException">Thrown if the format version is not supported.</exception>
    public static RuntimeConfiguration Read(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var version = root.TryGetProperty(nameof(RuntimeConfiguration.SchemaVersion), out var versionElement)
            ? versionElement.GetInt32()
            : 0;

        if (version != RuntimeConfiguration.CurrentSchemaVersion)
            throw new InvalidDataException(
                $"Runtime configuration has format version {version}, " +
                $"this build understands {RuntimeConfiguration.CurrentSchemaVersion}");

        var components = new List<ConfigurationComponent>();

        if (root.TryGetProperty(nameof(RuntimeConfiguration.Components), out var componentsElement))
            foreach (var element in componentsElement.EnumerateArray())
            {
                var kind = element.GetProperty(nameof(ConfigurationComponent.Kind)).GetString() ?? string.Empty;

                if (ComponentTypes.TryGetValue(kind, out var type))
                    components.Add((ConfigurationComponent)element.Deserialize(type, Options)!);
            }

        var configuration = new RuntimeConfiguration
        {
            SchemaVersion = version,
            ProjectName = ReadString(root, nameof(RuntimeConfiguration.ProjectName)),
            CompanyName = ReadString(root, nameof(RuntimeConfiguration.CompanyName)),
            Version = ReadString(root, nameof(RuntimeConfiguration.Version)),
            StellarOrchesterVersion = ReadString(root, nameof(RuntimeConfiguration.StellarOrchesterVersion)),
            StellarEngineVersion = ReadString(root, nameof(RuntimeConfiguration.StellarEngineVersion)),
            EntryPoint = ReadString(root, nameof(RuntimeConfiguration.EntryPoint)),
            BuildDate = ReadString(root, nameof(RuntimeConfiguration.BuildDate)),
            Components = components.ToArray()
        };

        if (root.TryGetProperty(nameof(RuntimeConfiguration.Settings), out var settings))
            foreach (var setting in settings.EnumerateObject())
                configuration.Settings[setting.Name] = setting.Value.ValueKind == JsonValueKind.String
                    ? setting.Value.GetString() ?? string.Empty
                    : setting.Value.GetRawText();

        return configuration;
    }

    private static string ReadString(JsonElement root, string name)
    {
        return root.TryGetProperty(name, out var element) ? element.GetString() ?? string.Empty : string.Empty;
    }
}
