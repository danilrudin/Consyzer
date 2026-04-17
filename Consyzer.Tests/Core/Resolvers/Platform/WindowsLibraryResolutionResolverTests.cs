using Xunit;
using Consyzer.Core.Models;
using Consyzer.Core.Resolvers.Platform;

namespace Consyzer.Tests.Core.Resolvers.Platform;

public sealed class WindowsLibraryResolutionResolverTests : IDisposable
{
    private const string DummyFileContent = "dummy";
    private const string DllExtension = ".dll";
    private const string LibAnalyzed = "testlib-analyzed" + DllExtension;
    private const string LibAbsolute = "testlib-abs" + DllExtension;
    private const string LibRelative = "testlib-rel" + DllExtension;
    private const string LibEnvironment = "testlib-env" + DllExtension;
    private const string LibMissing = "nonexistent-lib" + DllExtension;

    private const string PathVariableName = "PATH";

    private readonly string _analyzedDirectory = Path.Combine(Path.GetTempPath(), "analyzed-" + Guid.NewGuid());
    private readonly string _envPathDirectory = Path.Combine(Path.GetTempPath(), "envpath-" + Guid.NewGuid());

    public WindowsLibraryResolutionResolverTests()
    {
        Directory.CreateDirectory(_analyzedDirectory);
        Directory.CreateDirectory(_envPathDirectory);
    }

    [Fact]
    public void Resolve_ShouldReturnResolvedWithApplicationDirectory_WhenLibraryExistsInAnalyzedDirectory()
    {
        var libraryPath = Path.Combine(_analyzedDirectory, LibAnalyzed);
        File.WriteAllText(libraryPath, DummyFileContent);

        try
        {
            var resolver = new WindowsLibraryResolutionResolver(_analyzedDirectory);
            var result = resolver.Resolve(LibAnalyzed);

            Assert.Equal(ResolutionState.Resolved, result.State);
            Assert.NotNull(result.Resolved);
            Assert.Equal(MechanismKind.ApplicationDirectory, result.Resolved!.MechanismKind);
            Assert.Equal(Path.GetFullPath(libraryPath), result.Resolved.Path);
            Assert.Empty(result.HeuristicCandidates);
            Assert.Equal(NotSimulatedMechanisms.None, result.NotSimulated);
        }
        finally
        {
            File.Delete(libraryPath);
        }
    }

    [Fact]
    public void Resolve_ShouldReturnResolvedWithDefaultSystemLocations_WhenLibraryExistsInSystemDirectory()
    {
        var systemDirectory = Environment.SystemDirectory;
        var libraryName = Directory.GetFiles(systemDirectory)
            .Select(Path.GetFileName)
            .FirstOrDefault(name => name?.EndsWith(DllExtension, StringComparison.OrdinalIgnoreCase) is true
        );

        Assert.False(string.IsNullOrWhiteSpace(libraryName));

        var resolver = new WindowsLibraryResolutionResolver(_analyzedDirectory);
        var result = resolver.Resolve(libraryName!);

        Assert.Equal(ResolutionState.Resolved, result.State);
        Assert.NotNull(result.Resolved);
        Assert.Equal(MechanismKind.DefaultSystemLocations, result.Resolved!.MechanismKind);
        Assert.Equal(
            Path.GetFullPath(Path.Combine(systemDirectory, libraryName!)),
            result.Resolved.Path
        );
        Assert.Empty(result.HeuristicCandidates);
        Assert.Equal(NotSimulatedMechanisms.None, result.NotSimulated);
    }

