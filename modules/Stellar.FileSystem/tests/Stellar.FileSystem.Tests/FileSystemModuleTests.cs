namespace Stellar.FileSystem.Tests;

public class FileSystemModuleTests
{
    [Fact]
    public void TheAssemblyIsTheModule() =>
        typeof(FileSystemModule).Assembly.GetName().Name.Should().Be(FileSystemModule.Id);
}
