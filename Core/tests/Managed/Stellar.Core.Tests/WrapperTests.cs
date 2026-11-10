using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Stellar.Core.Data.Registry;
using Stellar.Core.Modules;
using Stellar.Core.Native;
using Stellar.Kernel.Interop;
using Stellar.Kernel.Versioning;

namespace Stellar.Core.Tests;

/// <summary>
/// What the SDK built this test assembly with.
/// </summary>
internal static class Build
{
    private static string Metadata(string key)
    {
        return typeof(Build).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                   .Single(attribute => attribute.Key == key).Value
               ?? string.Empty;
    }

    public static string Wrapper => Metadata("StellarNativeWrapper");

    public static string EngineCoreDir => Metadata("StellarEngineCoreDir");

    public static string Configuration => Metadata("StellarTestConfiguration");
}

/// <summary>
/// A test that needs the native libraries. It is skipped, visibly, when they were not built.
/// </summary>
public sealed class NativeFactAttribute : FactAttribute
{
    public NativeFactAttribute()
    {
        if (!NativeCore.IsAvailable)
            Skip = "The native core is not built, run scripts/build.ps1";
    }
}

/// <summary>
/// A test that reads both wrappers. It is skipped, visibly, until both are built.
/// </summary>
public sealed class BothWrappersFactAttribute : FactAttribute
{
    public BothWrappersFactAttribute()
    {
        if (!File.Exists(WrapperParityTests.CliPath) || !File.Exists(WrapperParityTests.ManagedPath))
            Skip = $"Both wrappers must be built: {WrapperParityTests.CliPath}, {WrapperParityTests.ManagedPath}";
    }
}

public class ContractTests
{
    [Theory]
    [InlineData(1u, 0u, 1u, 0u, true)]
    [InlineData(1u, 2u, 1u, 1u, true)]
    [InlineData(1u, 1u, 1u, 2u, false)]
    [InlineData(2u, 0u, 1u, 0u, false)]
    [InlineData(1u, 0u, 2u, 0u, false)]
    public void Satisfies_FollowsTheVersionRule(uint major, uint minor, uint requiredMajor, uint requiredMinor,
        bool expected)
    {
        new ContractVersion(major, minor).Satisfies(new ContractVersion(requiredMajor, requiredMinor)).Should()
            .Be(expected);
    }

    [Fact]
    public void AbiTypes_KeepTheirLayout()
    {
        Unsafe.SizeOf<ContractVersion>().Should().Be(8);
        Unsafe.SizeOf<NativeHandle>().Should().Be(8);
        Unsafe.SizeOf<Guid>().Should().Be(16);
    }

    [Fact]
    public void Handle_WithZeroGeneration_IsInvalid()
    {
        NativeHandle.Invalid.IsValid.Should().BeFalse();
        new NativeHandle(0, 1).IsValid.Should().BeTrue();
    }
}

/// <summary>
/// The behaviour every wrapper must have. The suite runs once per wrapper: <c>-p:StellarNativeWrapper=...</c>.
/// </summary>
public class WrapperTests
{
    [NativeFact]
    public void Wrapper_IsTheOneTheSdkChose()
    {
        NativeCore.Wrapper.Should().Be(Build.Wrapper);
    }

    [NativeFact]
    public void Identifier_LooksTheSameOnBothSides()
    {
        // The real proof of the layout: System.Guid and the native identifier agree on bytes and on text
        var managed = Guid.Parse("00112233-4455-6677-8899-aabbccddeeff");

        new Identifier(managed).ToCanonicalString().Should().Be(managed.ToString());
        Identifier.Parse(managed.ToString())!.UID.Should().Be(managed);

        var native = Identifier.Create();

        native.UID.Should().NotBe(Guid.Empty);
        native.ToCanonicalString().Should().Be(native.UID.ToString());
    }

    [NativeFact]
    public void Parse_RejectsMalformedText()
    {
        Identifier.Parse("not a guid").Should().BeNull();
        Identifier.Parse(string.Empty).Should().BeNull();
    }

    [NativeFact]
    public void IdentifierRegistry_LivesInTheNativeCore()
    {
        var registry = CoreContext.Current.Identifiers;
        var id = new Identifier(Guid.NewGuid());
        var size = registry.Size;

        registry.Register(id).Should().BeTrue();
        registry.Register(id).Should().BeFalse();
        registry.Exists(id).Should().BeTrue();
        registry.Size.Should().Be(size + 1);

        registry.Pop(id).Should().Be(id);
        registry.Pop(id).Should().BeNull();
        id.IsRegistered.Should().BeFalse();
    }

    [NativeFact]
    public void Labels_SupportUnicode()
    {
        var name = $"Метка/{Guid.NewGuid()}";
        var label = Label.Create(name)!;

        Label.Get(name).Should().Be(label);
        Label.Get(name)!.Name.Should().Be(name);

        label.Dispose();
        Label.Get(name).Should().BeNull();
    }

    [NativeFact]
    public void Containers_KeepMembersAndBranches()
    {
        var registry = CoreContext.Current.Containers;
        var container = Guid.NewGuid();
        var item = Guid.NewGuid();
        var branch = Guid.NewGuid();

        // A branch is a container too: the one of a sub-roster
        registry.Create(branch).Should().BeTrue();
        registry.Create(container).Should().BeTrue();
        registry.Create(container).Should().BeFalse();

        registry.Add(container, item).Should().BeTrue();
        registry.Add(container, item).Should().BeFalse();
        registry.Contains(container, item).Should().BeTrue();
        registry.Items(container).Should().Equal(item);

        registry.AddBranch(container, branch).Should().BeTrue();
        registry.ContainsBranch(container, branch).Should().BeTrue();
        registry.Branches(container).Should().Equal(branch);
        registry.RemoveBranch(container, branch).Should().BeTrue();

        registry.Destroy(container).Should().BeTrue();
        registry.Destroy(branch).Should().BeTrue();
        registry.Exists(container).Should().BeFalse();
    }

