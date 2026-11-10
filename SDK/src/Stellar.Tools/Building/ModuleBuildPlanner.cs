using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Stellar.Tools.Diagnostics;
using Stellar.Tools.Environment;
using Stellar.Tools.Modules;

namespace Stellar.Tools.Building;

/// <summary>
/// What to build.
/// </summary>
/// <param name="Configuration">Debug or Release.</param>
/// <param name="Native">Whether to build the native projects.</param>
/// <param name="Managed">Whether to build the managed projects.</param>
/// <param name="Tests">Whether to run the managed tests.</param>
/// <param name="Pack">Whether to put the packages into the local feed.</param>
/// <param name="NativeWrapper">The wrapper of the native core, or <c>null</c> for the default of the SDK.</param>
public sealed record ModuleBuildOptions(
    string Configuration = "Debug",
    bool Native = true,
    bool Managed = true,
    bool Tests = false,
    bool Pack = false,
    string? NativeWrapper = null);

/// <summary>
/// One program to run.
/// </summary>
/// <param name="Module">The module the step belongs to.</param>
/// <param name="Description">What the step does, for the output.</param>
/// <param name="FileName">The program.</param>
/// <param name="Arguments">Its arguments.</param>
public sealed record BuildStep(string Module, string Description, string FileName, IReadOnlyList<string> Arguments);

/// <summary>
/// Turns modules into the steps that build them: for each module in load order, its native projects (MSBuild of
/// Visual Studio), its managed project, its tests and its package. The dependencies of the selected modules come
/// first, since a module is built against what they produced.
/// </summary>
public static class ModuleBuildPlanner
{
    /// <summary>
    /// Plans a build.
    /// </summary>
    /// <param name="installation">The installation.</param>
    /// <param name="set">Its modules.</param>
    /// <param name="selected">The modules to build. All of them when empty.</param>
    /// <param name="options">What to build.</param>
    /// <param name="msbuild">The MSBuild of Visual Studio, needed for native projects.</param>
    /// <returns>The steps, in order.</returns>
    /// <exception cref="ToolException">Thrown if a module is unknown, or native projects need an MSBuild there is not.</exception>
    public static IReadOnlyList<BuildStep> Plan(
        StellarInstallation installation,
        ModuleSet set,
        IReadOnlyCollection<string> selected,
        ModuleBuildOptions options,
        string? msbuild)
    {
        var byId = set.LoadOrder.ToDictionary(module => module.Id, StringComparer.Ordinal);
        var wanted = new HashSet<string>(StringComparer.Ordinal);

        foreach (var id in selected.Count == 0 ? byId.Keys : selected)
        {
            if (!byId.ContainsKey(id))
                throw new ToolException($"Unknown module {id}. Known: {string.Join(", ", byId.Keys)}");

            AddWithDependencies(id);
        }

        var steps = new List<BuildStep>();

        // Modules outside the root find it this way. Forward slashes: a trailing backslash would escape the quote
        var root = $"-p:StellarRoot={installation.Root.Replace('\\', '/').TrimEnd('/')}/";
        var properties = new List<string> { $"-p:Configuration={options.Configuration}", root };
        if (options.NativeWrapper is { Length: > 0 } wrapper)
            properties.Add($"-p:StellarNativeWrapper={wrapper}");

        foreach (var module in
                 set.LoadOrder.Where(module => wanted.Contains(module.Id) && module.Directory is not null))
        {
            if (options.Native)
                foreach (var project in module.NativeProjectPaths)
                {
                    if (msbuild is null)
                        throw new ToolException(
                            $"{module.Id} has native projects, and the MSBuild of Visual Studio was not found. " +
                            $"Install Visual Studio with the C++ workload, set {MSBuildLocator.EnvironmentVariable}, or build with --no-native.");

                    steps.Add(new BuildStep(module.Id, $"native {Path.GetFileNameWithoutExtension(project)}", msbuild,
                        new[]
                        {
                            project, "-restore", $"-p:Configuration={options.Configuration}", "-p:Platform=x64", root,
                            "-m", "-nologo", "-v:minimal"
                        }));
                }

            if (options.Managed && module.ProjectPath is { } managed)
                steps.Add(new BuildStep(module.Id, $"build {Path.GetFileNameWithoutExtension(managed)}", "dotnet",
                    new[] { "build", managed, "-v", "minimal", "--nologo" }.Concat(properties).ToArray()));

            if (options.Tests)
                foreach (var tests in module.TestProjectPaths.Where(path =>
                             path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)))
                    steps.Add(new BuildStep(module.Id, $"test {Path.GetFileNameWithoutExtension(tests)}", "dotnet",
                        new[] { "test", tests, "--nologo" }.Concat(properties).ToArray()));

            if (options.Pack && module.ProjectPath is { } packed)
                steps.Add(new BuildStep(module.Id, $"pack {Path.GetFileNameWithoutExtension(packed)}", "dotnet",
                    new[] { "pack", packed, "-o", installation.Packages, "-v", "quiet", "--nologo" }.Concat(properties)
                        .ToArray()));
        }

        return steps;

        void AddWithDependencies(string id)
        {
            if (!wanted.Add(id))
                return;

            foreach (var dependency in byId[id].Descriptor.Dependencies.Where(byId.ContainsKey))
                AddWithDependencies(dependency);
        }
    }
}

/// <summary>
/// Runs a build plan, stopping at the first failure.
/// </summary>
public sealed class ModuleBuilder
{
    private readonly IProcessRunner _runner;
    private readonly IToolLogger _logger;

    /// <summary>
    /// Creates a builder.
    /// </summary>
    public ModuleBuilder(IProcessRunner runner, IToolLogger logger)
    {
        _runner = runner;
        _logger = logger;
    }

    /// <summary>
    /// Runs the steps.
    /// </summary>
    /// <param name="steps">The plan.</param>
    /// <param name="workingDirectory">Where the programs run.</param>
    /// <exception cref="ToolException">Thrown at the first step that fails.</exception>
    public void Run(IReadOnlyList<BuildStep> steps, string workingDirectory)
    {
        for (var index = 0; index < steps.Count; index++)
        {
            var step = steps[index];
            _logger.Info($"[{index + 1}/{steps.Count}] {step.Module}: {step.Description}");

            var exitCode = _runner.Run(step.FileName, step.Arguments, workingDirectory);
            if (exitCode != 0)
                throw new ToolException($"{step.Module}: {step.Description} failed with exit code {exitCode}");
        }

        _logger.Info(steps.Count == 0
            ? "Nothing to build"
            : $"Built: {steps.Select(step => step.Module).Distinct().Count()} module(s)");
    }
}
