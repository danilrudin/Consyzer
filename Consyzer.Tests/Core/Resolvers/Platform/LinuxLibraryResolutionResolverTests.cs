using System.Runtime.InteropServices;
using Consyzer.Core.Models.Resolution;
using Consyzer.Core.Resolvers.Platform;
using Consyzer.Tests.TestSupport.Collections;
using Consyzer.Tests.TestSupport.FileSystem;
using Consyzer.Tests.TestSupport.Scopes;

namespace Consyzer.Tests.Core.Resolvers.Platform;

[Collection(TestCollectionNames.ResolverEnvironment)]
public sealed class LinuxLibraryResolutionResolverTests : IDisposable
{
    private const string LdLibraryPathVariableName = "LD_LIBRARY_PATH";
    private const string TestFileContent = "test file";
    private const string TargetFileName = "Target.dll";

    private readonly TemporaryDirectory _analysisDirectory = new("consyzer-linux-analysis-");
    private readonly TemporaryDirectory _environmentDirectory = new("consyzer-linux-env-");
    private readonly FileInfo _targetFile;
    private readonly LinuxLibraryResolutionResolver _resolver;

    public LinuxLibraryResolutionResolverTests()
    {
        _targetFile = _analysisDirectory.CreateFile(TargetFileName, TestFileContent);
        _resolver = new LinuxLibraryResolutionResolver(_analysisDirectory.Path);
    }

    [Theory]
    [InlineData("consyzer_linux_plain", "consyzer_linux_plain.so")]
    [InlineData("libconsyzer_linux_plain", "libconsyzer_linux_plain.so")]
    [InlineData("consyzer_linux_plain.so", "consyzer_linux_plain.so")]
    [InlineData("libconsyzer_linux_plain.so", "libconsyzer_linux_plain.so")]
    public void Resolve_ShouldUseExpectedLinuxNameVariation(
        string requestedName,
        string physicalName
    )
    {
        if (!OperatingSystem.IsLinux()) return;

        var libraryFile = _environmentDirectory.CreateFile(physicalName, TestFileContent);
        using var pathScope = new EnvironmentVariableScope(
            LdLibraryPathVariableName,
            _environmentDirectory.Path
        );

        var result = Resolve(requestedName);

        AssertResolved(
            result,
            requestedName,
            MechanismKind.EnvironmentOverride,
            libraryFile.FullName
        );
    }

    [Fact]
    public void Resolve_ShouldUseCanonicalAndLibPrefixedName_WhenNameHasNoExtension()
    {
        if (!OperatingSystem.IsLinux()) return;

        const string requestedName = "consyzer_linux_name_variation";
        var libraryFile = _environmentDirectory.CreateFile(
            "lib" + requestedName + ".so",
            TestFileContent
        );

        using var pathScope = new EnvironmentVariableScope(
            LdLibraryPathVariableName,
            _environmentDirectory.Path
        );

        var result = Resolve(requestedName);

        AssertResolved(
            result,
            requestedName,
            MechanismKind.EnvironmentOverride,
            libraryFile.FullName
        );
    }

    [Fact]
    public void Resolve_ShouldCompleteSearchForFirstNameVariationBeforeTryingNextVariation()
    {
        if (!OperatingSystem.IsLinux()) return;

        const string requestedName = "consyzer_variation_precedence";
        var environmentLibrary = _environmentDirectory.CreateFile(
            requestedName + ".so",
            TestFileContent
        );
        _analysisDirectory.CreateFile(
            "lib" + requestedName + ".so",
            TestFileContent
        );

        using var pathScope = new EnvironmentVariableScope(
            LdLibraryPathVariableName,
            _environmentDirectory.Path
        );

        var result = Resolve(requestedName);

        AssertResolved(
            result,
            requestedName,
            MechanismKind.EnvironmentOverride,
            environmentLibrary.FullName
        );
    }

    [Fact]
    public void Resolve_ShouldTryCanonicalSuffixAfterVersionedSharedObjectName()
    {
        if (!OperatingSystem.IsLinux()) return;

        const string requestedName = "consyzer_versioned.so.6";
        var libraryFile = _environmentDirectory.CreateFile(
            "lib" + requestedName + ".so",
            TestFileContent
        );

        using var pathScope = new EnvironmentVariableScope(
            LdLibraryPathVariableName,
            _environmentDirectory.Path
        );

        var result = Resolve(requestedName);

        AssertResolved(
            result,
            requestedName,
            MechanismKind.EnvironmentOverride,
            libraryFile.FullName
        );
    }

