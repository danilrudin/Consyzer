namespace Consyzer.Core.Models;

internal sealed class AnalysisOutcome
{
    public required IReadOnlyList<AssemblyMetadata> AssemblyMetadataList { get; init; }
    public required IReadOnlyList<PInvokeMethodGroup> PInvokeMethodGroups { get; init; }
    public required IReadOnlyList<LibraryResolutionResult> LibraryResolutions { get; init; }
    public required AnalysisSummary Summary { get; init; }
}
