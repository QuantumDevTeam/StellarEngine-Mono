using Stellar.Tools.Diagnostics;
using Stellar.Tools.Projects;
using Stellar.Tools.Resources;

namespace Stellar.Tools.Tests;

public class ResourceContainerTests
{
    private static CompiledResource Sample()
    {
        return new CompiledResource(new FourCC("MESH"),
        [
            new ResourceChunk(new FourCC("VERT"), [1, 2, 3]),
            new ResourceChunk(new FourCC("IDX"), new byte[100])
        ]);
    }

    private static byte[] Serialize(CompiledResource resource)
    {
        using var stream = new MemoryStream();
        ResourceContainer.Write(stream, resource);
        return stream.ToArray();
    }

    [Fact]
    public void Container_RoundTrips()
    {
        using var stream = new MemoryStream(Serialize(Sample()));

        var read = ResourceContainer.Read(stream);

        read.Type.ToString().Should().Be("MESH");
        read.Chunks.Should().HaveCount(2);
        read.Chunks[0].Tag.ToString().Should().Be("VERT");
        read.Chunks[0].Data.Should().Equal(1, 2, 3);
        read.Chunks[1].Tag.ToString().Should().Be("IDX ");
        read.Chunks[1].Data.Should().HaveCount(100);
    }

    [Fact]
    public void Layout_MatchesTheNativeHeader()
    {
        var bytes = Serialize(Sample());

        // The fields of ResourceHeader from Resources/ResourceContainer.h of the native Kernel
        BitConverter.ToUInt32(bytes, 0).Should().Be(new FourCC("SRES").Value);
        BitConverter.ToUInt16(bytes, 4).Should().Be(ResourceContainer.FormatVersion);
        BitConverter.ToUInt32(bytes, 8).Should().Be(new FourCC("MESH").Value);
        BitConverter.ToUInt32(bytes, 12).Should().Be(2);

        // The fields of the first ResourceChunkEntry
        BitConverter.ToUInt32(bytes, 16).Should().Be(new FourCC("VERT").Value);
        BitConverter.ToUInt64(bytes, 24).Should().Be(64);
        BitConverter.ToUInt64(bytes, 32).Should().Be(3);
    }

    [Fact]
    public void Chunks_AreAligned()
    {
        var bytes = Serialize(Sample());

        (BitConverter.ToUInt64(bytes, 24) % ResourceContainer.ChunkAlignment).Should().Be(0);
        (BitConverter.ToUInt64(bytes, 48) % ResourceContainer.ChunkAlignment).Should().Be(0);
    }

    [Fact]
    public void Read_RejectsForeignData()
    {
        using var stream = new MemoryStream(new byte[32]);

        var action = () => ResourceContainer.Read(stream);

        action.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void Read_RejectsChunkOutsideTheFile()
    {
        var bytes = Serialize(Sample());
        BitConverter.TryWriteBytes(bytes.AsSpan(32), ulong.MaxValue / 2);

        using var stream = new MemoryStream(bytes);
        var action = () => ResourceContainer.Read(stream);

        action.Should().Throw<InvalidDataException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("TOOLONG")]
    [InlineData("ЖЖ")]
    public void FourCC_RejectsInvalidText(string text)
    {
        var action = () => new FourCC(text);

        action.Should().Throw<ArgumentException>();
    }
}

public class ResourcePipelineTests
{
    private sealed class UpperCaseCompiler(int version = 1) : IResourceCompiler
    {
        public int Calls { get; private set; }

        public string Id => "Test.UpperCase";

        public int Version => version;

        public bool CanCompile(string extension)
        {
            return extension == ".txt";
        }

        public CompiledResource Compile(ResourceCompileContext context)
        {
            Calls++;

            var text = File.ReadAllText(context.SourcePath).ToUpperInvariant();
            return new CompiledResource(new FourCC("TEXT"),
                [new ResourceChunk(new FourCC("DATA"), System.Text.Encoding.UTF8.GetBytes(text))]);
        }
    }

    private sealed class ThrowingCompiler : IResourceCompiler
    {
        public string Id => "Test.Throwing";

        public int Version => 1;

        public bool CanCompile(string extension)
        {
            return true;
        }

        public CompiledResource Compile(ResourceCompileContext context)
        {
            throw new InvalidOperationException("bad data");
        }
    }

    private static ResourcePipeline Pipeline(IResourceCompiler compiler)
    {
        var registry = ResourceCompilerRegistry.CreateDefault();
        registry.Add(compiler);

        return new ResourcePipeline(registry, NullToolLogger.Instance);
    }

    private static ProjectFile Source(TempDirectory directory, string relativePath, string content = "content")
    {
        return new ProjectFile(directory.Write(relativePath, content), relativePath);
    }