    [Fact]
    public void Resolve_ShouldUseExactVersionedSharedObjectName()
    {
        if (!OperatingSystem.IsLinux()) return;

        const string requestedName = "consyzer_exact_versioned.so.6";
        var libraryFile = _environmentDirectory.CreateFile(requestedName, TestFileContent);
        using var pathScope = new EnvironmentVariableScope(
            LdLibraryPathVariableName,
            _environmentDirectory.Path
        );

        var result = Resolve(requestedName);

        AssertResolved(
            result,
            requestedName,
            MechanismKind.EnvironmentOverride,
            libraryFile.FullName
        );
    }

    [Fact]
    public void Resolve_ShouldStillTryLibPrefix_WhenDeclaredNameAlreadyStartsWithLib()
    {
        if (!OperatingSystem.IsLinux()) return;

        const string requestedName = "libconsyzer_already_prefixed.so";
        var libraryFile = _environmentDirectory.CreateFile(
            "lib" + requestedName,
            TestFileContent
        );

        using var pathScope = new EnvironmentVariableScope(
            LdLibraryPathVariableName,
            _environmentDirectory.Path
        );

        var result = Resolve(requestedName);

        AssertResolved(
            result,
            requestedName,
            MechanismKind.EnvironmentOverride,
            libraryFile.FullName
        );
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Resolve_ShouldReturnResolvedWithExplicitPath_WhenAbsolutePathExists(
        bool hasDllImportSearchPathOverride
    )
    {
        if (!OperatingSystem.IsLinux()) return;

        var libraryFile = _environmentDirectory.CreateFile(
            "libconsyzer_explicit.so",
            TestFileContent
        );

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

    [Fact]
    public void Resolve_ShouldReturnMissing_WhenExplicitPathDoesNotExist()
    {
        if (!OperatingSystem.IsLinux()) return;

        var missingPath = Path.Combine(
            _environmentDirectory.Path,
            "libconsyzer_missing_explicit.so"
        );

        var result = Resolve(missingPath);

        Assert.Equal(ResolutionState.Missing, result.ResolutionState);
        Assert.Null(result.ResolvedPresence);
        Assert.Empty(result.HeuristicCandidates);
        Assert.Equal(NotSimulatedMechanisms.None, result.NotSimulated);
    }

    [Fact]
    public void Resolve_ShouldUseSoVariationForRelativeExplicitPath()
    {
        if (!OperatingSystem.IsLinux()) return;

        const string requestedBaseName = "consyzer_relative_explicit";
        using var currentDirectory = new TemporaryDirectory("consyzer-linux-explicit-");
        var relativeDirectory = Directory.CreateDirectory(
            Path.Combine(currentDirectory.Path, "native")
        );
        var libraryPath = Path.Combine(
            relativeDirectory.FullName,
            requestedBaseName + ".so"
        );
        File.WriteAllText(libraryPath, TestFileContent);
        using var currentDirectoryScope = new CurrentDirectoryScope(currentDirectory.Path);
        using var pathScope = new EnvironmentVariableScope(LdLibraryPathVariableName, null);
        var requestedPath = Path.Combine(relativeDirectory.Name, requestedBaseName);

        var result = Resolve(requestedPath);

        AssertResolved(
            result,
            requestedPath,
            MechanismKind.ExplicitPath,
            libraryPath
        );
    }

    [Fact]
    public void Resolve_ShouldNotFallbackToLdLibraryPath_WhenRelativeExplicitPathIsMissing()
    {
        if (!OperatingSystem.IsLinux()) return;

        const string requestedBaseName = "consyzer_explicit_no_fallback";
        using var currentDirectory = new TemporaryDirectory("consyzer-linux-explicit-");
        var environmentSubdirectory = Directory.CreateDirectory(
            Path.Combine(_environmentDirectory.Path, "native")
        );
        File.WriteAllText(
            Path.Combine(environmentSubdirectory.FullName, requestedBaseName + ".so"),
            TestFileContent
        );
        using var currentDirectoryScope = new CurrentDirectoryScope(currentDirectory.Path);
        using var pathScope = new EnvironmentVariableScope(
            LdLibraryPathVariableName,
            _environmentDirectory.Path
        );
        var requestedPath = Path.Combine("native", requestedBaseName);

        var result = Resolve(requestedPath);

        Assert.Equal(ResolutionState.Missing, result.ResolutionState);
        Assert.Null(result.ResolvedPresence);
        Assert.Empty(result.HeuristicCandidates);
        Assert.Equal(NotSimulatedMechanisms.None, result.NotSimulated);
    }

    [Fact]
    public void Resolve_ShouldTreatEmptyLdLibraryPathSegmentAsCurrentDirectory()
    {
        if (!OperatingSystem.IsLinux()) return;

        const string requestedName = "consyzer_current_directory";
        using var currentDirectory = new TemporaryDirectory("consyzer-linux-current-");
        var libraryFile = currentDirectory.CreateFile(requestedName + ".so", TestFileContent);
        using var currentDirectoryScope = new CurrentDirectoryScope(currentDirectory.Path);
        using var pathScope = new EnvironmentVariableScope(
            LdLibraryPathVariableName,
            Path.PathSeparator.ToString()
        );

        var result = Resolve(requestedName);

        AssertResolved(
            result,
            requestedName,
            MechanismKind.EnvironmentOverride,
            libraryFile.FullName
        );
    }

    [Fact]
    public void Resolve_ShouldNotTreatSemicolonAsLdLibraryPathSeparator()
    {
        if (!OperatingSystem.IsLinux()) return;

        const string requestedName = "consyzer_semicolon_path";
        using var unusedDirectory = new TemporaryDirectory("consyzer-linux-unused-");
        var libraryFile = _environmentDirectory.CreateFile(
            requestedName + ".so",
            TestFileContent
        );
        var value = unusedDirectory.Path + ';' + _environmentDirectory.Path;

        using var pathScope = new EnvironmentVariableScope(LdLibraryPathVariableName, value);

        var result = Resolve(requestedName);

        Assert.Equal(ResolutionState.Inconclusive, result.ResolutionState);
        Assert.Null(result.ResolvedPresence);
        Assert.DoesNotContain(libraryFile.FullName, result.HeuristicCandidates);
    }

    [Fact]
    public void Resolve_ShouldNotTrimQuotesFromLdLibraryPathEntry()
    {
        if (!OperatingSystem.IsLinux()) return;

        const string requestedName = "consyzer_quoted_path";
        var libraryFile = _environmentDirectory.CreateFile(
            requestedName + ".so",
            TestFileContent
        );

        using var pathScope = new EnvironmentVariableScope(
            LdLibraryPathVariableName,
            $"\"{_environmentDirectory.Path}\""
        );

        var result = Resolve(requestedName);

        Assert.Equal(ResolutionState.Inconclusive, result.ResolutionState);
        Assert.Null(result.ResolvedPresence);
        Assert.DoesNotContain(libraryFile.FullName, result.HeuristicCandidates);
    }

    [Theory]
    [InlineData("$ORIGIN/libconsyzer_token.so")]
    [InlineData("${LIB}/libconsyzer_token.so")]
    [InlineData("$PLATFORM/libconsyzer_token.so")]
    public void Resolve_ShouldReturnInconclusive_WhenDependencyContainsDynamicStringToken(
        string requestedName
    )
    {
        if (!OperatingSystem.IsLinux()) return;

        var result = Resolve(requestedName);

        Assert.Equal(ResolutionState.Inconclusive, result.ResolutionState);
        Assert.Null(result.ResolvedPresence);
        Assert.True(
            result.NotSimulated.HasFlag(
                NotSimulatedMechanisms.LinuxDependencyDynamicStringTokens
            )
        );
    }

    [Fact]
    public void Resolve_ShouldNotProbeTokenizedLdLibraryPathEntryAsLiteralDirectory()
    {
        if (!OperatingSystem.IsLinux()) return;

        const string requestedName = "consyzer_literal_token_directory";
        using var currentDirectory = new TemporaryDirectory("consyzer-linux-token-cwd-");
        var literalTokenDirectory = Directory.CreateDirectory(
            Path.Combine(currentDirectory.Path, "${ORIGIN}")
        );
        File.WriteAllText(
            Path.Combine(literalTokenDirectory.FullName, requestedName + ".so"),
            TestFileContent
        );
        using var currentDirectoryScope = new CurrentDirectoryScope(currentDirectory.Path);
        using var pathScope = new EnvironmentVariableScope(
            LdLibraryPathVariableName,
            "${ORIGIN}"
        );

        var result = Resolve(requestedName);

        Assert.Equal(ResolutionState.Inconclusive, result.ResolutionState);
        Assert.Null(result.ResolvedPresence);
        Assert.True(
            result.NotSimulated.HasFlag(
                NotSimulatedMechanisms.LinuxLdLibraryPathDynamicStringTokens
            )
        );
    }

    [Theory]
    [InlineData("$ORIGIN")]
    [InlineData("${LIB}")]
    [InlineData("$PLATFORM")]
    public void Resolve_ShouldReportDynamicStringTokenCaveat_WhenLdLibraryPathContainsToken(
        string token
    )
    {
        if (!OperatingSystem.IsLinux()) return;

        const string requestedName = "consyzer_dynamic_token";
        var libraryFile = _environmentDirectory.CreateFile(requestedName + ".so", TestFileContent);
        var value = token + Path.PathSeparator + _environmentDirectory.Path;

        using var pathScope = new EnvironmentVariableScope(LdLibraryPathVariableName, value);

        var result = Resolve(requestedName);

        AssertResolved(
            result,
            requestedName,
            MechanismKind.EnvironmentOverride,
            libraryFile.FullName
        );
        Assert.True(
            result.NotSimulated.HasFlag(
                NotSimulatedMechanisms.LinuxLdLibraryPathDynamicStringTokens
            )
        );
    }

    [Fact]
    public void Resolve_ShouldReturnResolvedWithAssemblyDirectory_WhenLibraryIsNextToTarget()
    {
        if (!OperatingSystem.IsLinux()) return;

        const string requestedName = "consyzer_adjacent_library";
        using var nestedDirectory = new TemporaryDirectory(
            "consyzer-linux-target-",
            _analysisDirectory.Path
        );
        var targetFile = nestedDirectory.CreateFile(TargetFileName, TestFileContent);
        var libraryFile = nestedDirectory.CreateFile(requestedName + ".so", TestFileContent);
        using var pathScope = new EnvironmentVariableScope(LdLibraryPathVariableName, null);

        var result = _resolver.Resolve(
            new LibraryResolutionContext(targetFile, requestedName)
        );

        AssertResolved(
            result,
            requestedName,
            MechanismKind.AssemblyDirectory,
            libraryFile.FullName
        );
    }

    [Fact]
    public void Resolve_ShouldReturnInconclusive_WhenSearchPathOverrideIsPresent()
    {
        if (!OperatingSystem.IsLinux()) return;

        const string requestedName = "consyzer_ignored_search_path_attribute";
        _analysisDirectory.CreateFile(
            requestedName + ".so",
            TestFileContent
        );
        using var pathScope = new EnvironmentVariableScope(
            LdLibraryPathVariableName,
            null
        );

        var result = _resolver.Resolve(new LibraryResolutionContext(
            _targetFile,
            requestedName,
            HasDllImportSearchPathOverride: true
        ));

        Assert.Equal(ResolutionState.Inconclusive, result.ResolutionState);
        Assert.Null(result.ResolvedPresence);
        Assert.Empty(result.HeuristicCandidates);
        Assert.True(result.NotSimulated.HasFlag(
            NotSimulatedMechanisms.LinuxDotNetSearchPathOverrides
        ));
    }

    [Fact]
    public void Resolve_ShouldComparePhysicalFileNamesCaseSensitively()
    {
        if (!OperatingSystem.IsLinux()) return;

        _environmentDirectory.CreateFile("Consyzer_Case_Sensitive.so", TestFileContent);
        using var pathScope = new EnvironmentVariableScope(
            LdLibraryPathVariableName,
            _environmentDirectory.Path
        );

        var result = Resolve("consyzer_case_sensitive");

        Assert.Equal(ResolutionState.Inconclusive, result.ResolutionState);
        Assert.Null(result.ResolvedPresence);
    }

    [Fact]
    public void Resolve_ShouldUseMultiarchDirectoryForCurrentArchitecture()
    {
        if (!OperatingSystem.IsLinux()) return;

        var multiarchDirectory = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => "/lib/x86_64-linux-gnu",
            Architecture.X86 => "/lib/i386-linux-gnu",
            Architecture.Arm64 => "/lib/aarch64-linux-gnu",
            Architecture.Arm => "/lib/arm-linux-gnueabihf",
            _ => null
        };

        Assert.NotNull(multiarchDirectory);
        Assert.True(Directory.Exists(multiarchDirectory));

        string[] nonMultiarchLocations = ["/lib", "/usr/lib", "/lib64", "/usr/lib64"];
        var libraryPath = Directory
            .EnumerateFiles(multiarchDirectory, "*.so*")
            .FirstOrDefault(path => nonMultiarchLocations.All(
                directory => !File.Exists(Path.Combine(directory, Path.GetFileName(path)))
            ));

        Assert.NotNull(libraryPath);
        using var pathScope = new EnvironmentVariableScope(LdLibraryPathVariableName, null);

        var result = Resolve(Path.GetFileName(libraryPath));

        AssertResolved(
            result,
            Path.GetFileName(libraryPath),
            MechanismKind.DefaultSystemLocations,
            libraryPath
        );
    }

    [Fact]
    public void Resolve_ShouldReportAnalysisDirectoryCandidateAsHeuristic_WhenTargetIsNested()
    {
        if (!OperatingSystem.IsLinux()) return;

        const string requestedName = "consyzer_analysis_directory_heuristic";
        using var nestedDirectory = new TemporaryDirectory(
            "consyzer-linux-target-",
            _analysisDirectory.Path
        );
        var targetFile = nestedDirectory.CreateFile(TargetFileName, TestFileContent);
        var libraryFile = _analysisDirectory.CreateFile(
            requestedName + ".so",
            TestFileContent
        );
        using var pathScope = new EnvironmentVariableScope(LdLibraryPathVariableName, null);

        var result = _resolver.Resolve(
            new LibraryResolutionContext(targetFile, requestedName)
        );

        Assert.Equal(ResolutionState.Inconclusive, result.ResolutionState);
        Assert.Null(result.ResolvedPresence);
        Assert.Contains(Path.GetFullPath(libraryFile.FullName), result.HeuristicCandidates);
        Assert.NotEqual(NotSimulatedMechanisms.None, result.NotSimulated);
    }

    [Fact]
    public void Resolve_ShouldReturnInconclusiveWithKnownLoaderGaps_WhenLibraryIsNotFound()
    {
        if (!OperatingSystem.IsLinux()) return;

        using var pathScope = new EnvironmentVariableScope(LdLibraryPathVariableName, null);

        var result = Resolve("consyzer_library_that_does_not_exist");

        Assert.Equal(ResolutionState.Inconclusive, result.ResolutionState);
        Assert.Null(result.ResolvedPresence);
        Assert.Empty(result.HeuristicCandidates);
        Assert.True(result.NotSimulated.HasFlag(NotSimulatedMechanisms.LinuxRPathRunPath));
        Assert.True(result.NotSimulated.HasFlag(NotSimulatedMechanisms.LinuxLdSoCache));
        Assert.True(result.NotSimulated.HasFlag(NotSimulatedMechanisms.LinuxMultiarchDefaultPaths));
    }

    public void Dispose()
    {
        _analysisDirectory.Dispose();
        _environmentDirectory.Dispose();
    }

    private LibraryResolution Resolve(string libraryName)
        => _resolver.Resolve(new LibraryResolutionContext(_targetFile, libraryName));

    private static void AssertResolved(
        LibraryResolution result,
        string libraryName,
        MechanismKind mechanismKind,
        string resolvedPath
    )
    {
        Assert.Equal(libraryName, result.LibraryName);
        Assert.Equal(ResolutionState.Resolved, result.ResolutionState);
        Assert.NotNull(result.ResolvedPresence);
        Assert.Equal(mechanismKind, result.ResolvedPresence!.MechanismKind);
        Assert.Equal(Path.GetFullPath(resolvedPath), result.ResolvedPresence.Path);
    }
}
