using Xunit;
using Consyzer.Core.Models;
using Consyzer.Core.Resolvers.Platform;

namespace Consyzer.Tests.Core.Resolvers.Platform;

public sealed class WindowsLibraryPresenceResolverTests : IDisposable
{
    private const string DummyFileContent = "dummy";
    private const string DllExtension = ".dll";
    private const string LibAnalyzed = "testlib-analyzed" + DllExtension;
    private const string LibAbsolute = "testlib-abs" + DllExtension;
    private const string LibRelative = "testlib-rel" + DllExtension;
    private const string LibEnvironment = "testlib-env" + DllExtension;
    private const string LibMissing = "nonexistent-lib" + DllExtension;

    private static readonly string PathVariableName = "PATH";

    private readonly string _analyzedDirectory = Path.Combine(Path.GetTempPath(), "analyzed-" + Guid.NewGuid());
    private readonly string _envPathDirectory = Path.Combine(Path.GetTempPath(), "envpath-" + Guid.NewGuid());

    public WindowsLibraryPresenceResolverTests()
    {
        Directory.CreateDirectory(_analyzedDirectory);
        Directory.CreateDirectory(_envPathDirectory);
    }

    [Fact]
    public void Resolve_ShouldReturnInAnalyzedDirectory_WhenLibraryIsInAnalyzedDirectory()
    {
        var libraryPath = Path.Combine(_analyzedDirectory, LibAnalyzed);
        File.WriteAllText(libraryPath, DummyFileContent);

        try
        {
            var resolver = new WindowsLibraryPresenceResolver(_analyzedDirectory);
            var result = resolver.Resolve(LibAnalyzed);

            Assert.Equal(LibraryLocationKind.InAnalyzedDirectory, result.LocationKind);
            Assert.Equal(libraryPath, result.ResolvedPath);
        }
        finally
        {
            File.Delete(libraryPath);
        }
    }

    [Fact]
    public void Resolve_ShouldReturnInSystemDirectory_WhenLibraryIsInSystemDirectory()
    {
        var systemDir = Environment.SystemDirectory;
        var libraryName = Directory.GetFiles(systemDir)
            .Select(Path.GetFileName)
            .FirstOrDefault(name => name?.EndsWith(DllExtension, StringComparison.OrdinalIgnoreCase) is true);

        if (libraryName is null) return;

        var resolver = new WindowsLibraryPresenceResolver(_analyzedDirectory);
        var result = resolver.Resolve(libraryName);

        Assert.Contains(result.LocationKind, new[] {
            LibraryLocationKind.InSystemDirectory,
            LibraryLocationKind.InEnvironmentPath
        });
        Assert.Contains(systemDir, result.ResolvedPath!);
    }

    [Fact]
    public void Resolve_ShouldReturnInEnvironmentPath_WhenLibraryInPath()
    {
        var libraryPath = Path.Combine(_envPathDirectory, LibEnvironment);
        File.WriteAllText(libraryPath, DummyFileContent);

        var originalPath = Environment.GetEnvironmentVariable(PathVariableName) ?? string.Empty;
        Environment.SetEnvironmentVariable(PathVariableName, _envPathDirectory + Path.PathSeparator + originalPath);

        try
        {
            var resolver = new WindowsLibraryPresenceResolver(_analyzedDirectory);
            var result = resolver.Resolve(LibEnvironment);

            Assert.Equal(LibraryLocationKind.InEnvironmentPath, result.LocationKind);
            Assert.Equal(Path.GetFullPath(libraryPath), result.ResolvedPath);
        }
        finally
        {
            Environment.SetEnvironmentVariable(PathVariableName, originalPath);
            File.Delete(libraryPath);
        }
    }

    [Fact]
    public void Resolve_ShouldReturnOnAbsolutePath_WhenLibraryExistsAtAbsolutePath()
    {
        var libraryPath = Path.Combine(Path.GetTempPath(), LibAbsolute);
        File.WriteAllText(libraryPath, DummyFileContent);

        try
        {
            var resolver = new WindowsLibraryPresenceResolver(_analyzedDirectory);
            var result = resolver.Resolve(libraryPath);

            Assert.Equal(LibraryLocationKind.OnAbsolutePath, result.LocationKind);
            Assert.Equal(libraryPath, result.ResolvedPath);
        }
        finally
        {
            File.Delete(libraryPath);
        }
    }

    [Fact]
    public void Resolve_ShouldReturnOnRelativePath_WhenLibraryExistsInCwd()
    {
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), LibRelative);
        File.WriteAllText(filePath, DummyFileContent);

        try
        {
            var resolver = new WindowsLibraryPresenceResolver(_analyzedDirectory);
            var result = resolver.Resolve(LibRelative);

            Assert.Equal(LibraryLocationKind.OnRelativePath, result.LocationKind);
            Assert.Equal(filePath, result.ResolvedPath);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void Resolve_ShouldReturnMissing_WhenLibraryNotFound()
    {
        var resolver = new WindowsLibraryPresenceResolver(_analyzedDirectory);
        var result = resolver.Resolve(LibMissing);

        Assert.Equal(LibraryLocationKind.Missing, result.LocationKind);
        Assert.Null(result.ResolvedPath);
    }

    public void Dispose()
    {
        DeleteIfExists(_analyzedDirectory);
        DeleteIfExists(_envPathDirectory);
    }

    private static void DeleteIfExists(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }
}
