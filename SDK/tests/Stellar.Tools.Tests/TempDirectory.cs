namespace Stellar.Tools.Tests;

/// <summary>
/// A directory that lives for the duration of one test.
/// </summary>
public sealed class TempDirectory : IDisposable
{
    public string Path { get; } =
        Directory.CreateDirectory(System.IO.Path.Combine(System.IO.Path.GetTempPath(),
            $"stellar-tests-{Guid.NewGuid():N}")).FullName;

    public string Write(string relativePath, string content = "content")
    {
        var path = System.IO.Path.Combine(Path, relativePath);

        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);

        return path;
    }

    public void Dispose()
    {
        Directory.Delete(Path, true);
    }
}
