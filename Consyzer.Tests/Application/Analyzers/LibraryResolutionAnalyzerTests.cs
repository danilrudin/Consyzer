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
