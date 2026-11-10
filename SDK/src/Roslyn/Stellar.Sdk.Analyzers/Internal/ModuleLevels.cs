using System;

namespace Stellar.Sdk.Analyzers.Internal;

/// <summary>
/// The architectural levels, as the SDK writes them (Stellar.Kernel.Modules.ModuleLevel). The analyzers do not
/// reference the Kernel: they run inside the compiler.
/// </summary>
internal static class ModuleLevels
{
    public const int Kernel = 0;
    public const int Core = 1;
    public const int CoreModule = 2;
    public const int Runtime = 3;
    public const int HighLevel = 4;
    public const int EndPoint = 5;

    public static int? Parse(string? name)
    {
        return name switch
        {
            "Kernel" => Kernel,
            "Core" => Core,
            "CoreModule" => CoreModule,
            "Runtime" => Runtime,
            "HighLevel" => HighLevel,
            "EndPoint" => EndPoint,
            _ => null
        };
    }

    /// <summary>
    /// The level of a project that is not a module of the catalog, from what it says it is.
    /// </summary>
    public static int? FromUsage(string? usage)
    {
        return usage switch
        {
            "EngineCore" => Core,
            "EngineCoreModule" => CoreModule,
            "EngineModule" => HighLevel,
            "Module" or "ExecutableModule" => EndPoint,
            _ => null
        };
    }

    /// <summary>
    /// The rule of the architecture: a module depends only on strictly lower levels. The runtime drives the modules
    /// above it, and the end user may build on other end-user code.
    /// </summary>
    public static bool MayDependOn(int level, int dependency)
    {
        return level == Runtime || dependency < level || (level == EndPoint && dependency == EndPoint);
    }

    public static string Name(int level)
    {
        return level switch
        {
            Kernel => "Kernel",
            Core => "Core",
            CoreModule => "CoreModule",
            Runtime => "Runtime",
            HighLevel => "HighLevel",
            EndPoint => "EndPoint",
            _ => level.ToString()
        };
    }

    public static bool IsCore(string id)
    {
        return string.Equals(id, "Stellar.Kernel", StringComparison.Ordinal) ||
               string.Equals(id, "Stellar.Core", StringComparison.Ordinal);
    }
}
