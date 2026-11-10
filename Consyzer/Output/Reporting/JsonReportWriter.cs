using System.Text;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Reflection;
using Microsoft.Extensions.Options;
using Consyzer.Options;
using Consyzer.Core.Models.Analysis;
using Consyzer.Core.Models.Resolution;
using Consyzer.Output.Reporting.Converters;

namespace Consyzer.Output.Reporting;

internal sealed class JsonReportWriter(
    IOptions<AppSettingsOptions> options
) : FileReportWriterBase
{
    private const string JsonExtension = ".json";

    private readonly AppSettingsOptions.OutputOptions.JsonOptions _options = options.Value.Output.Json;

    protected override string FileExtension => JsonExtension;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = 
        { 
            new JsonFileInfoConverter(),
            new JsonEnumConverter<ResolutionState>(),
            new JsonEnumConverter<MechanismKind>(),
            new JsonEnumConverter<NotSimulatedMechanisms>(),
            new JsonEnumConverter<MethodImportAttributes>()
        }
    };

    protected override void WriteReport(AnalysisOutcome outcome, string fullPath)
    {
        var encoding = Encoding.GetEncoding(_options.Encoding);

        // Keep the public report shape independent of the internal resolution model.
        var report = new
        {
            Analysis = new { outcome.Platform },
            AssemblyMetadataList = outcome.AssemblyMetadataList.Select(metadata => new
            {
                metadata.File,
                metadata.Version,
                CreationDateUtc = metadata.CreationDateUtc.ToString("O"),
                metadata.Sha256
            }),
            outcome.PInvokeMethodGroups,
            LibraryResolutions = outcome.LibraryResolutions.Select(resolution => new
            {
                resolution.TargetPath,
                resolution.LibraryName,
                resolution.ResolutionState,
                ResolvedPath = resolution.ResolvedPresence?.Path,
                MechanismKind = resolution.ResolvedPresence?.MechanismKind,
                resolution.HeuristicCandidates,
                resolution.NotSimulated
            }),
            outcome.Summary
        };
        var json = JsonSerializer.Serialize(report, JsonOptions);
        File.WriteAllText(fullPath, json, encoding);
    }
}
