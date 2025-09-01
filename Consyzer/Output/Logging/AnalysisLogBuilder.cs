using Microsoft.Extensions.Options;
using Consyzer.Options;
using Consyzer.Core.Models;
using Consyzer.Output.Builders;
using static Consyzer.Constants.Output.Structure;

namespace Consyzer.Output.Logging;

internal sealed class AnalysisLogBuilder(
    IOptions<AppOptions> options
) : IAnalysisLogBuilder
{
    private readonly AppOptions.OutputOptions.ConsoleOptions _options = options.Value.Output.Console;

    public string BuildAnalysisOptionsLog(AnalysisOptions options) =>
        new IndentedTextBuilder(_options.IndentChars)
            .Title(Section.Bracketed.AnalysisOptions)
            .PushIndent()
            .Line(Label.Options.AnalysisDirectory, options.AnalysisDirectory)
            .Line(Label.Options.SearchPatterns, options.SearchPatterns)
            .PopIndent()
            .Build();

    public string BuildFoundFilesLog(IEnumerable<FileInfo> files) =>
        new IndentedTextBuilder(_options.IndentChars)
            .Title($"{Section.Bracketed.FilesFound} Count: {files.Count()}")
            .PushIndent()
            .IndexedItems(files, f => f.Name)
            .PopIndent()
            .Build();

    public string BuildFileClassificationLog(AnalysisFileClassification fileClassification) =>
        new IndentedTextBuilder(_options.IndentChars)
            .Title(Section.Bracketed.FileClassification)
            .PushIndent()
            .Title($"{Section.Bracketed.NotEcma} Count: {fileClassification.NonEcmaModules.Count()}")
            .IndexedItems(fileClassification.NonEcmaModules, f => f.Name)
            .PopIndent()
            .PushIndent()
            .Title($"{Section.Bracketed.NotAssemblies} Count: {fileClassification.NonEcmaAssemblies.Count()}")
            .IndexedItems(fileClassification.NonEcmaAssemblies, f => f.Name)
            .PopIndent()
            .PushIndent()
            .Title($"{Section.Bracketed.EcmaAssemblies} Count: {fileClassification.EcmaAssemblies.Count()}")
            .IndexedItems(fileClassification.EcmaAssemblies, f => f.Name)
            .PopIndent()
            .Build();
}