    [NativeFact]
    public void SealedContainer_RejectsChanges()
    {
        var registry = CoreContext.Current.Containers;
        var container = Guid.NewGuid();
        var item = Guid.NewGuid();

        registry.Create(container);
        registry.Add(container, item);
        registry.Seal(container);

        registry.IsSealed(container).Should().BeTrue();
        registry.Add(container, Guid.NewGuid()).Should().BeFalse();
        registry.Remove(container, item).Should().BeFalse();
        registry.Count(container).Should().Be(1);

        registry.Destroy(container);
    }

    [NativeFact]
    public void UnknownContainer_IsAFailure()
    {
        var count = () => CoreContext.Current.Containers.Count(Guid.NewGuid());

        count.Should().Throw<NativeCoreException>().Where(exception => exception.Result == NativeResult.NotFound);
    }

    [NativeFact]
    public void Failures_ComeBackAsExceptions()
    {
        var load = () => CoreContext.Current.Modules.Load("no-such-module.dll");

        load.Should().Throw<NativeCoreException>()
            .Where(exception => exception.Result == NativeResult.NotFound)
            .WithMessage("*no-such-module.dll*");
    }

    [NativeFact]
    public void StaleModuleHandle_IsRejected()
    {
        var stale = new NativeHandle(1000, 1);
        var getName = () => CoreContext.Current.Modules.GetName(stale);

        CoreContext.Current.Modules.Unload(stale).Should().BeFalse();
        getName.Should().Throw<NativeCoreException>()
            .Where(exception => exception.Result == NativeResult.InvalidHandle);
    }
}

/// <summary>
/// Engine code never names a wrapper, only the classes: both wrappers must define the same ones, with the same
/// members. The assemblies are read as metadata, so the check needs neither of them to be loadable here.
/// </summary>
public class WrapperParityTests
{
    internal static string CliPath => Path.Combine(Build.EngineCoreDir, "artifacts", "bin",
        "Stellar.Native.Core.Wrapper.Cli", Build.Configuration, "Stellar.Native.Core.Wrapper.Cli.dll");

    internal static string ManagedPath => Path.Combine(Build.EngineCoreDir, "artifacts", "bin",
        "Stellar.Native.Core.Wrapper.Managed", Build.Configuration, "Stellar.Native.Core.Wrapper.Managed.dll");

    [BothWrappersFact]
    public void Wrappers_DefineTheSameApi()
    {
        var cli = Surface(CliPath);
        var managed = Surface(ManagedPath);

        cli.Except(managed).Should().BeEmpty("the C++/CLI wrapper must not have what the Managed one lacks");
        managed.Except(cli).Should().BeEmpty("the Managed wrapper must not have what the C++/CLI one lacks");
    }

    private static SortedSet<string> Surface(string path)
    {
        var runtime = Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll");
        var kernel = typeof(NativeHandle).Assembly.Location;
        var resolver = new PathAssemblyResolver(runtime.Append(kernel).Append(path));

        using var context = new MetadataLoadContext(resolver);
        var assembly = context.LoadFromAssemblyPath(path);
        var surface = new SortedSet<string>(StringComparer.Ordinal);

        const BindingFlags members = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static |
                                     BindingFlags.DeclaredOnly;

        foreach (var type in assembly.GetExportedTypes())
        {
            surface.Add($"type {type.FullName}");

            foreach (var member in type.GetMembers(members))
            {
                var signature = member switch
                {
                    MethodBase { IsSpecialName: true } method when !method.IsConstructor &&
                                                                   !method.Name.StartsWith("op_") => null,
                    MethodBase method =>
                        $"{Name(method)}({string.Join(", ", method.GetParameters().Select(Parameter))})"
                        + (method is MethodInfo info ? $" : {TypeName(info.ReturnType)}" : string.Empty),
                    PropertyInfo property => $"property {property.Name} : {TypeName(property.PropertyType)}"
                                             + (property.CanWrite && property.SetMethod!.IsPublic
                                                 ? " { set }"
                                                 : string.Empty),
                    FieldInfo field => $"field {field.Name} : {TypeName(field.FieldType)}",
                    _ => $"{member.MemberType} {member.Name}"
                };

                if (signature is not null)
                    surface.Add($"{type.FullName}::{signature}");
            }
        }

        return surface;

        static string TypeName(Type type)
        {
            return type switch
            {
                { IsByRef: true } => $"{TypeName(type.GetElementType()!)}&",
                { IsArray: true } => $"{TypeName(type.GetElementType()!)}[]",
                { IsGenericType: true } =>
                    $"{type.GetGenericTypeDefinition().FullName}<{string.Join(", ", type.GetGenericArguments().Select(TypeName))}>",
                _ => type.FullName ?? type.Name
            };
        }

        static string Name(MethodBase method)
        {
            return (method.IsStatic ? "static " : string.Empty) + method.Name;
        }

        static string Parameter(ParameterInfo parameter)
        {
            return
                $"{TypeName(parameter.ParameterType)} {parameter.Name}{(parameter.IsOptional ? " = optional" : string.Empty)}";
        }
    }
}
