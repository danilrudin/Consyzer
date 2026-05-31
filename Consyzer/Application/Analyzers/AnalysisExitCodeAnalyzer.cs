using Consyzer.Core.Models.Exit;
using Consyzer.Core.Models.Resolution;

namespace Consyzer.Application.Analyzers;

internal sealed class AnalysisExitCodeAnalyzer
    : IAnalyzer<IEnumerable<LibraryResolution>, AnalysisExitCode>
{
    public AnalysisExitCode Analyze(IEnumerable<LibraryResolution> results)
    {
        var hasMissing = false;
        var hasInconclusive = false;

        foreach (var result in results)
        {
            switch (result.ResolutionState)
            {
                case ResolutionState.Missing:
                    hasMissing = true;
                    break;

                case ResolutionState.Inconclusive:
                    hasInconclusive = true;
                    break;
            }
        }

        if (hasMissing) return AnalysisExitCode.Missing;
        if (hasInconclusive) return AnalysisExitCode.Inconclusive;

        return AnalysisExitCode.Success;
    }
}
