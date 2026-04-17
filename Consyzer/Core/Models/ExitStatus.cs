namespace Consyzer.Core.Models;

internal sealed record ExitStatus(
    AnalysisExitCode Code,
    InvalidInputReason? InvalidReason = null,
    string? Message = null,
    Exception? Exception = null
);

internal enum AnalysisExitCode
{
    Success = 0,
    Missing = 1,
    Inconclusive = 2,
    InvalidInput = 3,
    ToolError = 4
}

internal enum InvalidInputReason
{
    NoAnalysisDirectory = -1,
    NoSearchPatterns = -2,
    NoFilesFound = -3,
    AllFilesInvalid = -4,
    NoPInvokeMethodsFound = -5
}
