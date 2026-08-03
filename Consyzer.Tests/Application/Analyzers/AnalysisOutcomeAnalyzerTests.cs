using System.Reflection;
using Consyzer.Application.Analyzers;
using Consyzer.Core.Models.Analysis;
using Consyzer.Core.Models.Metadata;
using Consyzer.Core.Models.Resolution;

namespace Consyzer.Tests.Application.Analyzers;

public sealed class AnalysisOutcomeAnalyzerTests
{
    [Fact]
    public void Analyze_ShouldBuildOutcomeAndSummary()
    {
        var targetFile = new FileInfo("Target.dll");
        var methodGroups = new[]
        {
            CreateMethodGroup(targetFile, 2),
            CreateMethodGroup(targetFile, 1)
        };
        var resolutions = new[]
        {
            CreateResolution(ResolutionState.Resolved),
            CreateResolution(ResolutionState.Missing),
            CreateResolution(ResolutionState.Inconclusive)
        };
        var input = new AnalysisOutcomeInput(
            4,
            [
                new AssemblyMetadata
                {
                    File = targetFile,
                    Version = "1.0.0.0",
                    CreationDateUtc = DateTime.UnixEpoch,
                    Sha256 = "HASH"
                }
            ],
            methodGroups,
            new LibraryResolutionOutcome
            {
                Platform = "Test",
                Results = resolutions
            }
        );

        var outcome = new AnalysisOutcomeAnalyzer().Analyze(input);

        Assert.Equal("Test", outcome.Platform);
        Assert.Same(input.AssemblyMetadataList, outcome.AssemblyMetadataList);
        Assert.Same(methodGroups, outcome.PInvokeMethodGroups);
        Assert.Same(resolutions, outcome.LibraryResolutions);
        Assert.Equal(4, outcome.Summary.TotalFiles);
        Assert.Equal(1, outcome.Summary.EcmaAssemblies);
        Assert.Equal(2, outcome.Summary.AssembliesWithPInvoke);
        Assert.Equal(3, outcome.Summary.TotalPInvokeMethods);
        Assert.Equal(1, outcome.Summary.ResolvedLibraries);
        Assert.Equal(1, outcome.Summary.MissingLibraries);
        Assert.Equal(1, outcome.Summary.InconclusiveLibraries);
    }

    private static PInvokeMethodGroup CreateMethodGroup(FileInfo file, int methodCount)
        => new()
        {
            File = file,
            Methods = [.. Enumerable
                .Range(0, methodCount)
                .Select(_ => new PInvokeMethod
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
                    ImportName = "native",
                    ImportFlags = MethodImportAttributes.None
                })]
        };

    private static LibraryResolution CreateResolution(ResolutionState state)
        => new()
        {
            TargetPath = "Target.dll",
            LibraryName = "native",
            ResolutionState = state,
            ResolvedPresence = state == ResolutionState.Resolved
                ? new ResolvedPresence("native", MechanismKind.ExplicitPath)
                : null,
            HeuristicCandidates = [],
            NotSimulated = state == ResolutionState.Inconclusive
                ? NotSimulatedMechanisms.LinuxRPathRunPath
                : NotSimulatedMechanisms.None
        };
}
