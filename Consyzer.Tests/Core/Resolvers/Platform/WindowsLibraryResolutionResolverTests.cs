using Consyzer.Core.Models.Resolution;
using Consyzer.Core.Resolvers.Platform;
using Consyzer.Tests.TestSupport.Scopes;
using Consyzer.Tests.TestSupport.FileSystem;

namespace Consyzer.Tests.Core.Resolvers.Platform;

public sealed class WindowsLibraryResolutionResolverTests : IDisposable
{
    private const string PathVariableName = "PATH";

    private const string TestFileContent = "test file";
    private const string TargetFileName = "Target.dll";
    private const string NativeLibraryExtension = ".dll";

    private const string AnalyzedDirectoryPrefix = "consyzer-analyzed-";
    private const string EnvironmentPathDirectoryPrefix = "consyzer-envpath-";
    private const string AbsolutePathDirectoryPrefix = "consyzer-absolute-";
    private const string RelativePathDirectoryPrefix = "consyzer-relative-";

    private const string ApplicationDirectoryLibraryName = "application-directory-library" + NativeLibraryExtension;
    private const string AbsolutePathLibraryName = "absolute-path-library" + NativeLibraryExtension;
    private const string RelativePathLibraryName = "relative-path-library" + NativeLibraryExtension;
    private const string EnvironmentPathLibraryName = "environment-path-library" + NativeLibraryExtension;
    private const string MissingLibraryName = "missing-library" + NativeLibraryExtension;

    private readonly TemporaryDirectory _analyzedDirectory = new(AnalyzedDirectoryPrefix);
    private readonly TemporaryDirectory _envPathDirectory = new(EnvironmentPathDirectoryPrefix);
    private readonly FileInfo _targetFile;

    public WindowsLibraryResolutionResolverTests()
    {
        _targetFile = _analyzedDirectory.CreateFile(TargetFileName, TestFileContent);
    }

    [Fact]
    public void Resolve_ShouldReturnResolvedWithApplicationDirectory_WhenLibraryExistsInAnalyzedDirectory()
    {
        var libraryFile = _analyzedDirectory.CreateFile(ApplicationDirectoryLibraryName, TestFileContent);

        var resolver = new WindowsLibraryResolutionResolver(_analyzedDirectory.Path);
        var result = Resolve(resolver, ApplicationDirectoryLibraryName);

        AssertResolved(result, ApplicationDirectoryLibraryName, MechanismKind.ApplicationDirectory, libraryFile.FullName);
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
            .FirstOrDefault(name => name?.EndsWith(NativeLibraryExtension, StringComparison.OrdinalIgnoreCase) is true);

        Assert.False(string.IsNullOrWhiteSpace(libraryName));

        var resolver = new WindowsLibraryResolutionResolver(_analyzedDirectory.Path);
        var result = Resolve(resolver, libraryName!);

        AssertResolved(
            result,
            libraryName!,
            MechanismKind.DefaultSystemLocations,
            Path.Combine(systemDirectory, libraryName!)
        );
    }

    [Fact]
    public void Resolve_ShouldReturnResolvedWithEnvironmentOverride_WhenLibraryExistsInPath()
    {
        var libraryFile = _envPathDirectory.CreateFile(EnvironmentPathLibraryName, TestFileContent);
        var path = _envPathDirectory.Path + Path.PathSeparator + Environment.GetEnvironmentVariable(PathVariableName);

        using var pathScope = new EnvironmentVariableScope(PathVariableName, path);

        var resolver = new WindowsLibraryResolutionResolver(_analyzedDirectory.Path);
        var result = Resolve(resolver, EnvironmentPathLibraryName);

        AssertResolved(result, EnvironmentPathLibraryName, MechanismKind.EnvironmentOverride, libraryFile.FullName);
    }

    [Fact]
    public void Resolve_ShouldReturnResolvedWithExplicitPath_WhenLibraryExistsAtAbsolutePath()
    {
        using var directory = new TemporaryDirectory(AbsolutePathDirectoryPrefix);
        var libraryFile = directory.CreateFile(AbsolutePathLibraryName, TestFileContent);

        var resolver = new WindowsLibraryResolutionResolver(_analyzedDirectory.Path);
        var result = Resolve(resolver, libraryFile.FullName);

        AssertResolved(result, libraryFile.FullName, MechanismKind.ExplicitPath, libraryFile.FullName);
    }

    [Fact]
    public void Resolve_ShouldReturnResolvedWithExplicitPath_WhenLibraryExistsAtRelativePath()
    {
        using var relativeDirectory = new TemporaryDirectory(RelativePathDirectoryPrefix, Directory.GetCurrentDirectory());
        var libraryFile = relativeDirectory.CreateFile(RelativePathLibraryName, TestFileContent);
        var relativeRequestPath = Path.Combine(relativeDirectory.Directory.Name, RelativePathLibraryName);

        var resolver = new WindowsLibraryResolutionResolver(_analyzedDirectory.Path);
        var result = Resolve(resolver, relativeRequestPath);

        AssertResolved(result, relativeRequestPath, MechanismKind.ExplicitPath, libraryFile.FullName);
    }

    [Fact]
    public void Resolve_ShouldReturnInconclusive_WhenLibraryIsNotFound()
    {
        var resolver = new WindowsLibraryResolutionResolver(_analyzedDirectory.Path);
        var result = Resolve(resolver, MissingLibraryName);

        Assert.Equal(_targetFile.FullName, result.TargetPath);
        Assert.Equal(MissingLibraryName, result.LibraryName);
        Assert.Equal(ResolutionState.Inconclusive, result.ResolutionState);
        Assert.Null(result.ResolvedPresence);
        Assert.Empty(result.HeuristicCandidates);
        Assert.NotEqual(NotSimulatedMechanisms.None, result.NotSimulated);
        Assert.True(result.NotSimulated.HasFlag(NotSimulatedMechanisms.WindowsKnownDlls));
        Assert.True(result.NotSimulated.HasFlag(NotSimulatedMechanisms.WindowsSxS));
    }

    public void Dispose()
    {
        _analyzedDirectory.Dispose();
        _envPathDirectory.Dispose();
    }

    private LibraryResolution Resolve(WindowsLibraryResolutionResolver resolver, string libraryName)
        => resolver.Resolve(new LibraryResolutionContext(_targetFile, libraryName));

    private void AssertResolved(
        LibraryResolution result,
        string libraryName,
        MechanismKind mechanismKind,
        string resolvedPath
    )
    {
        Assert.Equal(_targetFile.FullName, result.TargetPath);
        Assert.Equal(libraryName, result.LibraryName);
        Assert.Equal(ResolutionState.Resolved, result.ResolutionState);
        Assert.NotNull(result.ResolvedPresence);
        Assert.Equal(mechanismKind, result.ResolvedPresence!.MechanismKind);
        Assert.Equal(Path.GetFullPath(resolvedPath), result.ResolvedPresence.Path);
        Assert.Empty(result.HeuristicCandidates);
        Assert.Equal(NotSimulatedMechanisms.None, result.NotSimulated);
    }
}
