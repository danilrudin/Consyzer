namespace Consyzer.Core.Models;

internal sealed class AnalysisSummary
{
    public required int TotalFiles { get; init; }
    public required int EcmaAssemblies { get; init; }
    public required int AssembliesWithPInvoke { get; init; }
    public required int TotalPInvokeMethods { get; init; }
    public required int ResolvedLibraries { get; init; }
    public required int MissingLibraries { get; init; }
    public required int InconclusiveLibraries { get; init; }
}