    [Fact]
    public void Compile_UsesTheMatchingCompiler_AndFallsBackToRaw()
    {
        using var directory = new TempDirectory();
        var output = Path.Combine(directory.Path, "out");

        var result = Pipeline(new UpperCaseCompiler()).Compile(
            [Source(directory, "src/a.txt", "abc"), Source(directory, "src/b.bin", "abc")], output);

        result.Compiled.Should().Be(2);

        using var text = File.OpenRead(Path.Combine(output, "src/a.txt.sres"));
        var compiled = ResourceContainer.Read(text);
        compiled.Type.ToString().Should().Be("TEXT");
        System.Text.Encoding.UTF8.GetString(compiled.Chunks[0].Data).Should().Be("ABC");

        using var raw = File.OpenRead(Path.Combine(output, "src/b.bin.sres"));
        ResourceContainer.Read(raw).Type.Should().Be(RawResourceCompiler.ResourceType);
    }

    [Fact]
    public void Compile_SkipsUpToDateFiles()
    {
        using var directory = new TempDirectory();
        var output = Path.Combine(directory.Path, "out");
        var compiler = new UpperCaseCompiler();
        var source = Source(directory, "a.txt");

        Pipeline(compiler).Compile([source], output);
        var second = Pipeline(compiler).Compile([source], output);

        compiler.Calls.Should().Be(1);
        second.UpToDate.Should().Be(1);
        second.Outputs.Should().ContainSingle();
    }

    [Fact]
    public void Compile_RebuildsWhenTheSourceChanges()
    {
        using var directory = new TempDirectory();
        var output = Path.Combine(directory.Path, "out");
        var compiler = new UpperCaseCompiler();

        Pipeline(compiler).Compile([Source(directory, "a.txt", "one")], output);
        Pipeline(compiler).Compile([Source(directory, "a.txt", "two")], output);

        compiler.Calls.Should().Be(2);
    }

    [Fact]
    public void Compile_RebuildsWhenTheCompilerVersionChanges()
    {
        using var directory = new TempDirectory();
        var output = Path.Combine(directory.Path, "out");
        var source = Source(directory, "a.txt");
        var updated = new UpperCaseCompiler(2);

        Pipeline(new UpperCaseCompiler()).Compile([source], output);
        Pipeline(updated).Compile([source], output);

        updated.Calls.Should().Be(1);
    }

    [Fact]
    public void Compile_RebuildsWhenTheOutputIsGone()
    {
        using var directory = new TempDirectory();
        var output = Path.Combine(directory.Path, "out");
        var compiler = new UpperCaseCompiler();
        var source = Source(directory, "a.txt");

        Pipeline(compiler).Compile([source], output);
        File.Delete(Path.Combine(output, "a.txt.sres"));
        Pipeline(compiler).Compile([source], output);

        compiler.Calls.Should().Be(2);
    }

    [Fact]
    public void Compile_RemovesOutputsOfDeletedSources()
    {
        using var directory = new TempDirectory();
        var output = Path.Combine(directory.Path, "out");
        var compiler = new UpperCaseCompiler();

        Pipeline(compiler).Compile([Source(directory, "a.txt"), Source(directory, "b.txt")], output);
        var result = Pipeline(compiler).Compile([Source(directory, "a.txt")], output);

        result.Removed.Should().Be(1);
        File.Exists(Path.Combine(output, "b.txt.sres")).Should().BeFalse();
    }

    [Fact]
    public void Compile_SurvivesCorruptedCache()
    {
        using var directory = new TempDirectory();
        var output = Path.Combine(directory.Path, "out");
        var source = Source(directory, "a.txt");

        Pipeline(new UpperCaseCompiler()).Compile([source], output);
        File.WriteAllText(Path.Combine(output, ResourcePipeline.CacheFileName), "{ broken");

        var result = Pipeline(new UpperCaseCompiler()).Compile([source], output);

        result.Compiled.Should().Be(1);
    }

    [Fact]
    public void Compile_ReportsWhichFileFailed()
    {
        using var directory = new TempDirectory();

        var action = () => Pipeline(new ThrowingCompiler()).Compile(
            [Source(directory, "broken.txt")], Path.Combine(directory.Path, "out"));

        action.Should().Throw<ToolException>().WithMessage("*Test.Throwing*broken.txt*bad data*");
    }

    [Fact]
    public void Registry_PrefersTheLatestCompiler()
    {
        var registry = ResourceCompilerRegistry.CreateDefault();
        var compiler = new UpperCaseCompiler();
        registry.Add(compiler);

        registry.Find("a.TXT").Should().BeSameAs(compiler);
        registry.Find("a.bin").Should().BeOfType<RawResourceCompiler>();
    }

    [Fact]
    public void Registry_LoadsCompilersFromAnAssembly()
    {
        var registry = new ResourceCompilerRegistry();

        registry.AddFrom(typeof(RawResourceCompiler).Assembly.Location, NullToolLogger.Instance);

        registry.Compilers.Should().ContainSingle().Which.Should().BeOfType<RawResourceCompiler>();
    }

    [Fact]
    public void Registry_IgnoresMissingCompilerDirectory()
    {
        var registry = new ResourceCompilerRegistry();

        registry.AddFromDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()), NullToolLogger.Instance);

        registry.Compilers.Should().BeEmpty();
    }
}
