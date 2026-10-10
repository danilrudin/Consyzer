using Consyzer.Core.Models.Resolution;
using Consyzer.Core.Resolvers.Platform;
using Consyzer.Tests.TestSupport.Scopes;
using Consyzer.Tests.TestSupport;
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

    [WindowsFact]
    public void Resolve_ShouldReturnResolvedWithAssemblyDirectory_WhenLibraryExistsNextToTarget()
    {
        var libraryFile = _analyzedDirectory.CreateFile(AssemblyDirectoryLibraryName, TestFileContent);

        var result = Resolve(AssemblyDirectoryLibraryName);

        AssertResolved(result, AssemblyDirectoryLibraryName, MechanismKind.AssemblyDirectory, libraryFile.FullName);
    }

    [WindowsFact]
    public void Resolve_ShouldComparePhysicalFileNamesCaseInsensitively()
    {
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

    [WindowsFact]
    public void Resolve_ShouldReturnInconclusive_WhenSearchPathOverrideIsPresent()
    {
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

    [WindowsFact]
    public void Resolve_ShouldPreferSystemDirectoryOverCurrentDirectoryAndPath()
    {
        var systemDirectory = Environment.SystemDirectory;
        var libraryName = Directory.GetFiles(systemDirectory)
            .Select(Path.GetFileName)
            .FirstOrDefault(name => name?.EndsWith(NativeLibraryExtension, StringComparison.OrdinalIgnoreCase) is true);

        Assert.False(string.IsNullOrWhiteSpace(libraryName));

        using var currentDirectory = new TemporaryDirectory(RelativePathDirectoryPrefix);
        currentDirectory.CreateFile(libraryName!, TestFileContent);
        _envPathDirectory.CreateFile(libraryName!, TestFileContent);
        using var currentDirectoryScope = new CurrentDirectoryScope(currentDirectory.Path);
        using var pathScope = new EnvironmentVariableScope(PathVariableName, _envPathDirectory.Path);

        var result = Resolve(libraryName!);

        AssertResolved(
            result,
            libraryName!,
            MechanismKind.DefaultSystemLocations,
            Path.Combine(systemDirectory, libraryName!)
        );
    }

    [WindowsFact]
    public void Resolve_ShouldReturnResolvedWithDefaultSystemLocations_WhenLibraryExistsInWindowsDirectory()
    {
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

    [WindowsFact]
    public void Resolve_ShouldReturnResolvedWithEnvironmentOverride_WhenLibraryExistsInPath()
    {
        var libraryFile = _envPathDirectory.CreateFile(EnvironmentPathLibraryName, TestFileContent);
        var path = _envPathDirectory.Path + Path.PathSeparator + Environment.GetEnvironmentVariable(PathVariableName);

        using var pathScope = new EnvironmentVariableScope(PathVariableName, path);

        var result = Resolve(EnvironmentPathLibraryName);

        AssertResolved(result, EnvironmentPathLibraryName, MechanismKind.EnvironmentOverride, libraryFile.FullName);
    }

    [WindowsFact]
    public void Resolve_ShouldAppendDllExtension_WhenLibraryNameHasNoExtension()
    {
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

    [WindowsFact]
    public void Resolve_ShouldCompleteSearchForExactNameBeforeTryingDllVariation()
    {
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

    [WindowsFact]
    public void Resolve_ShouldNotTreatExtensionlessFileAsPhysicalDll_WhenNameHasNoExtension()
    {
        const string requestedName = "consyzer_extensionless_file";
        _analyzedDirectory.CreateFile(requestedName, TestFileContent);

        var result = Resolve(requestedName);

        Assert.Equal(ResolutionState.Inconclusive, result.ResolutionState);
        Assert.Null(result.ResolvedPresence);
    }

    [WindowsFact]
    public void Resolve_ShouldUseDllFile_WhenAbsolutePathHasNoExtension()
    {
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

    [WindowsFact]
    public void Resolve_ShouldUseDllVariationForRelativeExplicitPath()
    {
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

    [WindowsFact]
    public void Resolve_ShouldReturnResolvedWithCurrentDirectory_WhenLibraryExistsThere()
    {
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

    [WindowsFact]
    public void Resolve_ShouldReturnResolvedWithAssemblyDirectory_WhenTargetIsNested()
    {
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

    [WindowsFact]
    public void Resolve_ShouldReportAnalysisDirectoryCandidateAsHeuristic_WhenTargetIsNested()
    {
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

    [WindowsFact]
    public void Resolve_ShouldNotAppendDllExtension_WhenNameEndsWithDot()
    {
        const string requestedName = "consyzer_trailing_dot.";
        _analyzedDirectory.CreateFile(requestedName + ".dll", TestFileContent);

        var result = Resolve(requestedName);

        Assert.Equal(ResolutionState.Inconclusive, result.ResolutionState);
        Assert.Null(result.ResolvedPresence);
    }

    [WindowsTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Resolve_ShouldReturnResolvedWithExplicitPath_WhenLibraryExistsAtAbsolutePath(
        bool hasDllImportSearchPathOverride
    )
    {
        using var directory = new TemporaryDirectory(AbsolutePathDirectoryPrefix);
        var libraryFile = directory.CreateFile(AbsolutePathLibraryName, TestFileContent);

        var result = _resolver.Resolve(new LibraryResolutionContext(
            _targetFile,
            libraryFile.FullName,
            hasDllImportSearchPathOverride
        ));

        AssertResolved(
            result,
            libraryFile.FullName,
            MechanismKind.ExplicitPath,
            libraryFile.FullName
        );
    }

    [WindowsFact]
    public void Resolve_ShouldReturnResolvedWithExplicitPath_WhenLibraryExistsAtRelativePath()
    {
        using var relativeDirectory = new TemporaryDirectory(RelativePathDirectoryPrefix, Directory.GetCurrentDirectory());
        var libraryFile = relativeDirectory.CreateFile(RelativePathLibraryName, TestFileContent);
        var relativeRequestPath = Path.Combine(relativeDirectory.Directory.Name, RelativePathLibraryName);

        var result = Resolve(relativeRequestPath);

        AssertResolved(result, relativeRequestPath, MechanismKind.ExplicitPath, libraryFile.FullName);
    }

    [WindowsFact]
    public void Resolve_ShouldSearchAssemblyDirectoryForRelativePath()
    {
        const string directoryName = "native";
        var subdirectory = Directory.CreateDirectory(
            Path.Combine(_analyzedDirectory.Path, directoryName)
        );
        var libraryPath = Path.Combine(subdirectory.FullName, RelativePathLibraryName);
        File.WriteAllText(libraryPath, TestFileContent);

        var requestedPath = Path.Combine(directoryName, RelativePathLibraryName);
        var result = Resolve(requestedPath);

        AssertResolved(
            result,
            requestedPath,
            MechanismKind.AssemblyDirectory,
            libraryPath
        );
    }

    [WindowsFact]
    public void Resolve_ShouldReturnInconclusiveForRelativePath_WhenSearchPathOverrideIsPresent()
    {
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

    [WindowsFact]
    public void Resolve_ShouldSearchPathForRelativePath()
    {
        const string directoryName = "native";
        using var currentDirectory = new TemporaryDirectory(
            "consyzer-relative-missing-"
        );
        var environmentSubdirectory = Directory.CreateDirectory(
            Path.Combine(_envPathDirectory.Path, directoryName)
        );
        var libraryPath = Path.Combine(environmentSubdirectory.FullName, RelativePathLibraryName);
        File.WriteAllText(libraryPath, TestFileContent);

        using var currentDirectoryScope = new CurrentDirectoryScope(currentDirectory.Path);
        using var pathScope = new EnvironmentVariableScope(
            PathVariableName,
            _envPathDirectory.Path
        );

        var requestedPath = Path.Combine(directoryName, RelativePathLibraryName);
        var result = Resolve(requestedPath);

        AssertResolved(
            result,
            requestedPath,
            MechanismKind.EnvironmentOverride,
            libraryPath
        );
    }

    [WindowsTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Resolve_ShouldBeInconclusiveWhenRelativePathIsNotFound(
        bool hasDllImportSearchPathOverride
    )
    {
        using var currentDirectory = new TemporaryDirectory("consyzer-relative-missing-");
        using var currentDirectoryScope = new CurrentDirectoryScope(currentDirectory.Path);
        using var pathScope = new EnvironmentVariableScope(
            PathVariableName,
            _envPathDirectory.Path
        );

        var result = _resolver.Resolve(new LibraryResolutionContext(
            _targetFile,
            Path.Combine("native", MissingLibraryName),
            hasDllImportSearchPathOverride
        ));

        Assert.Equal(ResolutionState.Inconclusive, result.ResolutionState);
        Assert.Null(result.ResolvedPresence);
        Assert.NotEqual(NotSimulatedMechanisms.None, result.NotSimulated);
    }

    [WindowsTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Resolve_ShouldReportRedirectionUncertainty_WhenAbsolutePathDoesNotExist(
        bool hasDllImportSearchPathOverride
    )
    {
        using var directory = new TemporaryDirectory(AbsolutePathDirectoryPrefix);
        var missingPath = Path.Combine(directory.Path, "consyzer_missing_explicit.dll");

        var result = _resolver.Resolve(new LibraryResolutionContext(
            _targetFile,
            missingPath,
            hasDllImportSearchPathOverride
        ));

        Assert.Equal(ResolutionState.Inconclusive, result.ResolutionState);
        Assert.Null(result.ResolvedPresence);
        Assert.Empty(result.HeuristicCandidates);
        Assert.Equal(NotSimulatedMechanisms.WindowsDllRedirection, result.NotSimulated);
    }

    [WindowsFact]
    public void Resolve_ShouldReturnInconclusive_WhenLibraryIsNotFound()
    {
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
        Assert.Equal(mechanismKind, result.ResolvedPresence.MechanismKind);
        Assert.Equal(Path.GetFullPath(resolvedPath), result.ResolvedPresence.Path);
        Assert.Empty(result.HeuristicCandidates);
        Assert.Equal(NotSimulatedMechanisms.None, result.NotSimulated);
    }
}
