using Consyzer.Application.Analyzers;
using Consyzer.Core.Models.Exit;
using Consyzer.Core.Models.Resolution;

namespace Consyzer.Tests.Application.Analyzers;

public sealed class AnalysisExitCodeAnalyzerTests
{
    private readonly AnalysisExitCodeAnalyzer _analyzer = new();

    [Fact]
    public void Analyze_ShouldReturnSuccess_WhenThereAreNoNativeDependencies()
    {
        Assert.Equal(ExitStatus.Success(), _analyzer.Analyze([]));
    }

    [Fact]
    public void Analyze_ShouldReturnSuccess_WhenAllDependenciesAreResolved()
    {
        var result = _analyzer.Analyze(
        [
            CreateResolved(),
            CreateResolved()
        ]);

        Assert.Equal(AnalysisExitCode.Success, result.Code);
    }

    [Fact]
    public void Analyze_ShouldReturnInconclusive_WhenNoDependencyIsMissing()
    {
        var result = _analyzer.Analyze(
        [
            CreateResolved(),
            CreateInconclusive()
        ]);

        Assert.Equal(AnalysisExitCode.Inconclusive, result.Code);
    }

    [Fact]
    public void Analyze_ShouldPrioritizeMissingOverInconclusive()
    {
        var result = _analyzer.Analyze(
        [
            CreateResolved(),
            CreateInconclusive(),
            LibraryResolution.CreateMissing("Target.dll", "native", [])
        ]);

        Assert.Equal(AnalysisExitCode.Missing, result.Code);
    }

    private static LibraryResolution CreateResolved()
        => LibraryResolution.CreateResolved(
            "Target.dll",
            "native",
            new ResolvedPresence("native", MechanismKind.ExplicitPath),
            []
        );

    private static LibraryResolution CreateInconclusive()
        => LibraryResolution.CreateInconclusive(
            "Target.dll",
            "native",
            [],
            NotSimulatedMechanisms.LinuxRPathRunPath
        );
}
