using Consyzer.Application.Analyzers;
using Consyzer.Core.Models.Exit;
using Consyzer.Core.Models.Resolution;

namespace Consyzer.Tests.Application.Analyzers;

public sealed class AnalysisExitCodeAnalyzerTests
{
    private readonly AnalysisExitCodeAnalyzer _analyzer = new();

    [Fact]
    public void Analyze_ShouldReturnSuccess_WhenAllDependenciesAreResolved()
    {
        var result = _analyzer.Analyze(
        [
            CreateResolution(ResolutionState.Resolved),
            CreateResolution(ResolutionState.Resolved)
        ]);

        Assert.Equal(AnalysisExitCode.Success, result.Code);
    }

    [Fact]
    public void Analyze_ShouldReturnInconclusive_WhenNoDependencyIsMissing()
    {
        var result = _analyzer.Analyze(
        [
            CreateResolution(ResolutionState.Resolved),
            CreateResolution(ResolutionState.Inconclusive)
        ]);

        Assert.Equal(AnalysisExitCode.Inconclusive, result.Code);
    }

    [Fact]
    public void Analyze_ShouldPrioritizeMissingOverInconclusive()
    {
        var result = _analyzer.Analyze(
        [
            CreateResolution(ResolutionState.Resolved),
            CreateResolution(ResolutionState.Inconclusive),
            CreateResolution(ResolutionState.Missing)
        ]);

        Assert.Equal(AnalysisExitCode.Missing, result.Code);
    }

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
