using Microsoft.Extensions.Options;
using Consyzer.Options;
using Consyzer.Output.Builders;
using Consyzer.Core.Models.Analysis;

namespace Consyzer.Output.Logging;

internal sealed class AnalysisLogBuilder(
    IOptions<AppSettingsOptions> options
) : IAnalysisLogBuilder
{
    private readonly AppSettingsOptions.OutputOptions.ConsoleOptions _options = options.Value.Output.Console;

    public string BuildAnalysisOptionsLog(CommandLineOptions options) =>
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
            .Title($"{Section.Bracketed.NotEcma} Count: {fileClassification.NonEcmaModules.Count}")
            .IndexedItems(fileClassification.NonEcmaModules, f => f.Name)
            .PopIndent()
            .PushIndent()
            .Title($"{Section.Bracketed.NotAssemblies} Count: {fileClassification.NonEcmaAssemblies.Count}")
            .IndexedItems(fileClassification.NonEcmaAssemblies, f => f.Name)
            .PopIndent()
            .PushIndent()
            .Title($"{Section.Bracketed.EcmaAssemblies} Count: {fileClassification.EcmaAssemblies.Count}")
            .IndexedItems(fileClassification.EcmaAssemblies, f => f.Name)
            .PopIndent()
            .Build();

    private static class Section
    {
        public static class Name
        {
            public const string AnalysisOptions = nameof(CommandLineOptions);
            public const string FilesFound = "FilesFound";
            public const string FileClassification = nameof(AnalysisFileClassification);
            public const string NotEcma = nameof(AnalysisFileClassification.NonEcmaModules);
            public const string NotAssemblies = nameof(AnalysisFileClassification.NonEcmaAssemblies);
            public const string EcmaAssemblies = nameof(AnalysisFileClassification.EcmaAssemblies);
        }

        public static class Bracketed
        {
            public const string AnalysisOptions = $"[{Name.AnalysisOptions}]";
            public const string FilesFound = $"[{Name.FilesFound}]";
            public const string FileClassification = $"[{Name.FileClassification}]";
            public const string NotEcma = $"[{Name.NotEcma}]";
            public const string NotAssemblies = $"[{Name.NotAssemblies}]";
            public const string EcmaAssemblies = $"[{Name.EcmaAssemblies}]";
        }
    }

    private static class Label
    {
        public static class Options
        {
            public const string AnalysisDirectory = nameof(CommandLineOptions.AnalysisDirectory);
            public const string SearchPatterns = nameof(CommandLineOptions.SearchPatterns);
        }
    }
}
