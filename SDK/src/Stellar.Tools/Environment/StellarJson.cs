using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Stellar.Tools.Diagnostics;

namespace Stellar.Tools.Environment;

/// <summary>
/// Reads and writes the JSON files of the engine with the same settings everywhere.
/// </summary>
public static class StellarJson
{
    private static readonly JsonSerializerSettings Settings = new()
    {
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore,
        Converters = { new StringEnumConverter() }
    };

    /// <summary>
    /// Reads a JSON file.
    /// </summary>
    /// <typeparam name="T">The type of the content.</typeparam>
    /// <param name="path">The file.</param>
    /// <returns>The content.</returns>
    /// <exception cref="ToolException">Thrown if the file does not exist or is malformed.</exception>
    public static T Read<T>(string path)
        where T : class
    {
        if (!File.Exists(path))
            throw new ToolException($"File not found: {path}");

        try
        {
            return JsonConvert.DeserializeObject<T>(File.ReadAllText(path), Settings)
                   ?? throw new ToolException($"File is empty: {path}");
        }
        catch (JsonException exception)
        {
            throw new ToolException($"Malformed JSON in {path}: {exception.Message}", exception);
        }
    }

    /// <summary>
    /// Serializes a value.
    /// </summary>
    public static string Serialize(object value)
    {
        return JsonConvert.SerializeObject(value, Settings);
    }

    /// <summary>
    /// Writes a JSON file, creating its directory.
    /// </summary>
    /// <param name="path">The file.</param>
    /// <param name="value">The content.</param>
    public static void Write(string path, object value)
    {
        FileWriter.WriteAllText(path, Serialize(value));
    }
}

/// <summary>
/// File output shared by the tools.
/// </summary>
public static class FileWriter
{
    /// <summary>
    /// Writes a text file, creating its directory. An identical file is left untouched, so timestamps stay
    /// valid for incremental builds.
    /// </summary>
    /// <param name="path">The file.</param>
    /// <param name="content">The content.</param>
    /// <returns><c>true</c> if the file was written.</returns>
    public static bool WriteAllText(string path, string content)
    {
        if (File.Exists(path) && string.Equals(File.ReadAllText(path), content, StringComparison.Ordinal))
            return false;

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(path, content);
        return true;
    }
}
