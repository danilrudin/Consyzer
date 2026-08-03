using Consyzer.Core.Models.Resolution;
using Consyzer.Core.Resolvers.Platform;
using Consyzer.Tests.TestSupport.Scopes;
using Consyzer.Tests.TestSupport.FileSystem;
using Consyzer.Tests.TestSupport.Collections;

namespace Consyzer.Tests.Core.Resolvers.Platform;

[Collection(TestCollectionNames.ResolverEnvironment)]
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

    private const string AssemblyDirectoryLibraryName = "assembly-directory-library" + NativeLibraryExtension;
    private const string AbsolutePathLibraryName = "absolute-path-library" + NativeLibraryExtension;
    private const string RelativePathLibraryName = "relative-path-library" + NativeLibraryExtension;
    private const string EnvironmentPathLibraryName = "environment-path-library" + NativeLibraryExtension;
    private const string MissingLibraryName = "missing-library" + NativeLibraryExtension;
    private const string CurrentDirectoryLibraryName = "current-directory-library" + NativeLibraryExtension;
    private const string InferredExtensionLibraryBaseName = "inferred-extension-library";
    private const string NestedHeuristicLibraryName = "nested-heuristic-library" + NativeLibraryExtension;

    private readonly TemporaryDirectory _analyzedDirectory = new(AnalyzedDirectoryPrefix);
    private readonly TemporaryDirectory _envPathDirectory = new(EnvironmentPathDirectoryPrefix);
    private readonly FileInfo _targetFile;
    private readonly WindowsLibraryResolutionResolver _resolver;

    public WindowsLibraryResolutionResolverTests()
    {
        _targetFile = _analyzedDirectory.CreateFile(TargetFileName, TestFileContent);
        _resolver = new WindowsLibraryResolutionResolver(_analyzedDirectory.Path);
    }

    [Fact]
    public void Resolve_ShouldReturnResolvedWithAssemblyDirectory_WhenLibraryExistsNextToTarget()
    {
        if (!OperatingSystem.IsWindows()) return;

        var libraryFile = _analyzedDirectory.CreateFile(AssemblyDirectoryLibraryName, TestFileContent);

        var result = Resolve(AssemblyDirectoryLibraryName);

        AssertResolved(result, AssemblyDirectoryLibraryName, MechanismKind.AssemblyDirectory, libraryFile.FullName);
    }

    [Fact]
    public void Resolve_ShouldComparePhysicalFileNamesCaseInsensitively()
    {
        if (!OperatingSystem.IsWindows()) return;

        const string requestedName = "consyzer_case_insensitive.dll";
        _analyzedDirectory.CreateFile(
            "Consyzer_Case_Insensitive.dll",
            TestFileContent
        );

        var result = Resolve(requestedName);

        AssertResolved(
            result,
            requestedName,
            MechanismKind.AssemblyDirectory,
            Path.Combine(_analyzedDirectory.Path, requestedName)
        );
    }

    [Fact]
    public void Resolve_ShouldReturnInconclusive_WhenSearchPathOverrideIsPresent()
    {
        if (!OperatingSystem.IsWindows()) return;

        _analyzedDirectory.CreateFile(AssemblyDirectoryLibraryName, TestFileContent);

        var result = _resolver.Resolve(new LibraryResolutionContext(
            _targetFile,
            AssemblyDirectoryLibraryName,
            HasDllImportSearchPathOverride: true
        ));

        Assert.Equal(ResolutionState.Inconclusive, result.ResolutionState);
        Assert.Null(result.ResolvedPresence);
        Assert.True(result.NotSimulated.HasFlag(
            NotSimulatedMechanisms.WindowsDotNetSearchPathOverrides
        ));
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

        var result = Resolve(libraryName!);

        AssertResolved(
            result,
            libraryName!,
            MechanismKind.DefaultSystemLocations,
            Path.Combine(systemDirectory, libraryName!)
        );
    }

    [Fact]
    public void Resolve_ShouldReturnResolvedWithDefaultSystemLocations_WhenLibraryExistsInWindowsDirectory()
    {
        if (!OperatingSystem.IsWindows()) return;

        var systemDirectory = Environment.SystemDirectory;
        var windowsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var libraryPath = Directory
            .EnumerateFiles(windowsDirectory, "*.dll", SearchOption.TopDirectoryOnly)
            .FirstOrDefault(path => !File.Exists(
                Path.Combine(systemDirectory, Path.GetFileName(path))
            ));

        Assert.NotNull(libraryPath);

        var result = Resolve(Path.GetFileName(libraryPath));

        AssertResolved(
            result,
            Path.GetFileName(libraryPath),
            MechanismKind.DefaultSystemLocations,
            libraryPath
        );
    }

    [Fact]
    public void Resolve_ShouldReturnResolvedWithEnvironmentOverride_WhenLibraryExistsInPath()
    {
        if (!OperatingSystem.IsWindows()) return;

        var libraryFile = _envPathDirectory.CreateFile(EnvironmentPathLibraryName, TestFileContent);
        var path = _envPathDirectory.Path + Path.PathSeparator + Environment.GetEnvironmentVariable(PathVariableName);

        using var pathScope = new EnvironmentVariableScope(PathVariableName, path);

        var result = Resolve(EnvironmentPathLibraryName);

        AssertResolved(result, EnvironmentPathLibraryName, MechanismKind.EnvironmentOverride, libraryFile.FullName);
    }

    [Fact]
    public void Resolve_ShouldAppendDllExtension_WhenLibraryNameHasNoExtension()
    {
        if (!OperatingSystem.IsWindows()) return;

        var libraryFile = _analyzedDirectory.CreateFile(
            InferredExtensionLibraryBaseName + NativeLibraryExtension,
            TestFileContent
        );

        var result = Resolve(InferredExtensionLibraryBaseName);

        AssertResolved(
            result,
            InferredExtensionLibraryBaseName,
            MechanismKind.AssemblyDirectory,
            libraryFile.FullName
        );
    }

    [Fact]
    public void Resolve_ShouldCompleteSearchForExactNameBeforeTryingDllVariation()
    {
        if (!OperatingSystem.IsWindows()) return;

        const string requestedName = "consyzer_variation_precedence.native";
        using var currentDirectory = new TemporaryDirectory("consyzer-variation-current-");
        var exactLibrary = currentDirectory.CreateFile(requestedName, TestFileContent);
        _analyzedDirectory.CreateFile(requestedName + NativeLibraryExtension, TestFileContent);
        using var currentDirectoryScope = new CurrentDirectoryScope(currentDirectory.Path);

        var result = Resolve(requestedName);

        AssertResolved(
            result,
            requestedName,
            MechanismKind.CurrentDirectory,
            exactLibrary.FullName
        );
    }

    [Fact]
    public void Resolve_ShouldNotTreatExtensionlessFileAsPhysicalDll_WhenNameHasNoExtension()
    {
        if (!OperatingSystem.IsWindows()) return;

        const string requestedName = "consyzer_extensionless_file";
        _analyzedDirectory.CreateFile(requestedName, TestFileContent);

        var result = Resolve(requestedName);

        Assert.Equal(ResolutionState.Inconclusive, result.ResolutionState);
        Assert.Null(result.ResolvedPresence);
    }

    [Fact]
    public void Resolve_ShouldUseDllFile_WhenAbsolutePathHasNoExtension()
    {
        if (!OperatingSystem.IsWindows()) return;

        const string requestedBaseName = "consyzer_absolute_without_extension";
        using var directory = new TemporaryDirectory(AbsolutePathDirectoryPrefix);
        var libraryFile = directory.CreateFile(
            requestedBaseName + NativeLibraryExtension,
            TestFileContent
        );
        var requestedPath = Path.Combine(directory.Path, requestedBaseName);

        var result = Resolve(requestedPath);

        AssertResolved(
            result,
            requestedPath,
            MechanismKind.ExplicitPath,
            libraryFile.FullName
        );
    }

    [Fact]
    public void Resolve_ShouldUseDllVariationForRelativeExplicitPath()
    {
        if (!OperatingSystem.IsWindows()) return;

        const string requestedBaseName = "consyzer_relative_without_extension";
        using var relativeDirectory = new TemporaryDirectory(
            RelativePathDirectoryPrefix,
            Directory.GetCurrentDirectory()
        );
        var libraryFile = relativeDirectory.CreateFile(
            requestedBaseName + NativeLibraryExtension,
            TestFileContent
        );
        var requestedPath = Path.Combine(relativeDirectory.Directory.Name, requestedBaseName);

        var result = Resolve(requestedPath);

        AssertResolved(
            result,
            requestedPath,
            MechanismKind.ExplicitPath,
            libraryFile.FullName
        );
    }

    [Fact]
    public void Resolve_ShouldReturnResolvedWithCurrentDirectory_WhenLibraryExistsThere()
    {
        if (!OperatingSystem.IsWindows()) return;

        using var currentDirectory = new TemporaryDirectory(RelativePathDirectoryPrefix);
        var libraryFile = currentDirectory.CreateFile(CurrentDirectoryLibraryName, TestFileContent);
        using var currentDirectoryScope = new CurrentDirectoryScope(currentDirectory.Path);

        var result = Resolve(CurrentDirectoryLibraryName);

        AssertResolved(
            result,
            CurrentDirectoryLibraryName,
            MechanismKind.CurrentDirectory,
            libraryFile.FullName
        );
    }

    [Fact]
    public void Resolve_ShouldReturnResolvedWithAssemblyDirectory_WhenTargetIsNested()
    {
        if (!OperatingSystem.IsWindows()) return;

        using var nestedDirectory = new TemporaryDirectory(
            "consyzer-nested-",
            _analyzedDirectory.Path
        );
        var targetFile = nestedDirectory.CreateFile(TargetFileName, TestFileContent);
        var libraryFile = nestedDirectory.CreateFile(NestedHeuristicLibraryName, TestFileContent);

        var result = _resolver.Resolve(
            new LibraryResolutionContext(targetFile, NestedHeuristicLibraryName)
        );

        AssertResolved(
            result,
            NestedHeuristicLibraryName,
            MechanismKind.AssemblyDirectory,
            libraryFile.FullName,
            targetFile.FullName
        );
    }

    [Fact]
    public void Resolve_ShouldReportAnalysisDirectoryCandidateAsHeuristic_WhenTargetIsNested()
    {
        if (!OperatingSystem.IsWindows()) return;

        using var nestedDirectory = new TemporaryDirectory(
            "consyzer-nested-",
            _analyzedDirectory.Path
        );
        var targetFile = nestedDirectory.CreateFile(TargetFileName, TestFileContent);
        var libraryFile = _analyzedDirectory.CreateFile(
            NestedHeuristicLibraryName,
            TestFileContent
        );

        var result = _resolver.Resolve(
            new LibraryResolutionContext(targetFile, NestedHeuristicLibraryName)
        );

        Assert.Equal(ResolutionState.Inconclusive, result.ResolutionState);
        Assert.Null(result.ResolvedPresence);
        Assert.Contains(Path.GetFullPath(libraryFile.FullName), result.HeuristicCandidates);
        Assert.NotEqual(NotSimulatedMechanisms.None, result.NotSimulated);
    }

    [Fact]
    public void Resolve_ShouldNotAppendDllExtension_WhenNameEndsWithDot()
    {
        if (!OperatingSystem.IsWindows()) return;

        const string requestedName = "consyzer_trailing_dot.";
        _analyzedDirectory.CreateFile(requestedName + ".dll", TestFileContent);

        var result = Resolve(requestedName);

        Assert.Equal(ResolutionState.Inconclusive, result.ResolutionState);
        Assert.Null(result.ResolvedPresence);
    }

    [Fact]
    public void Resolve_ShouldReturnResolvedWithExplicitPath_WhenLibraryExistsAtAbsolutePath()
    {
        if (!OperatingSystem.IsWindows()) return;

        using var directory = new TemporaryDirectory(AbsolutePathDirectoryPrefix);
        var libraryFile = directory.CreateFile(AbsolutePathLibraryName, TestFileContent);

        var result = Resolve(libraryFile.FullName);

        AssertResolved(result, libraryFile.FullName, MechanismKind.ExplicitPath, libraryFile.FullName);
    }

    [Fact]
    public void Resolve_ShouldReturnResolvedForAbsolutePath_WhenSearchPathOverrideIsPresent()
    {
        if (!OperatingSystem.IsWindows()) return;

        using var directory = new TemporaryDirectory(AbsolutePathDirectoryPrefix);
        var libraryFile = directory.CreateFile(AbsolutePathLibraryName, TestFileContent);

        var result = _resolver.Resolve(new LibraryResolutionContext(
            _targetFile,
            libraryFile.FullName,
            HasDllImportSearchPathOverride: true
        ));

        AssertResolved(
            result,
            libraryFile.FullName,
            MechanismKind.ExplicitPath,
            libraryFile.FullName
        );
    }

    [Fact]
    public void Resolve_ShouldReturnResolvedWithExplicitPath_WhenLibraryExistsAtRelativePath()
    {
        if (!OperatingSystem.IsWindows()) return;

        using var relativeDirectory = new TemporaryDirectory(RelativePathDirectoryPrefix, Directory.GetCurrentDirectory());
        var libraryFile = relativeDirectory.CreateFile(RelativePathLibraryName, TestFileContent);
        var relativeRequestPath = Path.Combine(relativeDirectory.Directory.Name, RelativePathLibraryName);

        var result = Resolve(relativeRequestPath);

        AssertResolved(result, relativeRequestPath, MechanismKind.ExplicitPath, libraryFile.FullName);
    }

    [Fact]
    public void Resolve_ShouldReturnInconclusiveForRelativePath_WhenSearchPathOverrideIsPresent()
    {
        if (!OperatingSystem.IsWindows()) return;

        using var relativeDirectory = new TemporaryDirectory(
            RelativePathDirectoryPrefix,
            Directory.GetCurrentDirectory()
        );
        relativeDirectory.CreateFile(RelativePathLibraryName, TestFileContent);
        var relativeRequestPath = Path.Combine(
            relativeDirectory.Directory.Name,
            RelativePathLibraryName
        );

        var result = _resolver.Resolve(new LibraryResolutionContext(
            _targetFile,
            relativeRequestPath,
            HasDllImportSearchPathOverride: true
        ));

        Assert.Equal(ResolutionState.Inconclusive, result.ResolutionState);
        Assert.Null(result.ResolvedPresence);
        Assert.True(result.NotSimulated.HasFlag(
            NotSimulatedMechanisms.WindowsDotNetSearchPathOverrides
        ));
    }

    [Fact]
    public void Resolve_ShouldNotFallbackToPath_WhenRelativeExplicitPathDoesNotExist()
    {
        if (!OperatingSystem.IsWindows()) return;

        const string directoryName = "native";
        using var currentDirectory = new TemporaryDirectory(
            "consyzer-relative-missing-"
        );
        var environmentSubdirectory = Directory.CreateDirectory(
            Path.Combine(_envPathDirectory.Path, directoryName)
        );
        File.WriteAllText(
            Path.Combine(environmentSubdirectory.FullName, RelativePathLibraryName),
            TestFileContent
        );

        using var currentDirectoryScope = new CurrentDirectoryScope(currentDirectory.Path);
        using var pathScope = new EnvironmentVariableScope(
            PathVariableName,
            _envPathDirectory.Path
        );

        var requestedPath = Path.Combine(directoryName, RelativePathLibraryName);
        var result = Resolve(requestedPath);

        Assert.Equal(ResolutionState.Missing, result.ResolutionState);
        Assert.Null(result.ResolvedPresence);
        Assert.Empty(result.HeuristicCandidates);
        Assert.Equal(NotSimulatedMechanisms.None, result.NotSimulated);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Resolve_ShouldReturnMissing_WhenExplicitPathDoesNotExist(
        bool hasDllImportSearchPathOverride
    )
    {
        if (!OperatingSystem.IsWindows()) return;

        using var directory = new TemporaryDirectory(AbsolutePathDirectoryPrefix);
        var missingPath = Path.Combine(directory.Path, "consyzer_missing_explicit.dll");

        var result = _resolver.Resolve(new LibraryResolutionContext(
            _targetFile,
            missingPath,
            hasDllImportSearchPathOverride
        ));

        Assert.Equal(ResolutionState.Missing, result.ResolutionState);
        Assert.Null(result.ResolvedPresence);
        Assert.Empty(result.HeuristicCandidates);
        Assert.Equal(NotSimulatedMechanisms.None, result.NotSimulated);
    }

    [Fact]
    public void Resolve_ShouldReturnInconclusive_WhenLibraryIsNotFound()
    {
        if (!OperatingSystem.IsWindows()) return;

        var result = Resolve(MissingLibraryName);

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

    private LibraryResolution Resolve(string libraryName)
        => _resolver.Resolve(new LibraryResolutionContext(_targetFile, libraryName));

    private void AssertResolved(
        LibraryResolution result,
        string libraryName,
        MechanismKind mechanismKind,
        string resolvedPath,
        string? targetPath = null
    )
    {
        Assert.Equal(targetPath ?? _targetFile.FullName, result.TargetPath);
        Assert.Equal(libraryName, result.LibraryName);
        Assert.Equal(ResolutionState.Resolved, result.ResolutionState);
        Assert.NotNull(result.ResolvedPresence);
        Assert.Equal(mechanismKind, result.ResolvedPresence!.MechanismKind);
        Assert.Equal(Path.GetFullPath(resolvedPath), result.ResolvedPresence.Path);
        Assert.Empty(result.HeuristicCandidates);
        Assert.Equal(NotSimulatedMechanisms.None, result.NotSimulated);
    }
}
