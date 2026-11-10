using Consyzer.Core.Models.Exit;
using Consyzer.Core.Models.Resolution;

namespace Consyzer.Application.Analyzers;

internal sealed class AnalysisExitCodeAnalyzer
    : IAnalyzer<IEnumerable<LibraryResolution>, ExitStatus>
{
    public ExitStatus Analyze(IEnumerable<LibraryResolution> results)
    {
        var hasInconclusive = false;

        foreach (var result in results)
        {
            switch (result.ResolutionState)
            {
                case ResolutionState.Missing:
                    return ExitStatus.Missing();

                case ResolutionState.Inconclusive:
                    hasInconclusive = true;
                    break;
            }
        }

        if (hasInconclusive) return ExitStatus.Inconclusive();

        return ExitStatus.Success();
    }
}
