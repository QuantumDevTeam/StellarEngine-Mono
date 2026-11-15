using System.Text.Json;
using System.Text.RegularExpressions;

namespace Stellar.Tools.Tests;

/// <summary>
/// Facts of the checkout that live in two places and must not drift apart.
/// </summary>
public class RepositoryTests
{
    private static string Root()
    {
        // The tests run from artifacts/bin/... of the checkout
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null;
             directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "Stellar.Sdk.InRepo.props")))
                return directory.FullName;

        throw new InvalidOperationException($"No checkout of the core above {AppContext.BaseDirectory}");
    }

    [Fact]
    public void GlobalJson_PinsTheStellarSdkOfThisCheckout()
    {
        // An IDE that loads StellarEngine.sln resolves Stellar.Sdk with the global.json of the solution folder,
        // the root: without this version the modules and games in it do not load
        var root = Root();
        var versions = File.ReadAllText(Path.Combine(root, "Shared", "Build", "Stellar.Versions.props"));
        var sdkVersion = Regex.Match(versions, "<StellarSdkVersion>([^<]+)</StellarSdkVersion>").Groups[1].Value;

        using var globalJson = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "global.json")));
        var pinned = globalJson.RootElement.GetProperty("msbuild-sdks").GetProperty("Stellar.Sdk").GetString();

        sdkVersion.Should().NotBeEmpty();
        pinned.Should().Be(sdkVersion, "global.json must follow StellarSdkVersion of Shared/Build/Stellar.Versions.props");
    }
}
