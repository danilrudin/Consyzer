namespace Consyzer.Core.Models.Analysis;

internal sealed class AnalysisFileClassification
{
    public required IReadOnlyList<FileInfo> NonEcmaModules { get; init; }
    public required IReadOnlyList<FileInfo> EcmaAssemblies { get; init; }
    public required IReadOnlyList<FileInfo> NonEcmaAssemblies { get; init; }
}
