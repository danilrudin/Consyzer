namespace Consyzer.Tests.TestSupport.FileSystem;

internal sealed class TemporaryDirectory(
    string prefix = TemporaryDirectory.DefaultDirectoryPrefix,
    string? rootPath = null
) : IDisposable
{
    private const string DefaultDirectoryPrefix = "consyzer-tests-";
    private const string CompactGuidFormat = "N";

    public DirectoryInfo Directory { get; } = rootPath is null
            ? System.IO.Directory.CreateTempSubdirectory(prefix)
            : System.IO.Directory.CreateDirectory(
                System.IO.Path.Combine(rootPath, CreateUniqueDirectoryName(prefix))
            );

    public string Path => Directory.FullName;

    public FileInfo CreateFile(string name, string content = "")
    {
        var path = System.IO.Path.Combine(Path, name);
        File.WriteAllText(path, content);

        return new FileInfo(path);
    }

    public void Dispose()
    {
        if (Directory.Exists)
        {
            Directory.Delete(recursive: true);
        }
    }

    private static string CreateUniqueDirectoryName(string prefix) =>
        prefix + Guid.NewGuid().ToString(CompactGuidFormat);
}
