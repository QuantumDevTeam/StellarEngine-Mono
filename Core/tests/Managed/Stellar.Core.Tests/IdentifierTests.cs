using System.Runtime.CompilerServices;
using Stellar.Kernel;

namespace Stellar.Core.Tests;

public class IdentifierTests
{
    [Fact]
    public void NewIdentifiers_AreDifferent_AndNotEmpty()
    {
        var first = Identifier.New();
        var second = Identifier.New();

        first.Should().NotBe(second);
        first.IsEmpty.Should().BeFalse();
        default(Identifier).Should().Be(Identifier.Empty);
        Identifier.Empty.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void NewIdentifiers_AreVersion4Uuids_DifferentOnEveryThread()
    {
        var made = new System.Collections.Concurrent.ConcurrentBag<Identifier>();

        Parallel.For(0, 8, _ =>
        {
            for (var i = 0; i < 5000; i++) made.Add(Identifier.New());
        });

        made.Distinct().Should().HaveCount(40_000);
        made.Should().AllSatisfy(id =>
        {
            var text = id.ToString();
            text[14].Should().Be('4');
            "89ab".Should().Contain(text[19].ToString());
        });
    }

    [Fact]
    public void TheTextForm_GoesBackAndForth()
    {
        var id = Identifier.New();
        var text = id.ToString();

        text.Should().HaveLength(36).And.Be(text.ToLowerInvariant());
        Identifier.Parse(text).Should().Be(id);
        Identifier.Parse(text.ToUpperInvariant()).Should().Be(id);
        id.GetHashCode().Should().Be(Identifier.Parse(text).GetHashCode());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not an identifier")]
    [InlineData("{00112233-4455-6677-8899-aabbccddeeff}")]
    [InlineData("00112233445566778899aabbccddeeff")]
    public void TryParse_RefusesOtherForms(string? text)
    {
        Identifier.TryParse(text, out var id).Should().BeFalse();
        id.Should().Be(Identifier.Empty);
    }

    [Fact]
    public void TheBytes_GoBackAndForth()
    {
        var id = Identifier.New();

        Identifier.FromBytes(id.ToBytes()).Should().Be(id);

        Span<byte> bytes = stackalloc byte[Identifier.Size];
        id.TryWriteBytes(bytes).Should().BeTrue();
        Identifier.FromBytes(bytes).Should().Be(id);
        FluentActions.Invoking(() => Identifier.FromBytes(new byte[3])).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AnIdentifier_IsSixteenBlittableBytes()
    {
        // It crosses into native code as it is, the StellarUid of the native core
        Unsafe.SizeOf<Identifier>().Should().Be(16);
        RuntimeHelpers.IsReferenceOrContainsReferences<Identifier>().Should().BeFalse();
    }
}

public class LabelTests
{
    [Fact]
    public void ALabel_IsItsIdentifierAndItsName()
    {
        var id = Identifier.New();

        new Label(id, "Player").Should().Be(new Label(id, "Player"));
        new Label(id, "Player").Should().NotBe(new Label(id, "Enemy"));
        new Label(Identifier.New(), "Player").Should().NotBe(new Label(id, "Player"));
        new Label(id, "Player").GetHashCode().Should().Be(new Label(id, "Player").GetHashCode());
    }

    [Fact]
    public void New_GivesANewIdentifier()
    {
        Label.New("Player").UID.Should().NotBe(Label.New("Player").UID);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ABlankName_IsRefused(string name)
    {
        FluentActions.Invoking(() => Label.New(name)).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TheEmptyLabel_HasNothing()
    {
        default(Label).IsEmpty.Should().BeTrue();
        default(Label).Name.Should().BeEmpty();
        default(Label).UID.IsEmpty.Should().BeTrue();
        Label.New("x").IsEmpty.Should().BeFalse();
    }
}
