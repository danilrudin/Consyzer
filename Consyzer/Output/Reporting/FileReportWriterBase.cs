using Consyzer.Helpers;
using Consyzer.Core.Models.Analysis;

namespace Consyzer.Output.Reporting;

internal abstract class FileReportWriterBase : IReportWriter
{
    private const string FileNamePrefix = "report_";
    private const string TargetDirectoryName = "Reports";
    private const string FallbackIdentifier = "fallback";

    private static readonly string ReportIdentifier = Path.GetFileNameWithoutExtension(
        LoggingHelper.GetLogFileName() ?? FallbackIdentifier
    );

    protected abstract string FileExtension { get; }

    public string Write(AnalysisOutcome outcome)
    {
        Directory.CreateDirectory(TargetDirectory);

        var fullPath = Path.Combine(TargetDirectory, GetFileName());

        WriteReport(outcome, fullPath);

        return fullPath;
    }

    protected abstract void WriteReport(AnalysisOutcome outcome, string fullPath);

    private static string TargetDirectory => Path.Combine(AppContext.BaseDirectory, TargetDirectoryName);

    private string GetFileName() => $"{FileNamePrefix}{ReportIdentifier}{FileExtension}";
}
