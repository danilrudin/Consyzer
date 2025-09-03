namespace Consyzer.Options;

internal sealed class AnalysisOptions
{
    public required string AnalysisDirectory { get; set; }
    public required string SearchPatterns { get; set; }
    public required bool RecursiveSearch { get; set; }
    public required OutputFormats ReportFormats { get; set; } = OutputFormats.Console;
}
