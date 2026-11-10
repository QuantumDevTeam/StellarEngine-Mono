using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Stellar.Tools.Building;

/// <summary>
/// Finds the MSBuild of Visual Studio: the .NET SDK cannot build C++ projects.
/// </summary>
public static class MSBuildLocator
{
    /// <summary>
    /// The environment variable that names an MSBuild.exe explicitly.
    /// </summary>
    public const string EnvironmentVariable = "STELLAR_MSBUILD";

    /// <summary>
    /// Finds MSBuild.exe: <see cref="EnvironmentVariable"/>, then vswhere.
    /// </summary>
    /// <returns>The path, or <c>null</c> if Visual Studio with MSBuild is not installed.</returns>
    public static string? Find()
    {
        var explicitPath = System.Environment.GetEnvironmentVariable(EnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(explicitPath) && File.Exists(explicitPath))
            return explicitPath;

        var programFiles = System.Environment.GetEnvironmentVariable("ProgramFiles(x86)");
        if (string.IsNullOrEmpty(programFiles))
            return null;

        var vswhere = Path.Combine(programFiles, "Microsoft Visual Studio", "Installer", "vswhere.exe");
        if (!File.Exists(vswhere))
            return null;

        var start = new ProcessStartInfo(vswhere,
            "-latest -products * -requires Microsoft.Component.MSBuild -find MSBuild\\**\\Bin\\MSBuild.exe")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(start);
        if (process is null)
            return null;

        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        return output.Split('\r', '\n').Select(line => line.Trim())
            .FirstOrDefault(line => line.Length != 0 && File.Exists(line));
    }
}
