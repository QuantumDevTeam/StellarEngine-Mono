using Stellar.Kernel.FileSystem;

namespace Stellar.Core.Tests;

public class FilePathTests
{
    [Theory]
    [InlineData("assets://textures/hero.png", "assets", "textures/hero.png")]
    [InlineData("Assets://textures/hero.png", "assets", "textures/hero.png")]
    [InlineData(@"assets://textures\hero.png", "assets", "textures/hero.png")]
    [InlineData("assets:///textures//./hero.png/", "assets", "textures/hero.png")]
    [InlineData("assets://textures/../sounds/hit.wav", "assets", "sounds/hit.wav")]
    [InlineData("assets://", "assets", "")]
    [InlineData("user-data.v2://saves/1.sav", "user-data.v2", "saves/1.sav")]
    public void Parse_GivesTheOneForm(string text, string domain, string value)
    {
        var path = FilePath.Parse(text);

        path.Domain.Should().Be(domain);
        path.Value.Should().Be(value);
        path.ToString().Should().Be($"{domain}://{value}");
        FilePath.Parse(path.ToString()).Should().Be(path);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("textures/hero.png")]
    [InlineData("://textures/hero.png")]
    [InlineData("-assets://a")]
    [InlineData("as sets://a")]
    [InlineData("assets://..")]
    [InlineData("assets://a/../../b")]
    [InlineData("assets://C:/Windows")]
    [InlineData("assets://a/b.")]
    [InlineData("assets://a /b")]
    [InlineData("assets://a/*.png")]
    [InlineData("assets://a\u0001b")]
    public void TryParse_RefusesWhatIsNotAPath(string? text)
    {
        FilePath.TryParse(text, out var path).Should().BeFalse();
        path.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void Paths_ToOneFile_AreEqual()
    {
        var first = FilePath.Create("Assets", @"textures\.\hero.png");
        var second = FilePath.Parse("assets://textures/hero.png");

        first.Should().Be(second);
        (first == second).Should().BeTrue();
        first.GetHashCode().Should().Be(second.GetHashCode());
        first.Should().NotBe(FilePath.Parse("assets://textures/Hero.png"));
    }

    [Fact]
    public void AnAlreadyNormalPath_IsNotCopied()
    {
        const string value = "textures/hero.png";
        FilePath.Create("assets", value).Value.Should().BeSameAs(value);
    }

    [Fact]
    public void Names_AndExtensions()
    {
        var path = FilePath.Parse("assets://textures/hero.albedo.png");

        path.Name.Should().Be("hero.albedo.png");
        path.Extension.Should().Be(".png");
        path.NameWithoutExtension.Should().Be("hero.albedo");
        path.Parent.Should().Be(FilePath.Parse("assets://textures"));
        path.Parent.Parent.IsRoot.Should().BeTrue();
        FilePath.Root("assets").Name.Should().BeEmpty();
    }

    [Fact]
    public void Root_HasNoParent()
    {
        var root = FilePath.Root("assets");

        root.IsRoot.Should().BeTrue();
        root.Invoking(r => r.Parent).Should().Throw<InvalidOperationException>();
        default(FilePath).Invoking(p => p.Parent).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Combine_StaysInsideTheDomain()
    {
        var textures = FilePath.Parse("assets://textures");

        textures.Combine("hero.png").Should().Be(FilePath.Parse("assets://textures/hero.png"));
        textures.Combine("../sounds/hit.wav").Should().Be(FilePath.Parse("assets://sounds/hit.wav"));
        textures.Combine("/levels/1.json").Should().Be(FilePath.Parse("assets://levels/1.json"));
        FilePath.Root("assets").Combine("a").Should().Be(FilePath.Parse("assets://a"));
        textures.Combine("").Should().Be(textures);
        textures.Invoking(t => t.Combine("../../escape")).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void IsInside_FollowsSegments()
    {
        var textures = FilePath.Parse("assets://textures");

        FilePath.Parse("assets://textures/hero.png").IsInside(textures).Should().BeTrue();
        FilePath.Parse("assets://textures/ui/button.png").IsInside(textures).Should().BeTrue();
        FilePath.Parse("assets://textures2/hero.png").IsInside(textures).Should().BeFalse();
        textures.IsInside(textures).Should().BeFalse();
        textures.IsInside(FilePath.Root("assets")).Should().BeTrue();
        FilePath.Parse("mods://textures/hero.png").IsInside(textures).Should().BeFalse();
    }

    [Fact]
    public void WithDomain_KeepsThePath()
    {
        FilePath.Parse("base://textures/hero.png").WithDomain("mods")
            .Should().Be(FilePath.Parse("mods://textures/hero.png"));
    }

    [Fact]
    public void TheEmptyPath_NamesNothing()
    {
        var empty = default(FilePath);

        empty.IsEmpty.Should().BeTrue();
        empty.IsRoot.Should().BeFalse();
        empty.ToString().Should().BeEmpty();
        empty.Should().NotBe(FilePath.Root("assets"));
        empty.Invoking(p => p.Combine("a")).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Schemes_AreFoldedNames()
    {
        new DomainScheme("DIR").Should().Be(DomainScheme.Directory);
        new DomainScheme("zip").Name.Should().Be("zip");
        FluentActions.Invoking(() => new DomainScheme("no scheme")).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ContentTypes_ComparedByValue()
    {
        new ContentType("Text/Plain", ContentKind.Text, "UTF-8").Should().Be(ContentType.PlainText);
        ContentType.PlainText.ToString().Should().Be("text/plain; charset=utf-8");
        default(ContentType).IsUnknown.Should().BeTrue();
        default(ContentType).MediaType.Should().Be(ContentType.OctetStream.MediaType);
        FluentActions.Invoking(() => new ContentType("png", ContentKind.Binary)).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void OpenOptions_TellWrites()
    {
        FileOpenOptions.Read.Writes.Should().BeFalse();
        FileOpenOptions.Write.Writes.Should().BeTrue();
        FileOpenOptions.Replace.Flags.Should().HaveFlag(FileOpenFlags.Atomic);
    }
}
