using Consyzer.Core.Models.Metadata;
using Consyzer.Core.Models.Resolution;

namespace Consyzer.Core.Models.Analysis;

internal sealed class AnalysisOutcome
{
    public required string Platform { get; init; }
    public required IReadOnlyList<AssemblyMetadata> AssemblyMetadataList { get; init; }
    public required IReadOnlyList<PInvokeMethodGroup> PInvokeMethodGroups { get; init; }
    public required IReadOnlyList<LibraryResolution> LibraryResolutions { get; init; }
    public required AnalysisSummary Summary { get; init; }
}
