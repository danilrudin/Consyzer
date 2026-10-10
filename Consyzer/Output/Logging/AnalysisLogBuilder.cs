using Microsoft.Extensions.Options;
using Consyzer.Options;
using Consyzer.Output.Builders;
using Consyzer.Core.Models.Analysis;

namespace Consyzer.Output.Logging;

internal sealed class AnalysisLogBuilder(
    IOptions<AppSettingsOptions> options
) : IAnalysisLogBuilder
{
    private const string CommandLineOptionsHeader = $"[{nameof(CommandLineOptions)}]";
    private const string FilesFoundHeader = "[FilesFound]";
    private const string FileClassificationHeader = $"[{nameof(AnalysisFileClassification)}]";
    private const string NonEcmaModulesHeader = $"[{nameof(AnalysisFileClassification.NonEcmaModules)}]";
    private const string NonEcmaAssembliesHeader = $"[{nameof(AnalysisFileClassification.NonEcmaAssemblies)}]";
    private const string EcmaAssembliesHeader = $"[{nameof(AnalysisFileClassification.EcmaAssemblies)}]";

    private readonly AppSettingsOptions.OutputOptions.ConsoleOptions _options = options.Value.Output.Console;

    public string BuildAnalysisOptionsLog(CommandLineOptions options) =>
        new IndentedTextBuilder(_options.IndentChars)
            .Title(CommandLineOptionsHeader)
            .PushIndent()
            .Line(nameof(CommandLineOptions.AnalysisDirectory), options.AnalysisDirectory)
            .Line(nameof(CommandLineOptions.SearchPatterns), options.SearchPatterns)
            .Line(nameof(CommandLineOptions.RecursiveSearch), options.RecursiveSearch)
            .Line(nameof(CommandLineOptions.ReportFormats), options.ReportFormats)
            .PopIndent()
            .Build();

    public string BuildFoundFilesLog(IEnumerable<FileInfo> files) =>
        new IndentedTextBuilder(_options.IndentChars)
            .Title($"{FilesFoundHeader} Count: {files.Count()}")
            .PushIndent()
            .IndexedItems(files, f => f.FullName)
            .PopIndent()
            .Build();

    public string BuildFileClassificationLog(AnalysisFileClassification fileClassification) =>
        new IndentedTextBuilder(_options.IndentChars)
            .Title(FileClassificationHeader)
            .PushIndent()
            .Title($"{NonEcmaModulesHeader} Count: {fileClassification.NonEcmaModules.Count}")
            .IndexedItems(fileClassification.NonEcmaModules, f => f.FullName)
            .PopIndent()
            .PushIndent()
            .Title($"{NonEcmaAssembliesHeader} Count: {fileClassification.NonEcmaAssemblies.Count}")
            .IndexedItems(fileClassification.NonEcmaAssemblies, f => f.FullName)
            .PopIndent()
            .PushIndent()
            .Title($"{EcmaAssembliesHeader} Count: {fileClassification.EcmaAssemblies.Count}")
            .IndexedItems(fileClassification.EcmaAssemblies, f => f.FullName)
            .PopIndent()
            .Build();
}
