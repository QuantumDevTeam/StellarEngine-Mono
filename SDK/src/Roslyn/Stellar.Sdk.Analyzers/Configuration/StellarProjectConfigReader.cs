// ReSharper disable UnusedAutoPropertyAccessor.Global

using System;

namespace Stellar.Sdk.Analyzers.Configuration;

internal readonly struct ConfigReadResult
{
    public bool IsSuccess { get; }
    public StellarAnalyzerConfigFile? Config { get; }
    public string? ErrorMessage { get; }

    private ConfigReadResult(StellarAnalyzerConfigFile config)
    {
        IsSuccess = true;
        Config = config;
        ErrorMessage = null;
    }

    private ConfigReadResult(string errorMessage)
    {
        IsSuccess = false;
        Config = null;
        ErrorMessage = errorMessage;
    }

    public static ConfigReadResult Success(StellarAnalyzerConfigFile config)
    {
        return new ConfigReadResult(config);
    }

    public static ConfigReadResult Failure(string errorMessage)
    {
        return new ConfigReadResult(errorMessage);
    }
}

internal static class MinimalJson
{
    public static string? GetNestedString(string json, string parentKey, string childKey)
    {
        var parentIndex = json.IndexOf($"\"{parentKey}\"", StringComparison.Ordinal);
        if (parentIndex < 0) return null;

        var colonIndex = json.IndexOf(':', parentIndex);
        if (colonIndex < 0) return null;

        var objStart = json.IndexOf('{', colonIndex);
        if (objStart < 0) return null;

        var objEnd = FindMatchingBrace(json, objStart);
        if (objEnd < 0) return null;

        // from here on only the inside of Project { ... } is looked at
        var span = json.Substring(objStart, objEnd - objStart + 1);

        return GetString(span, childKey);
    }

    public static string? GetString(string json, string key)
    {
        var keyIndex = json.IndexOf($"\"{key}\"", StringComparison.Ordinal);
        if (keyIndex < 0) return null;

        var colonIndex = json.IndexOf(':', keyIndex);
        if (colonIndex < 0) return null;

        var startQuote = json.IndexOf('"', colonIndex + 1);
        if (startQuote < 0) return null;

        var endQuote = FindStringEnd(json, startQuote + 1);
        if (endQuote < 0) return null;

        return json.Substring(startQuote + 1, endQuote - startQuote - 1);
    }

    private static int FindMatchingBrace(string json, int start)
    {
        var depth = 0;

        for (var i = start; i < json.Length; i++)
        {
            var c = json[i];

            if (c == '{')
            {
                depth++;
            }
            else if (c == '}')
            {
                depth--;
                if (depth == 0)
                    return i;
            }
            else if (c == '"')
            {
                i = FindStringEnd(json, i + 1);
                if (i < 0) return -1;
            }
        }

        return -1;
    }

    private static int FindStringEnd(string json, int start)
    {
        for (var i = start; i < json.Length; i++)
        {
            if (json[i] == '\\') // skip escaped char
            {
                i++;
                continue;
            }

            if (json[i] == '"')
                return i;
        }

        return -1;
    }
}

internal static class StellarProjectConfigReader
{
    public static ConfigReadResult TryRead(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return ConfigReadResult.Failure("Config file is empty.");

        try
        {
            var entryPoint = MinimalJson.GetNestedString(
                json,
                "Project",
                "StellarEntryPoint");

            var config = new StellarAnalyzerConfigFile
            {
                Project = entryPoint is not null
                    ? new StellarAnalyzerProjectConfig
                    {
                        StellarEntryPoint = entryPoint
                    }
                    : null
            };

            return ConfigReadResult.Success(config);
        }
        catch (Exception ex)
        {
            return ConfigReadResult.Failure($"Parse error: {ex.Message}");
        }
    }
}