    [Fact]
    public void Resolve_ShouldReturnResolvedWithEnvironmentOverride_WhenLibraryExistsInPath()
    {
        var libraryPath = Path.Combine(_envPathDirectory, LibEnvironment);
        File.WriteAllText(libraryPath, DummyFileContent);

        var originalPath = Environment.GetEnvironmentVariable(PathVariableName) ?? string.Empty;
        Environment.SetEnvironmentVariable(
            PathVariableName,
            _envPathDirectory + Path.PathSeparator + originalPath
        );

        try
        {
            var resolver = new WindowsLibraryResolutionResolver(_analyzedDirectory);
            var result = resolver.Resolve(LibEnvironment);

            Assert.Equal(ResolutionState.Resolved, result.State);
            Assert.NotNull(result.Resolved);
            Assert.Equal(MechanismKind.EnvironmentOverride, result.Resolved!.MechanismKind);
            Assert.Equal(Path.GetFullPath(libraryPath), result.Resolved.Path);
            Assert.Empty(result.HeuristicCandidates);
            Assert.Equal(NotSimulatedMechanisms.None, result.NotSimulated);
        }
        finally
        {
            Environment.SetEnvironmentVariable(PathVariableName, originalPath);
            File.Delete(libraryPath);
        }
    }

    [Fact]
    public void Resolve_ShouldReturnResolvedWithExplicitPath_WhenLibraryExistsAtAbsolutePath()
    {
        var libraryPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}-{LibAbsolute}");
        File.WriteAllText(libraryPath, DummyFileContent);

        try
        {
            var resolver = new WindowsLibraryResolutionResolver(_analyzedDirectory);
            var result = resolver.Resolve(libraryPath);

            Assert.Equal(ResolutionState.Resolved, result.State);
            Assert.NotNull(result.Resolved);
            Assert.Equal(MechanismKind.ExplicitPath, result.Resolved!.MechanismKind);
            Assert.Equal(Path.GetFullPath(libraryPath), result.Resolved.Path);
            Assert.Empty(result.HeuristicCandidates);
            Assert.Equal(NotSimulatedMechanisms.None, result.NotSimulated);
        }
        finally
        {
            File.Delete(libraryPath);
        }
    }

    [Fact]
    public void Resolve_ShouldReturnResolvedWithExplicitPath_WhenLibraryExistsAtRelativePath()
    {
        var relativeDirectoryName = "relative-" + Guid.NewGuid();
        var relativeDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), relativeDirectoryName);
        var relativeLibraryPath = Path.Combine(relativeDirectoryPath, LibRelative);
        var relativeRequestPath = Path.Combine(relativeDirectoryName, LibRelative);

        Directory.CreateDirectory(relativeDirectoryPath);
        File.WriteAllText(relativeLibraryPath, DummyFileContent);

        try
        {
            var resolver = new WindowsLibraryResolutionResolver(_analyzedDirectory);
            var result = resolver.Resolve(relativeRequestPath);

            Assert.Equal(ResolutionState.Resolved, result.State);
            Assert.NotNull(result.Resolved);
            Assert.Equal(MechanismKind.ExplicitPath, result.Resolved!.MechanismKind);
            Assert.Equal(Path.GetFullPath(relativeLibraryPath), result.Resolved.Path);
            Assert.Empty(result.HeuristicCandidates);
            Assert.Equal(NotSimulatedMechanisms.None, result.NotSimulated);
        }
        finally
        {
            DeleteIfExists(relativeDirectoryPath);
        }
    }

    [Fact]
    public void Resolve_ShouldReturnInconclusive_WhenLibraryIsNotFound()
    {
        var resolver = new WindowsLibraryResolutionResolver(_analyzedDirectory);
        var result = resolver.Resolve(LibMissing);

        Assert.Equal(ResolutionState.Inconclusive, result.State);
        Assert.Null(result.Resolved);
        Assert.Empty(result.HeuristicCandidates);
        Assert.NotEqual(NotSimulatedMechanisms.None, result.NotSimulated);
        Assert.True(result.NotSimulated.HasFlag(NotSimulatedMechanisms.WindowsKnownDlls));
        Assert.True(result.NotSimulated.HasFlag(NotSimulatedMechanisms.WindowsSxS));
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