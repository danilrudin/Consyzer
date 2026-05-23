using Consyzer.Core.Models.Resolution;
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
    private readonly FileInfo _targetFile;

    public WindowsLibraryResolutionResolverTests()
    {
        Directory.CreateDirectory(_analyzedDirectory);
        Directory.CreateDirectory(_envPathDirectory);

        _targetFile = new FileInfo(Path.Combine(_analyzedDirectory, "Target.dll"));
        File.WriteAllText(_targetFile.FullName, DummyFileContent);
    }

    [Fact]
    public void Resolve_ShouldReturnResolvedWithApplicationDirectory_WhenLibraryExistsInAnalyzedDirectory()
    {
        var libraryPath = Path.Combine(_analyzedDirectory, LibAnalyzed);
        File.WriteAllText(libraryPath, DummyFileContent);

        try
        {
            var resolver = new WindowsLibraryResolutionResolver(_analyzedDirectory);
            var result = Resolve(resolver, LibAnalyzed);

            Assert.Equal(_targetFile.FullName, result.TargetPath);
            Assert.Equal(LibAnalyzed, result.LibraryName);
            Assert.Equal("Windows", result.Platform);
            Assert.Equal(ResolutionState.Resolved, result.ResolutionState);
            Assert.NotNull(result.ResolvedPresence);
            Assert.Equal(MechanismKind.ApplicationDirectory, result.ResolvedPresence!.MechanismKind);
            Assert.Equal(Path.GetFullPath(libraryPath), result.ResolvedPresence.Path);
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
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var systemDirectory = Environment.SystemDirectory;
        var libraryName = Directory.GetFiles(systemDirectory)
            .Select(Path.GetFileName)
            .FirstOrDefault(name => name?.EndsWith(DllExtension, StringComparison.OrdinalIgnoreCase) is true);

        Assert.False(string.IsNullOrWhiteSpace(libraryName));

        var resolver = new WindowsLibraryResolutionResolver(_analyzedDirectory);
        var result = Resolve(resolver, libraryName!);

        Assert.Equal(_targetFile.FullName, result.TargetPath);
        Assert.Equal(libraryName, result.LibraryName);
        Assert.Equal("Windows", result.Platform);
        Assert.Equal(ResolutionState.Resolved, result.ResolutionState);
        Assert.NotNull(result.ResolvedPresence);
        Assert.Equal(MechanismKind.DefaultSystemLocations, result.ResolvedPresence!.MechanismKind);
        Assert.Equal(
            Path.GetFullPath(Path.Combine(systemDirectory, libraryName!)),
            result.ResolvedPresence.Path
        );
        Assert.Empty(result.HeuristicCandidates);
        Assert.Equal(NotSimulatedMechanisms.None, result.NotSimulated);
    }

    [Fact]
    public void Resolve_ShouldReturnResolvedWithEnvironmentOverride_WhenLibraryExistsInPath()
    {
        var libraryPath = Path.Combine(_envPathDirectory, LibEnvironment);
        File.WriteAllText(libraryPath, DummyFileContent);

        var originalPath = Environment.GetEnvironmentVariable(PathVariableName);
        Environment.SetEnvironmentVariable(
            PathVariableName,
            _envPathDirectory + Path.PathSeparator + originalPath
        );

        try
        {
            var resolver = new WindowsLibraryResolutionResolver(_analyzedDirectory);
            var result = Resolve(resolver, LibEnvironment);

            Assert.Equal(_targetFile.FullName, result.TargetPath);
            Assert.Equal(LibEnvironment, result.LibraryName);
            Assert.Equal("Windows", result.Platform);
            Assert.Equal(ResolutionState.Resolved, result.ResolutionState);
            Assert.NotNull(result.ResolvedPresence);
            Assert.Equal(MechanismKind.EnvironmentOverride, result.ResolvedPresence!.MechanismKind);
            Assert.Equal(Path.GetFullPath(libraryPath), result.ResolvedPresence.Path);
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
            var result = Resolve(resolver, libraryPath);

            Assert.Equal(_targetFile.FullName, result.TargetPath);
            Assert.Equal(libraryPath, result.LibraryName);
            Assert.Equal("Windows", result.Platform);
            Assert.Equal(ResolutionState.Resolved, result.ResolutionState);
            Assert.NotNull(result.ResolvedPresence);
            Assert.Equal(MechanismKind.ExplicitPath, result.ResolvedPresence!.MechanismKind);
            Assert.Equal(Path.GetFullPath(libraryPath), result.ResolvedPresence.Path);
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
            var result = Resolve(resolver, relativeRequestPath);

            Assert.Equal(_targetFile.FullName, result.TargetPath);
            Assert.Equal(relativeRequestPath, result.LibraryName);
            Assert.Equal("Windows", result.Platform);
            Assert.Equal(ResolutionState.Resolved, result.ResolutionState);
            Assert.NotNull(result.ResolvedPresence);
            Assert.Equal(MechanismKind.ExplicitPath, result.ResolvedPresence!.MechanismKind);
            Assert.Equal(Path.GetFullPath(relativeLibraryPath), result.ResolvedPresence.Path);
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
        var result = Resolve(resolver, LibMissing);

        Assert.Equal(_targetFile.FullName, result.TargetPath);
        Assert.Equal(LibMissing, result.LibraryName);
        Assert.Equal("Windows", result.Platform);
        Assert.Equal(ResolutionState.Inconclusive, result.ResolutionState);
        Assert.Null(result.ResolvedPresence);
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

    private LibraryResolutionResult Resolve(WindowsLibraryResolutionResolver resolver, string libraryName)
        => resolver.Resolve(new LibraryResolutionContext(_targetFile, libraryName));

    private static void DeleteIfExists(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }
}
