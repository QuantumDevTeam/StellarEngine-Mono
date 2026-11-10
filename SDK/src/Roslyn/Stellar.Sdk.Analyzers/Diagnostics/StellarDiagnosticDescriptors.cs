using Microsoft.CodeAnalysis;

namespace Stellar.Sdk.Analyzers.Diagnostics;

internal static class StellarDiagnosticDescriptors
{
    private const string EntryPointCategory = "Stellar.EntryPoint";

    // ── STLR0101 ─────────────────────────────────────────────────────────

    internal static readonly DiagnosticDescriptor EntryPointTypeNotFound = new(
        StellarDiagnosticIds.EntryPointTypeNotFound,
        "Entry-point type not found",
        "Type '{0}' specified as StellarEntryPoint in project config was not found",
        EntryPointCategory,
        DiagnosticSeverity.Error,
        true,
        customTags: WellKnownDiagnosticTags.CompilationEnd,
        description:
        "The fully-qualified type name written in .stellar.project[Project][StellarEntryPoint] " +
        "must exist in the current module. " +
        "If you want the default convention ({AssemblyName}.EntryPoint), use the value \"default\"."
    );

    // ── STLR0102 ─────────────────────────────────────────────────────────

    internal static readonly DiagnosticDescriptor EntryPointMissingInheritance = new(
        StellarDiagnosticIds.EntryPointMissingInheritance,
        "Entry-point must inherit StellarEntryPoint",
        "Type '{0}' is configured as the module entry point but does not inherit " +
        "'Stellar.Kernel.EntryPoint.StellarEntryPoint'",
        EntryPointCategory,
        DiagnosticSeverity.Error,
        true,
        customTags: WellKnownDiagnosticTags.CompilationEnd,
        description:
        "An entry-point class must extend StellarEntryPoint so the engine can manage its lifecycle."
    );

    // ── STLR0103 ─────────────────────────────────────────────────────────

    internal static readonly DiagnosticDescriptor EntryPointMissingAttribute = new(
        StellarDiagnosticIds.EntryPointMissingAttribute,
        "Entry-point missing [StellarEntryPoint] attribute",
        "Type '{0}' is configured as the module entry point but is not decorated " +
        "with [StellarEntryPoint]",
        EntryPointCategory,
        DiagnosticSeverity.Error,
        true,
        customTags: WellKnownDiagnosticTags.CompilationEnd,
        description:
        "Apply [StellarEntryPoint] to let the engine discover this class during assembly scanning."
    );

    // ── STLR0104 ─────────────────────────────────────────────────────────

    internal static readonly DiagnosticDescriptor NoEntryPointInModule = new(
        StellarDiagnosticIds.NoEntryPointInModule,
        "No entry point defined in module",
        "Module '{0}' has no type decorated with [StellarEntryPoint]",
        EntryPointCategory,
        DiagnosticSeverity.Warning,
        true,
        customTags: WellKnownDiagnosticTags.CompilationEnd,
        description:
        "Every Stellar module must declare exactly one entry-point — a class that inherits " +
        "StellarEntryPoint and is marked with [StellarEntryPoint]."
    );

    private const string ModulesCategory = "Stellar.Modules";
    private const string ConfigurationCategory = "Stellar.Configuration";

    // ── STLR0201 ─────────────────────────────────────────────────────────

    internal static readonly DiagnosticDescriptor ModuleLevelViolation = new(
        StellarDiagnosticIds.ModuleLevelViolation,
        "A module depends on a module of the same or a higher level",
        "'{0}' ({1}) must not depend on '{2}' ({3}): a module depends only on strictly lower levels",
        ModulesCategory,
        DiagnosticSeverity.Error,
        true,
        customTags: WellKnownDiagnosticTags.CompilationEnd,
        description:
        "The levels of the engine are Kernel, Core, CoreModule, Runtime, HighLevel and EndPoint. A module may depend " +
        "only on lower levels; the runtime may reach up, and end-user code may build on end-user code. " +
        "See Docs/Architecture/Core.md."
    );

    // ── STLR0202 ─────────────────────────────────────────────────────────

    internal static readonly DiagnosticDescriptor UndeclaredModuleDependency = new(
        StellarDiagnosticIds.UndeclaredModuleDependency,
        "A module uses a module its descriptor does not list",
        "'{0}' references the module '{1}', which is not in the Dependencies of {0}.stellar.module",
        ModulesCategory,
        DiagnosticSeverity.Warning,
        true,
        customTags: WellKnownDiagnosticTags.CompilationEnd,
        description:
        "The descriptor is what the installation, the load order and the runtime know about a module. A reference " +
        "added by hand works for the build, but the engine does not know about it: list the module in Dependencies."
    );

    // ── STLR0203 ─────────────────────────────────────────────────────────

    internal static readonly DiagnosticDescriptor ModuleDescriptorMismatch = new(
        StellarDiagnosticIds.ModuleDescriptorMismatch,
        "The module descriptor describes another module",
        "The descriptor says '{0}', the project builds '{1}'",
        ModulesCategory,
        DiagnosticSeverity.Warning,
        true,
        customTags: WellKnownDiagnosticTags.CompilationEnd,
        description:
        "The Id of a .stellar.module file is the assembly and the package name of the module. Set StellarModuleId " +
        "in the project if the project is named otherwise."
    );

    // ── STLR0301 ─────────────────────────────────────────────────────────

    internal static readonly DiagnosticDescriptor MalformedConfigurationFile = new(
        StellarDiagnosticIds.MalformedConfigurationFile,
        "A Stellar configuration file is not well-formed JSON",
        "{0} is not well-formed JSON: {1}",
        ConfigurationCategory,
        DiagnosticSeverity.Error,
        true,
        "The .stellar.project and .stellar.module files are JSON. Their schemas are in Shared/Schemas of the installation."
    );
}
