using System.Reflection;
using Consyzer.Application.Analyzers;
using Consyzer.Core.Models.Metadata;
using Consyzer.Core.Models.Resolution;
using Consyzer.Core.Resolvers;
using Consyzer.Tests.TestSupport.FileSystem;

namespace Consyzer.Tests.Application.Analyzers;

public sealed class LibraryResolutionAnalyzerTests
{
    [Fact]
    public void Analyze_ShouldKeepCaseDistinctLibraryNames_OnLinux()
    {
        if (!OperatingSystem.IsLinux()) return;

        using var directory = new TemporaryDirectory("consyzer-library-analyzer-");
        var targetFile = directory.CreateFile("Target.dll");
        var analyzer = CreateAnalyzer(directory.Path);

        var outcome = analyzer.Analyze(
        [
            CreateGroup(targetFile, "consyzer_case_sensitive.so", "Consyzer_Case_Sensitive.so")
        ]);

        Assert.Equal(2, outcome.Results.Count);
    }

    [Fact]
    public void Analyze_ShouldDeduplicateLibraryNamesIgnoringCase_OnWindows()
    {
        if (!OperatingSystem.IsWindows()) return;

        using var directory = new TemporaryDirectory("consyzer-library-analyzer-");
        var targetFile = directory.CreateFile("Target.dll");
        var analyzer = CreateAnalyzer(directory.Path);

        var outcome = analyzer.Analyze(
        [
            CreateGroup(targetFile, "consyzer_case_insensitive.dll", "Consyzer_Case_Insensitive.dll")
        ]);

        Assert.Single(outcome.Results);
    }

    [Fact]
    public void Analyze_ShouldApplySearchPathOverrideToDeduplicatedDependency_OnWindows()
    {
        if (!OperatingSystem.IsWindows()) return;

        const string libraryName = "consyzer_search_path_override.dll";
        using var directory = new TemporaryDirectory("consyzer-library-analyzer-");
        var targetFile = directory.CreateFile("Target.dll");
        directory.CreateFile(libraryName);
        var analyzer = CreateAnalyzer(directory.Path);
        var group = new PInvokeMethodGroup
        {
            File = targetFile,
            Methods =
            [
                CreateMethod(libraryName),
                CreateMethod(libraryName, hasDllImportSearchPathOverride: true)
            ]
        };

        var outcome = analyzer.Analyze([group]);

        var result = Assert.Single(outcome.Results);
        Assert.Equal(ResolutionState.Inconclusive, result.ResolutionState);
        Assert.Null(result.ResolvedPresence);
        Assert.True(result.NotSimulated.HasFlag(
            NotSimulatedMechanisms.WindowsDotNetSearchPathOverrides
        ));
    }

    [Fact]
    public void Analyze_ShouldResolveSameLibrarySeparatelyForEachTargetAssembly()
    {
        using var directory = new TemporaryDirectory("consyzer-library-analyzer-");
        using var firstDirectory = new TemporaryDirectory("first-", directory.Path);
        using var secondDirectory = new TemporaryDirectory("second-", directory.Path);
        var libraryName = OperatingSystem.IsWindows() ? "consyzer_scoped.dll" : "consyzer_scoped.so";
        var firstTarget = firstDirectory.CreateFile("First.dll");
        var secondTarget = secondDirectory.CreateFile("Second.dll");
        var firstLibrary = firstDirectory.CreateFile(libraryName);
        var secondLibrary = secondDirectory.CreateFile(libraryName);
        var analyzer = CreateAnalyzer(directory.Path);

        var outcome = analyzer.Analyze(
        [
            CreateGroup(firstTarget, libraryName, libraryName),
            CreateGroup(secondTarget, libraryName)
        ]);

        Assert.Equal(2, outcome.Results.Count);
        var firstResult = Assert.Single(outcome.Results, result => result.TargetPath == firstTarget.FullName);
        var secondResult = Assert.Single(outcome.Results, result => result.TargetPath == secondTarget.FullName);
        Assert.Equal(ResolutionState.Resolved, firstResult.ResolutionState);
        Assert.Equal(ResolutionState.Resolved, secondResult.ResolutionState);
        Assert.Equal(firstLibrary.FullName, firstResult.ResolvedPresence?.Path);
        Assert.Equal(secondLibrary.FullName, secondResult.ResolvedPresence?.Path);
    }

    private static LibraryResolutionAnalyzer CreateAnalyzer(string analysisDirectory)
        => new(new MultiPlatformLibraryResolutionResolver(analysisDirectory));

    private static PInvokeMethodGroup CreateGroup(FileInfo file, params string[] importNames)
        => new()
        {
            File = file,
            Methods = [.. importNames.Select(importName => CreateMethod(importName))]
        };

    private static PInvokeMethod CreateMethod(
        string importName,
        bool hasDllImportSearchPathOverride = false
    )
        => new()
        {
            Signature = new MethodSignature
            {
                ReturnType = "Void",
                IsStatic = true,
                Namespace = "Tests",
                Class = "Native",
                Method = "Invoke",
                MethodArguments = []
            },
            ImportName = importName,
            ImportFlags = MethodImportAttributes.None,
            HasDllImportSearchPathOverride = hasDllImportSearchPathOverride
        };
}
