using System.Text;
using Microsoft.Extensions.Options;
using Consyzer.Options;
using Consyzer.Output.Builders;
using Consyzer.Core.Models.Analysis;
using Consyzer.Core.Models.Metadata;
using Consyzer.Core.Models.Resolution;
using static Consyzer.Output.AnalysisOutputStructure;

namespace Consyzer.Output.Reporting;

internal sealed class CsvReportWriter(
    IOptions<AppSettingsOptions> options
) : FileReportWriterBase
{
    private const string CsvExtension = ".csv";

    private readonly AppSettingsOptions.OutputOptions.CsvOptions _options = options.Value.Output.Csv;

    protected override string FileExtension => CsvExtension;

    protected override void WriteReport(AnalysisOutcome outcome, string fullPath)
    {
        var encoding = Encoding.GetEncoding(_options.Encoding);
        var builder = new CsvTableBuilder(_options.Delimiter);

        WriteAnalysisInfo(builder, outcome);
        WriteAssemblyMetadata(builder, outcome.AssemblyMetadataList);
        WritePInvokeGroups(builder, outcome.PInvokeMethodGroups);
        WriteLibraryResolutionResults(builder, outcome.LibraryResolutions);
        WriteSummary(builder, outcome.Summary);

        File.WriteAllText(fullPath, builder.Build(), encoding);
    }

    private void WriteAnalysisInfo(CsvTableBuilder builder, AnalysisOutcome outcome)
    {
        builder.Record([Section.Bracketed.Analysis]);

        builder.Header([Label.Analysis.Platform]);
        builder.Record([SerializeValue(outcome.Platform)]);
        builder.Record([]);
    }

    private void WriteAssemblyMetadata(CsvTableBuilder builder, IEnumerable<AssemblyMetadata> metadataList)
    {
        builder.Record([Section.Bracketed.AssemblyMetadataList]);

        builder.Header(
        [
            Label.Assembly.File,
            Label.Assembly.Version,
            Label.Assembly.CreationDateUtc,
            Label.Assembly.Sha256
        ]);

        foreach (var metadata in metadataList)
        {
            builder.Record(
            [
                SerializeValue(metadata.File.Name),
                SerializeValue(metadata.Version),
                SerializeValue(metadata.CreationDateUtc.ToString("O")),
                SerializeValue(metadata.Sha256)
            ]);
        }

        builder.Record([]);
    }

    private void WritePInvokeGroups(CsvTableBuilder builder, IEnumerable<PInvokeMethodGroup> groups)
    {
        builder.Record([Section.Bracketed.PInvokeMethodGroups]);

        var signatureProperties = typeof(MethodSignature).GetProperties();
        var signaturePrefix = nameof(PInvokeMethod.Signature);

        var header = new List<string> { Label.PInvoke.File };
        header.AddRange(signatureProperties.Select(p => $"{signaturePrefix}_{p.Name}"));
        header.Add(Label.PInvoke.ImportName);
        header.Add(Label.PInvoke.ImportFlags);

        builder.Header(header);

        foreach (var group in groups)
        {
            foreach (var method in group.Methods)
            {
                var record = new List<string> { SerializeValue(group.File.FullName) };
                record.AddRange(signatureProperties.Select(p => SerializeValue(p.GetValue(method.Signature))));
                record.Add(SerializeValue(method.ImportName));
                record.Add(SerializeValue(method.ImportFlags.ToString()));

                builder.Record(record);
            }
        }

        builder.Record([]);
    }

    private void WriteLibraryResolutionResults(
        CsvTableBuilder builder,
        IEnumerable<LibraryResolution> libraryResolutions)
    {
        builder.Record([Section.Bracketed.LibraryResolutionResults]);

        builder.Header(
        [
            Label.Library.TargetPath,
            Label.Library.Name,
            Label.Library.ResolutionState,
            Label.Library.ResolvedPath,
            Label.Library.MechanismKind,
            Label.Library.HeuristicCandidates,
            Label.Library.NotSimulated
        ]);

        foreach (var libraryResolution in libraryResolutions)
        {
            builder.Record(
            [
                SerializeValue(libraryResolution.TargetPath),
                SerializeValue(libraryResolution.LibraryName),
                SerializeValue(libraryResolution.ResolutionState.ToString()),
                SerializeValue(libraryResolution.ResolvedPresence?.Path),
                SerializeValue(libraryResolution.ResolvedPresence?.MechanismKind.ToString()),
                SerializeValue(libraryResolution.HeuristicCandidates),
                SerializeValue(libraryResolution.NotSimulated.ToString())
            ]);
        }

        builder.Record([]);
    }

    private void WriteSummary(CsvTableBuilder builder, AnalysisSummary summary)
    {
        builder.Record([Section.Bracketed.Summary]);

        builder.Header(
        [
            Label.Summary.TotalFiles,
            Label.Summary.EcmaAssemblies,
            Label.Summary.AssembliesWithPInvoke,
            Label.Summary.TotalPInvokeMethods,
            Label.Summary.ResolvedLibraries,
            Label.Summary.MissingLibraries,
            Label.Summary.InconclusiveLibraries
        ]);

        builder.Record(
        [
            SerializeValue(summary.TotalFiles),
            SerializeValue(summary.EcmaAssemblies),
            SerializeValue(summary.AssembliesWithPInvoke),
            SerializeValue(summary.TotalPInvokeMethods),
            SerializeValue(summary.ResolvedLibraries),
            SerializeValue(summary.MissingLibraries),
            SerializeValue(summary.InconclusiveLibraries)
        ]);

        builder.Record([]);
    }

    private string SerializeValue(object? value)
    {
        if (value is IEnumerable<string> stringList && value is not string)
        {
            return EscapeList(stringList);
        }

        return EscapeValue(value?.ToString());
    }

    private string EscapeList(IEnumerable<string> items)
    {
        var innerDelimiter = GetSafeInnerDelimiter(_options.Delimiter);
        var joined = string.Join(innerDelimiter, items.Select(item => item ?? string.Empty));

        return EscapeValue(joined);
    }

    private static char GetSafeInnerDelimiter(char delimiter)
        => delimiter switch
        {
            ';' => '|',
            '|' => '/',
            ',' => ';',
            _ => ' '
        };

    private static string EscapeValue(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "\"\"";
        }

        return $"\"{value
            .Replace("\"", "\"\"")
            .Replace('\n', ' ')
            .Replace('\r', ' ')}\"";
    }
}
