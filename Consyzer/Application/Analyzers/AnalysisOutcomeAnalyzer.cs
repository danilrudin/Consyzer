using Consyzer.Core.Models.Analysis;
using Consyzer.Core.Models.Resolution;

namespace Consyzer.Application.Analyzers;

internal sealed class AnalysisOutcomeAnalyzer : IAnalyzer<AnalysisOutcomeInput, AnalysisOutcome>
{
    public AnalysisOutcome Analyze(AnalysisOutcomeInput input)
    {
        var totalPInvokeMethods = 0;

        foreach (var group in input.PInvokeMethodGroups)
        {
            totalPInvokeMethods += group.Methods.Count;
        }

        var resolvedLibraries = 0;
        var missingLibraries = 0;
        var inconclusiveLibraries = 0;

        foreach (var resolution in input.LibraryResolution.Results)
        {
            switch (resolution.ResolutionState)
            {
                case ResolutionState.Resolved:
                    ++resolvedLibraries;
                    break;
                case ResolutionState.Missing:
                    ++missingLibraries;
                    break;
                case ResolutionState.Inconclusive:
                    ++inconclusiveLibraries;
                    break;
            }
        }

        return new AnalysisOutcome
        {
            Platform = input.LibraryResolution.Platform,
            AssemblyMetadataList = input.AssemblyMetadataList,
            PInvokeMethodGroups = input.PInvokeMethodGroups,
            LibraryResolutions = input.LibraryResolution.Results,
            Summary = new AnalysisSummary
            {
                TotalFiles = input.TotalFiles,
                EcmaAssemblies = input.AssemblyMetadataList.Count,
                AssembliesWithPInvoke = input.PInvokeMethodGroups.Count,
                TotalPInvokeMethods = totalPInvokeMethods,
                ResolvedLibraries = resolvedLibraries,
                MissingLibraries = missingLibraries,
                InconclusiveLibraries = inconclusiveLibraries
            }
        };
    }
}
