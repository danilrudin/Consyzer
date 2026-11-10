namespace Consyzer.Options;

internal sealed class CommandLineOptions
{
    public required string AnalysisDirectory { get; set; }
    public required string SearchPatterns { get; set; }
    public required bool RecursiveSearch { get; set; }
    public required OutputFormats ReportFormats { get; set; } = OutputFormats.Console;

    [Flags]
    public enum OutputFormats
    {
        Console = 1 << 0,
        Json = 1 << 1,
        Csv = 1 << 2,
        Xml = 1 << 3
    }
}
