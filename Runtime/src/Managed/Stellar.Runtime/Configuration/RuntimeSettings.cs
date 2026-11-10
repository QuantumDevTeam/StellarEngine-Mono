using System.Globalization;
using Stellar.Kernel.Configuration;

namespace Stellar.Runtime.Configuration;

/// <summary>
/// The settings of the runtime: the <c>Runtime</c> section of the <c>.stellar.project</c> file, then the command line
/// (<c>--stellar:Name=Value</c>), which wins.
/// </summary>
public sealed class RuntimeSettings
{
    /// <summary>
    /// The prefix of the command line arguments that set a setting.
    /// </summary>
    public const string CommandLinePrefix = "--stellar:";

    private readonly Dictionary<string, string> _values;

    private RuntimeSettings(Dictionary<string, string> values)
    {
        _values = values;
    }

    /// <summary>
    /// Gets whether the main loop runs. <c>false</c> stops the engine as soon as the application is started.
    /// </summary>
    public bool MainLoop => GetBoolean(nameof(MainLoop), true);

    /// <summary>
    /// Gets the frames per second the main loop aims at. 0 means as fast as possible.
    /// </summary>
    public double TargetFrameRate => GetDouble(nameof(TargetFrameRate), 0);

    /// <summary>
    /// Gets the longest time step a frame reports, in seconds: a long pause does not become a huge step.
    /// </summary>
    public double MaxDeltaTime => GetDouble(nameof(MaxDeltaTime), 0.25);

    /// <summary>
    /// Gets the number of frames after which the engine stops. 0 means no limit.
    /// </summary>
    public ulong MaxFrames => (ulong)GetDouble(nameof(MaxFrames), 0);

    /// <summary>
    /// Gets the fixed step of the engine clock, in seconds: the step of the fixed updates. 0 makes a fixed update
    /// once per frame, with the step of the frame.
    /// </summary>
    public double FixedTimeStep => GetDouble(nameof(FixedTimeStep), 1.0 / 60);

    /// <summary>
    /// Gets the most fixed steps a participant runs in one frame.
    /// </summary>
    public int MaxFixedSteps => Math.Max(1, (int)GetDouble(nameof(MaxFixedSteps), 8));

    /// <summary>
    /// Gets whether modules are loaded into contexts that can be unloaded.
    /// </summary>
    public bool CollectibleModules => GetBoolean(nameof(CollectibleModules), true);

    /// <summary>
    /// Gets how many modules may be initialized at the same time, among those whose dependencies are ready. 1 keeps
    /// every initialization on the main thread, in order.
    /// </summary>
    public int ModuleInitializationParallelism =>
        Math.Max(1, (int)GetDouble(nameof(ModuleInitializationParallelism), 1));

    /// <summary>
    /// Gets whether the runtime tells what it does on the error output.
    /// </summary>
    public bool Verbose => GetBoolean(nameof(Verbose), false);

    /// <summary>
    /// Creates the settings of an application.
    /// </summary>
    /// <param name="configuration">The runtime configuration.</param>
    /// <param name="arguments">The command line.</param>
    /// <returns>The settings.</returns>
    public static RuntimeSettings Create(RuntimeConfiguration? configuration, IEnumerable<string>? arguments = null)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (configuration is not null)
            foreach (var pair in configuration.Settings)
                values[pair.Key] = pair.Value;

        foreach (var argument in arguments ?? [])
        {
            if (!argument.StartsWith(CommandLinePrefix, StringComparison.OrdinalIgnoreCase)) continue;

            var setting = argument[CommandLinePrefix.Length..];
            var separator = setting.IndexOf('=');

            if (separator > 0)
                values[setting[..separator]] = setting[(separator + 1)..];
            else if (setting.Length > 0)
                values[setting] = "true";
        }

        return new RuntimeSettings(values);
    }

    /// <summary>
    /// Gets a setting, for settings of modules the runtime itself does not know.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns>The value, or <c>null</c>.</returns>
    public string? Get(string name)
    {
        return _values.GetValueOrDefault(name);
    }

    private bool GetBoolean(string name, bool fallback)
    {
        return _values.TryGetValue(name, out var value) && bool.TryParse(value, out var result) ? result : fallback;
    }

    private double GetDouble(string name, double fallback)
    {
        return _values.TryGetValue(name, out var value) &&
               double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result)
            ? result
            : fallback;
    }
}
