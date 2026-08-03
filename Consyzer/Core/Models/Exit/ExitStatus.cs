namespace Consyzer.Core.Models.Exit;

internal readonly record struct ExitStatus(
    AnalysisExitCode Code,
    InvalidInputReason? InvalidReason = null
)
{
    public int ProcessExitCode => (int)Code;

    public static ExitStatus Success() => new(AnalysisExitCode.Success);

    public static ExitStatus Missing() => new(AnalysisExitCode.Missing);

    public static ExitStatus Inconclusive() => new(AnalysisExitCode.Inconclusive);

    public static ExitStatus InvalidInput(InvalidInputReason reason) => new(AnalysisExitCode.InvalidInput, reason);

    public static ExitStatus ToolError() => new(AnalysisExitCode.ToolError);
}

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
    AnalysisDirectoryNotFound = -5,
    InvalidOptionValue = -6
}
