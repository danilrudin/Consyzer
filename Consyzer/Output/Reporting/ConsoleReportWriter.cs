using Microsoft.Extensions.Options;
using Consyzer.Options;
using Consyzer.Core.Models;
using Consyzer.Output.Builders;
using static Consyzer.Constants.Output.Structure;

namespace Consyzer.Output.Reporting;

internal sealed class ConsoleReportWriter(
    IOptions<AppOptions> options
) : IReportWriter
{
    private const string Destination = "Console";

    private readonly AppOptions.OutputOptions.ConsoleOptions _options = options.Value.Output.Console;

    public string Write(AnalysisOutcome outcome)
    {
        var builder = new IndentedTextBuilder(_options.IndentChars);

        WriteAssemblyMetadata(builder, outcome.AssemblyMetadataList);
        WritePInvokeGroups(builder, outcome.PInvokeMethodGroups);
        WriteLibraryPresence(builder, outcome.LibraryPresences);
        WriteSummary(builder, outcome.Summary);

        Console.Out.Write(builder.Build());

        return Destination;
    }

    private static void WriteAssemblyMetadata(IndentedTextBuilder builder, IEnumerable<AssemblyMetadata> metadataList)
    {
        builder
            .Title(Section.Bracketed.AssemblyMetadataList)
            .PushIndent()
            .IndexedSection(metadataList, (b, m) =>
            {
                b.Line($"{Label.Assembly.File}: {m.File.Name}");
                b.Line($"{Label.Assembly.Version}: {m.Version}");
                b.Line($"{Label.Assembly.CreationDateUtc}: {m.CreationDateUtc}");
                b.Line($"{Label.Assembly.Sha256}: {m.Sha256}");
            })
            .PopIndent();
    }

    private static void WritePInvokeGroups(IndentedTextBuilder builder, IEnumerable<PInvokeMethodGroup> groups)
    {
        builder
            .Title(Section.Bracketed.PInvokeMethodGroups)
            .PushIndent()
            .IndexedSection(groups, (b, g) =>
            {
                b.Line($"{Label.PInvoke.File}: {g.File.Name}, Found: {g.Methods.Count()}");
                b.IndexedSection(g.Methods, (bb, m) =>
                {
                    bb.Line($"{Label.PInvoke.Signature}: '{m.Signature}'");
                    bb.Line($"{Label.PInvoke.ImportName}: '{m.ImportName}'");
                    bb.Line($"{Label.PInvoke.ImportFlags}: '{m.ImportFlags}'");
                });
            })
            .PopIndent();
    }

    private static void WriteLibraryPresence(IndentedTextBuilder builder, IEnumerable<LibraryPresence> presences)
    {
        builder
            .Title(Section.Bracketed.LibraryPresences)
            .PushIndent()
            .IndexedSection(presences, (b, p) =>
            {
                b.Line(Label.Library.Name, p.LibraryName);
                b.Line(Label.Library.ResolvedPath, p.ResolvedPath ?? "null");
                b.Line(Label.Library.LocationKind, p.LocationKind);
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
            .Line(Label.Summary.MissingLibraries, summary.MissingLibraries)
            .PopIndent();
    }
}