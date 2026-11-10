using Stellar.Core.Native;
using Stellar.Core.Data.Registry;
using Stellar.Kernel;
using Stellar.Kernel.Quantization;

namespace Stellar.Core.Tests;

public class IdentifierTests
{
    private sealed class ForeignIdentifier(Guid uid) : IIdentifier
    {
        public Guid UID { get; } = uid;

        public void Register(IQuantumObject registry)
        {
        }

        public void Unregister(IQuantumObject registry)
        {
        }

        public void Dispose()
        {
        }
    }

    [Fact]
    public void Identifiers_WithSameUid_AreEqual()
    {
        var uid = Guid.NewGuid();

        new Identifier(uid).Should().Be(new Identifier(uid));
        new Identifier(uid).GetHashCode().Should().Be(new Identifier(uid).GetHashCode());
        new Identifier(uid).Equals(new ForeignIdentifier(uid)).Should().BeTrue();
    }

    [Fact]
    public void Identifiers_WithDifferentUid_AreNotEqual()
    {
        Identifier.Create().Should().NotBe(Identifier.Create());
    }

    [Fact]
    public void CreateAndRegister_PutsIdentifierIntoRegistry()
    {
        // Regression: the registration used to be a silent no-op
        var id = Identifier.CreateAndRegister();

        CoreContext.Current.Identifiers.Exists(id).Should().BeTrue();
        Identifier.Get(id.UID).Should().Be(id);
        Identifier.Get(id.UID.ToString()).Should().Be(id);
        Identifier.Get(id.UID.ToByteArray()).Should().Be(id);
    }

    [Fact]
    public void Dispose_RemovesIdentifierFromRegistry()
    {
        var id = Identifier.CreateAndRegister();

        id.Dispose();

        CoreContext.Current.Identifiers.Exists(id).Should().BeFalse();
    }

    [Fact]
    public void Register_InWrongRegistry_Throws()
    {
        var action = () => Identifier.Create().Register(CoreContext.Current.Labels);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Registry_FindsIdentifier_ByForeignImplementationWithSameUid()
    {
        var id = Identifier.CreateAndRegister();

        CoreContext.Current.Identifiers.Get(new ForeignIdentifier(id.UID)).Should().Be(id);
    }
}

public class LabelTests
{
    private static string UniqueName()
    {
        return $"Test/{Guid.NewGuid()}";
    }

    [Fact]
    public void Create_RegistersLabel()
    {
        var name = UniqueName();

        var label = Label.Create(name);

        label.Should().NotBeNull();
        Label.Get(name).Should().Be(label);
        Label.Get(label!.UID).Should().Be(label);
    }

    [Fact]
    public void Create_WithTakenName_ReturnsNull()
    {
        var name = UniqueName();
        Label.Create(name);

        Label.Create(name).Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankName_ReturnsNull(string name)
    {
        Label.Create(name).Should().BeNull();
    }

    [Fact]
    public void Create_WithoutRegistration_LeavesRegistryAlone()
    {
        var name = UniqueName();

        Label.Create(name, false).Should().NotBeNull();

        Label.Get(name).Should().BeNull();
    }

    [Fact]
    public void ImplicitConversion_ReusesLabel()
    {
        var name = UniqueName();

        Label? first = name;
        Label? second = name;

        second.Should().Be(first);
    }

    [Fact]
    public void Dispose_FreesTheName()
    {
        // Regression: the registration used to be a silent no-op, so the name was never released
        var name = UniqueName();
        var label = Label.Create(name)!;

        label.Dispose();

        Label.Get(name).Should().BeNull();
        Label.Create(name).Should().NotBeNull();
    }
}
