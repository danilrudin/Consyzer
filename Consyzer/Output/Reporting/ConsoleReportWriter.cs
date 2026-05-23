using Microsoft.Extensions.Options;
using Consyzer.Options;
using Consyzer.Output.Builders;
using Consyzer.Core.Models.Analysis;
using Consyzer.Core.Models.Metadata;
using Consyzer.Core.Models.Resolution;
using static Consyzer.Output.AnalysisOutputStructure.Structure;

namespace Consyzer.Output.Reporting;

internal sealed class ConsoleReportWriter(
    IOptions<AppSettingsOptions> options
) : IReportWriter
{
    private const string Destination = "Console";

    private readonly AppSettingsOptions.OutputOptions.ConsoleOptions _options = options.Value.Output.Console;

    public string Write(AnalysisOutcome outcome)
    {
        var builder = new IndentedTextBuilder(_options.IndentChars);

        WriteAssemblyMetadata(builder, outcome.AssemblyMetadataList);
        WritePInvokeGroups(builder, outcome.PInvokeMethodGroups);
        WriteLibraryResolutions(builder, outcome.LibraryResolutions);
        WriteSummary(builder, outcome.Summary);

        Console.Out.Write(builder.Build());

        return Destination;
    }

    private static void WriteAssemblyMetadata(
        IndentedTextBuilder builder,
        IEnumerable<AssemblyMetadata> metadataList
    )
    {
        builder
            .Title(Section.Bracketed.AssemblyMetadataList)
            .PushIndent()
            .IndexedSection(metadataList, (b, metadata) =>
            {
                b.Line(Label.Assembly.File, metadata.File.Name);
                b.Line(Label.Assembly.Version, metadata.Version);
                b.Line(Label.Assembly.CreationDateUtc, metadata.CreationDateUtc.ToString("O"));
                b.Line(Label.Assembly.Sha256, metadata.Sha256);
            })
            .PopIndent();
    }

    private static void WritePInvokeGroups(
        IndentedTextBuilder builder,
        IEnumerable<PInvokeMethodGroup> groups
    )
    {
        builder
            .Title(Section.Bracketed.PInvokeMethodGroups)
            .PushIndent()
            .IndexedSection(groups, (b, group) =>
            {
                b.Line(Label.PInvoke.File, $"{group.File.Name}, Found: {group.Methods.Count}");

                b.IndexedSection(group.Methods, (bb, method) =>
                {
                    bb.Line(Label.PInvoke.Signature, $"'{method.Signature}'");
                    bb.Line(Label.PInvoke.ImportName, $"'{method.ImportName}'");
                    bb.Line(Label.PInvoke.ImportFlags, $"'{method.ImportFlags}'");
                });
            })
            .PopIndent();
    }

    private static void WriteLibraryResolutions(
        IndentedTextBuilder builder,
        IEnumerable<LibraryResolutionResult> libraryResolutions
    )
    {
        builder
            .Title(Section.Bracketed.LibraryResolutionResults)
            .PushIndent()
            .IndexedSection(libraryResolutions, (b, libraryResolution) =>
            {
                b.Line(Label.Library.TargetPath, libraryResolution.TargetPath);
                b.Line(Label.Library.Name, libraryResolution.LibraryName);
                b.Line(Label.Library.Platform, libraryResolution.Platform);
                b.Line(Label.Library.ResolutionState, libraryResolution.ResolutionState);
                b.Line(Label.Library.ResolvedPath, libraryResolution.ResolvedPresence?.Path ?? "null");
                b.Line(Label.Library.MechanismKind, libraryResolution.ResolvedPresence?.MechanismKind.ToString() ?? "null");

                b.Line(
                    Label.Library.HeuristicCandidates,
                    libraryResolution.HeuristicCandidates.Count == 0
                        ? "[]"
                        : string.Join(", ", libraryResolution.HeuristicCandidates));

                b.Line(
                    Label.Library.NotSimulated,
                    libraryResolution.NotSimulated == NotSimulatedMechanisms.None
                        ? "None"
                        : libraryResolution.NotSimulated);
            })
            .PopIndent();
    }

    private static void WriteSummary(IndentedTextBuilder builder, AnalysisSummary summary)
    {
        builder
            .Title(Section.Bracketed.Summary)
            .PushIndent()
            .Line(Label.Summary.TotalFiles, summary.TotalFiles)
            .Line(Label.Summary.EcmaAssemblies, summary.EcmaAssemblies)
            .Line(Label.Summary.AssembliesWithPInvoke, summary.AssembliesWithPInvoke)
            .Line(Label.Summary.TotalPInvokeMethods, summary.TotalPInvokeMethods)
            .Line(Label.Summary.ResolvedLibraries, summary.ResolvedLibraries)
            .Line(Label.Summary.MissingLibraries, summary.MissingLibraries)
            .Line(Label.Summary.InconclusiveLibraries, summary.InconclusiveLibraries)
            .PopIndent();
    }
}
