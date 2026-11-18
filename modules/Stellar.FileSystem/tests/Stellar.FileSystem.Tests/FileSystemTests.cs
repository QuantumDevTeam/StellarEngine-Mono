using System.Text;
using Stellar.Kernel.FileSystem;

namespace Stellar.FileSystem.Tests;

public class FileSystemTests
{
    private static VirtualFileSystem WithMemory(params string[] names)
    {
        var fileSystem = VirtualFileSystem.CreateDefault();
        foreach (var name in names) fileSystem.Mount(new DomainMount(name, DomainScheme.Memory, string.Empty));
        return fileSystem;
    }

    [Fact]
    public void Domains_AreFoundByName()
    {
        using var fileSystem = WithMemory("assets");

        fileSystem.Domains.Get("Assets").Name.Should().Be("assets");
        fileSystem.Domains.Contains("ASSETS").Should().BeTrue();
        fileSystem.Domains.Contains("not a name").Should().BeFalse();
        fileSystem.Invoking(fs => fs.Domains.Get("nowhere")).Should().Throw<DomainNotFoundException>();
        fileSystem.Invoking(fs => fs.Mount(new DomainMount("assets", DomainScheme.Memory, ""))).Should().Throw<InvalidOperationException>();
        fileSystem.Invoking(fs => fs.Mount(new DomainMount("net", new DomainScheme("net"), ""))).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Drivers_AreACatalogBySchemes()
    {
        using var fileSystem = VirtualFileSystem.CreateDefault();

        fileSystem.Drivers.Keys.Should().BeEquivalentTo([DomainScheme.Directory, DomainScheme.Assembly, DomainScheme.Memory, DomainScheme.Overlay]);
        fileSystem.Invoking(fs => fs.AddDriver(new Drivers.MemoryDriver())).Should().Throw<InvalidOperationException>();
        fileSystem.Invoking(fs => fs.Drivers.Set(new DomainScheme("zip"), new Drivers.MemoryDriver())).Should().Throw<ArgumentException>();

        fileSystem.Drivers.Remove(DomainScheme.Memory).Should().BeTrue();
        fileSystem.Invoking(fs => fs.Mount(new DomainMount("m", DomainScheme.Memory, ""))).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ContentTypes_AreACatalogByExtension()
    {
        var types = ContentTypeCatalog.CreateDefault();

        types.Get(".PNG").MediaType.Should().Be("image/png");
        types.Resolve(FilePath.Parse("a://b/c.Json")).IsText.Should().BeTrue();
        types.Resolve(FilePath.Parse("a://b/c.unknown")).Should().Be(ContentType.OctetStream);
        types.Contains("png").Should().BeFalse();
        types.Invoking(t => t.Set("png", ContentType.PlainText)).Should().Throw<ArgumentException>();
        types.Invoking(t => t.Set(".x", default)).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Unmount_TellsAndDisposes()
    {
        using var fileSystem = WithMemory("assets");
        var domain = fileSystem.Domains.Get("assets");
        IDomain? told = null;
        fileSystem.Unmounting += d => told = d;

        fileSystem.Unmount("assets").Should().BeTrue();

        told.Should().BeSameAs(domain);
        domain.Invoking(d => d.Exists("a")).Should().Throw<ObjectDisposedException>();
        fileSystem.Unmount("assets").Should().BeFalse();
    }

    [Fact]
    public void Memory_WritesWhenTheStreamCloses()
    {
        using var fileSystem = WithMemory("mem");
        var path = FilePath.Parse("mem://levels/1.json");

        using (var stream = fileSystem.Open(path, FileOpenOptions.Write))
        {
            stream.Write("{}"u8);
            fileSystem.Exists(path).Should().BeFalse();
        }

        fileSystem.ReadAllText(path).Should().Be("{}");
        var entry = fileSystem.GetEntry(path);
        entry.IsFile.Should().BeTrue();
        entry.Length.Should().Be(2);
        entry.Content.MediaType.Should().Be("application/json");
        fileSystem.GetEntry(FilePath.Parse("mem://levels")).IsDirectory.Should().BeTrue();
    }

    [Fact]
    public void Memory_GivesBytesThatNeverChange()
    {
        using var fileSystem = WithMemory("mem");
        var path = FilePath.Parse("mem://data.bin");
        fileSystem.WriteAllBytes(path, [1, 2, 3]);

        fileSystem.TryGetMemory(path, out var before).Should().BeTrue();
        var revision = fileSystem.GetEntry(path).Revision;
        fileSystem.WriteAllBytes(path, [9]);

        before.ToArray().Should().Equal(1, 2, 3);
        fileSystem.ReadAllBytes(path).Should().Equal(9);
        fileSystem.GetEntry(path).Revision.Should().NotBe(revision);
    }

    [Fact]
    public void Memory_ModesAsOnADisk()
    {
        using var fileSystem = WithMemory("mem");
        var path = FilePath.Parse("mem://log.txt");

        fileSystem.Invoking(fs => fs.OpenRead(path)).Should().Throw<FileNotFoundException>();
        fileSystem.WriteAllText(path, "a");
        using (var append = fileSystem.Open(path, FileOpenOptions.Append)) append.Write("b"u8);
        fileSystem.ReadAllText(path).Should().Be("ab");
        fileSystem.Invoking(fs => fs.Open(path, new FileOpenOptions(FileMode.CreateNew, FileAccess.Write))).Should().Throw<IOException>();
        fileSystem.Invoking(fs => fs.WriteAllText(FilePath.Parse("mem://log.txt/inner.txt"), "x")).Should().Throw<IOException>();
    }

    [Fact]
    public void Memory_ListsMovesAndDeletes()
    {
        using var fileSystem = WithMemory("mem");
        fileSystem.WriteAllText(FilePath.Parse("mem://a/one.txt"), "1");
        fileSystem.WriteAllText(FilePath.Parse("mem://a/b/two.json"), "2");

        fileSystem.Enumerate(FilePath.Parse("mem://a")).Select(e => e.Path.ToString())
            .Should().Equal("mem://a/b", "mem://a/one.txt");
        fileSystem.Enumerate(FilePath.Root("mem"), "*.json", recursive: true).Select(e => e.Path.Value)
            .Should().Equal("a/b/two.json");

        fileSystem.Move(FilePath.Parse("mem://a"), FilePath.Parse("mem://c"));
        fileSystem.ReadAllText(FilePath.Parse("mem://c/b/two.json")).Should().Be("2");
        fileSystem.Exists(FilePath.Parse("mem://a")).Should().BeFalse();

        fileSystem.Invoking(fs => fs.Delete(FilePath.Parse("mem://c"))).Should().Throw<IOException>();
        fileSystem.Delete(FilePath.Parse("mem://c"), recursive: true).Should().BeTrue();
        fileSystem.Enumerate(FilePath.Root("mem"), recursive: true).Should().BeEmpty();
        fileSystem.Invoking(fs => fs.Delete(FilePath.Root("mem"), true)).Should().Throw<IOException>();
    }

    [Fact]
    public void Memory_ReportsChanges()
    {
        using var fileSystem = WithMemory("mem");
        fileSystem.CreateDirectory(FilePath.Parse("mem://watched"));
        var changes = new List<FileChange>();

        using (fileSystem.Watch(FilePath.Parse("mem://watched"), recursive: true, changes.Add))
        {
            fileSystem.WriteAllText(FilePath.Parse("mem://watched/a.txt"), "1");
            fileSystem.WriteAllText(FilePath.Parse("mem://watched/a.txt"), "2");
            fileSystem.WriteAllText(FilePath.Parse("mem://elsewhere.txt"), "3");
            fileSystem.Move(FilePath.Parse("mem://watched/a.txt"), FilePath.Parse("mem://watched/b.txt"));
            fileSystem.Delete(FilePath.Parse("mem://watched/b.txt"));
        }

        fileSystem.WriteAllText(FilePath.Parse("mem://watched/c.txt"), "after");

        changes.Select(c => c.Kind).Should().Equal(
            FileChangeKind.Created, FileChangeKind.Changed, FileChangeKind.Renamed, FileChangeKind.Deleted);
        changes[2].OldPath.Should().Be(FilePath.Parse("mem://watched/a.txt"));
    }

    [Fact]
    public void RandomAccess_ReadsAtOffsets()
    {
        using var fileSystem = WithMemory("mem");
        var path = FilePath.Parse("mem://pack.bin");
        fileSystem.WriteAllBytes(path, [0, 1, 2, 3, 4, 5]);

        using var file = fileSystem.OpenRandomAccess(path);
        var buffer = new byte[4];

        file.Length.Should().Be(6);
        file.Read(4, buffer).Should().Be(2);
        buffer[..2].Should().Equal(4, 5);
        file.Read(10, buffer).Should().Be(0);
    }

    [Fact]
    public void Capabilities_AreCheckedFirst()
    {
        using var fileSystem = VirtualFileSystem.CreateDefault();
        fileSystem.Mount(new DomainMount("self", DomainScheme.Assembly, typeof(FileSystemTests).Assembly.GetName().Name!));

        fileSystem.Invoking(fs => fs.WriteAllText(FilePath.Parse("self://data/hello.txt"), "x")).Should().Throw<NotSupportedException>();
        fileSystem.Invoking(fs => fs.Watch(FilePath.Root("self"), true, _ => { })).Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void Assembly_ServesEmbeddedFilesWithoutACopy()
    {
        using var fileSystem = VirtualFileSystem.CreateDefault();
        fileSystem.Mount(new DomainMount("self", DomainScheme.Assembly, typeof(FileSystemTests).Assembly.GetName().Name!));
        var path = FilePath.Parse("self://data/hello.txt");

        fileSystem.ReadAllText(path).Should().Be("Hello from the assembly");
        fileSystem.TryGetMemory(path, out var memory).Should().BeTrue();
        Encoding.UTF8.GetString(memory.Span).Should().Be("Hello from the assembly");
        fileSystem.GetEntry(path).Content.IsText.Should().BeTrue();
        fileSystem.Enumerate(FilePath.Root("self"), recursive: true).Select(e => e.Path.Value).Should().Contain(["data", "data/hello.txt"]);
    }

    [Fact]
    public void Overlay_FirstLayerWins_AndWritesGoUp()
    {
        using var fileSystem = WithMemory("mods", "base");
        fileSystem.WriteAllText(FilePath.Parse("base://textures/hero.txt"), "base hero");
        fileSystem.WriteAllText(FilePath.Parse("base://textures/enemy.txt"), "base enemy");
        fileSystem.WriteAllText(FilePath.Parse("mods://textures/hero.txt"), "mod hero");
        fileSystem.Mount(new DomainMount("game", DomainScheme.Overlay, "mods, base", new Dictionary<string, string> { ["write"] = "mods" }));

        fileSystem.ReadAllText(FilePath.Parse("game://textures/hero.txt")).Should().Be("mod hero");
        fileSystem.ReadAllText(FilePath.Parse("game://textures/enemy.txt")).Should().Be("base enemy");
        fileSystem.Enumerate(FilePath.Parse("game://textures")).Select(e => e.Path.ToString())
            .Should().BeEquivalentTo("game://textures/hero.txt", "game://textures/enemy.txt");
        fileSystem.GetEntry(FilePath.Parse("game://textures/enemy.txt")).Path.Domain.Should().Be("game");

        using (var stream = fileSystem.Open(FilePath.Parse("game://textures/enemy.txt"), FileOpenOptions.Append)) stream.Write(" modded"u8);
        fileSystem.ReadAllText(FilePath.Parse("mods://textures/enemy.txt")).Should().Be("base enemy modded");
        fileSystem.ReadAllText(FilePath.Parse("base://textures/enemy.txt")).Should().Be("base enemy");

        fileSystem.Delete(FilePath.Parse("game://textures/enemy.txt"));
        fileSystem.ReadAllText(FilePath.Parse("game://textures/enemy.txt")).Should().Be("base enemy");
    }

    [Fact]
    public void Overlay_RefusesItself_AndReadsOnlyWithoutAWriteLayer()
    {
        using var fileSystem = WithMemory("base");

        fileSystem.Invoking(fs => fs.Mount(new DomainMount("loop", DomainScheme.Overlay, "loop,base"))).Should().Throw<ArgumentException>();
        fileSystem.Mount(new DomainMount("view", DomainScheme.Overlay, "base"));
        fileSystem.Invoking(fs => fs.WriteAllText(FilePath.Parse("view://a.txt"), "x")).Should().Throw<NotSupportedException>();
    }
}
