using Stellar.Kernel.FileSystem;

namespace Stellar.FileSystem.Tests;

public sealed class DirectoryDomainTests
    : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "Stellar.FileSystem.Tests", Guid.NewGuid().ToString("N"));
    private readonly VirtualFileSystem _fileSystem = VirtualFileSystem.CreateDefault();

    public DirectoryDomainTests()
    {
        Directory.CreateDirectory(_root);
        _fileSystem.Mount(new DomainMount("disk", DomainScheme.Directory, _root));
    }

    public void Dispose()
    {
        _fileSystem.Dispose();
        // Directory.Delete removes links as links, never what they lead to
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void ARelativeSource_IsRefused()
    {
        _fileSystem.Invoking(fs => fs.Mount(new DomainMount("rel", DomainScheme.Directory, "relative/dir")))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ReadOnly_NeedsTheDirectory_AndRefusesWrites()
    {
        var options = new Dictionary<string, string> { ["readonly"] = "true" };
        _fileSystem.Invoking(fs => fs.Mount(new DomainMount("missing", DomainScheme.Directory, Path.Combine(_root, "missing"), options)))
            .Should().Throw<DirectoryNotFoundException>();

        _fileSystem.Mount(new DomainMount("ro", DomainScheme.Directory, _root, options));
        _fileSystem.Invoking(fs => fs.WriteAllText(FilePath.Parse("ro://a.txt"), "x")).Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void Files_GoUnderTheRoot()
    {
        var path = FilePath.Parse("disk://saves/slot1/game.json");
        _fileSystem.WriteAllText(path, "{\"level\":1}");

        File.ReadAllText(Path.Combine(_root, "saves", "slot1", "game.json")).Should().Be("{\"level\":1}");
        _fileSystem.ReadAllText(path).Should().Be("{\"level\":1}");

        var entry = _fileSystem.GetEntry(path);
        entry.Length.Should().Be(11);
        entry.Content.MediaType.Should().Be("application/json");
        entry.Revision.Should().NotBe(0);
    }

    [Fact]
    public void AtomicWrite_ShowsNoTemporaryFile()
    {
        var path = FilePath.Parse("disk://settings.ini");
        _fileSystem.WriteAllText(path, "old");

        using (var stream = _fileSystem.Open(path, FileOpenOptions.Replace))
        {
            stream.Write("new"u8);
            _fileSystem.ReadAllText(path).Should().Be("old");
            _fileSystem.Enumerate(FilePath.Root("disk")).Select(e => e.Path.Value).Should().Equal("settings.ini");
        }

        _fileSystem.ReadAllText(path).Should().Be("new");
        Directory.GetFiles(_root).Should().ContainSingle();
    }

    [Fact]
    public void Enumerate_ListsWithPatterns()
    {
        _fileSystem.WriteAllText(FilePath.Parse("disk://a/one.txt"), "1");
        _fileSystem.WriteAllText(FilePath.Parse("disk://a/b/two.json"), "2");

        _fileSystem.Enumerate(FilePath.Parse("disk://a")).Select(e => e.Path.Value).Should().BeEquivalentTo("a/one.txt", "a/b");
        _fileSystem.Enumerate(FilePath.Root("disk"), "*.json", recursive: true).Select(e => e.Path.Value).Should().Equal("a/b/two.json");
        _fileSystem.Invoking(fs => fs.Enumerate(FilePath.Parse("disk://none")).ToList()).Should().Throw<DirectoryNotFoundException>();
    }

    [Fact]
    public void RandomAccess_ReadsTheDisk()
    {
        var path = FilePath.Parse("disk://pack.bin");
        _fileSystem.WriteAllBytes(path, [10, 11, 12, 13]);

        using var file = _fileSystem.OpenRandomAccess(path);
        var buffer = new byte[2];
        file.Read(2, buffer).Should().Be(2);
        buffer.Should().Equal(12, 13);
    }

    [Fact]
    public void ALinkOutOfTheRoot_IsRefused()
    {
        var outside = Path.Combine(Path.GetTempPath(), "Stellar.FileSystem.Tests", Guid.NewGuid().ToString("N") + "-outside");
        Directory.CreateDirectory(outside);
        File.WriteAllText(Path.Combine(outside, "secret.txt"), "secret");

        try
        {
            try { Directory.CreateSymbolicLink(Path.Combine(_root, "escape"), outside); }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { return; } // No right to make links here

            var path = FilePath.Parse("disk://escape/secret.txt");
            _fileSystem.Exists(path).Should().BeFalse();
            _fileSystem.Invoking(fs => fs.ReadAllText(path)).Should().Throw<UnauthorizedAccessException>();
            _fileSystem.Enumerate(FilePath.Root("disk"), recursive: true).Should().BeEmpty();
        }
        finally
        {
            Directory.Delete(outside, recursive: true);
        }
    }

    [Fact]
    public async Task Watch_ReportsChanges()
    {
        var changes = new List<FileChange>();
        var created = new TaskCompletionSource();

        using (_fileSystem.Watch(FilePath.Root("disk"), recursive: true, change =>
               {
                   lock (changes) changes.Add(change);
                   if (change.Path.Value == "watched.txt") created.TrySetResult();
               }))
        {
            _fileSystem.WriteAllText(FilePath.Parse("disk://watched.txt"), "1");
            (await Task.WhenAny(created.Task, Task.Delay(TimeSpan.FromSeconds(10)))).Should().Be(created.Task);
        }

        lock (changes) changes.Should().Contain(c => c.Path == FilePath.Parse("disk://watched.txt"));
    }
}
