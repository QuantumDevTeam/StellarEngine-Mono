namespace Stellar.Sdk.Analyzers.Diagnostics;

/// <summary>
/// Central registry of all Stellar diagnostic IDs.
/// Convention: STLR + 4-digit category + sequential number.
/// 01xx EntryPoint, 02xx Modules (the architecture), 03xx Configuration files.
/// Every rule can be tuned in .editorconfig: dotnet_diagnostic.STLR0201.severity = none | suggestion | warning | error
/// </summary>
public static class StellarDiagnosticIds
{
    // ── EntryPoint ────────────────────────────────────────────────────────

    /// <summary>Type specified in config[Project][StellarEntryPoint] not found in compilation.</summary>
    public const string EntryPointTypeNotFound = "STLR0101";

    /// <summary>Configured entry-point type does not inherit StellarEntryPoint base class.</summary>
    public const string EntryPointMissingInheritance = "STLR0102";

    /// <summary>Configured entry-point type is not decorated with [StellarEntryPoint] attribute.</summary>
    public const string EntryPointMissingAttribute = "STLR0103";

    /// <summary>No entry-point types (with [StellarEntryPoint]) found anywhere in the module.</summary>
    public const string NoEntryPointInModule = "STLR0104";

    // ── Modules ───────────────────────────────────────────────────────────

    /// <summary>A referenced module sits on the same or a higher architectural level.</summary>
    public const string ModuleLevelViolation = "STLR0201";

    /// <summary>A module references another module its descriptor does not list.</summary>
    public const string UndeclaredModuleDependency = "STLR0202";

    /// <summary>The module descriptor names another module than the project builds.</summary>
    public const string ModuleDescriptorMismatch = "STLR0203";

    // ── Configuration ─────────────────────────────────────────────────────

    /// <summary>A .stellar.project or .stellar.module file is not well-formed JSON.</summary>
    public const string MalformedConfigurationFile = "STLR0301";
}
