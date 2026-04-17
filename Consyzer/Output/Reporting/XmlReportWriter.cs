using System.Xml;
using System.Text;
using Microsoft.Extensions.Options;
using Consyzer.Options;
using Consyzer.Core.Models;
using static Consyzer.Constants.Output;

namespace Consyzer.Output.Reporting;

internal sealed class XmlReportWriter(
    IOptions<AppSettingsOptions> options
) : IReportWriter
{
    private const string ReportName = "ConsyzerReport";

    private readonly AppSettingsOptions.OutputOptions.XmlOptions _options = options.Value.Output.Xml;

    public string Write(AnalysisOutcome outcome)
    {
        Directory.CreateDirectory(Destination.TargetDirectory);
        var fullPath = Path.Combine(Destination.TargetDirectory, Destination.Xml);

        var encoding = Encoding.GetEncoding(_options.Encoding);

        using var writer = XmlWriter.Create(fullPath, new XmlWriterSettings
        {
            Indent = true,
            Encoding = encoding,
            IndentChars = _options.IndentChars
        });

        writer.WriteStartDocument();
        writer.WriteStartElement(ReportName);

        WriteAssemblyMetadata(writer, outcome.AssemblyMetadataList);
        WritePInvokeGroups(writer, outcome.PInvokeMethodGroups);
        WriteLibraryResolutions(writer, outcome.LibraryResolutions);
        WriteSummary(writer, outcome.Summary);

        writer.WriteEndElement();
        writer.WriteEndDocument();

        return fullPath;
    }

    private static void WriteAssemblyMetadata(XmlWriter writer, IEnumerable<AssemblyMetadata> metadataList)
    {
        writer.WriteStartElement(Structure.Section.Name.AssemblyMetadataList);

        foreach (var info in metadataList)
        {
            writer.WriteStartElement(Structure.Element.Assembly);
            writer.WriteElementString(Structure.Label.Assembly.File, info.File.Name);
            writer.WriteElementString(Structure.Label.Assembly.Version, info.Version);
            writer.WriteElementString(
                Structure.Label.Assembly.CreationDateUtc,
                info.CreationDateUtc.ToString("O")
            );
            writer.WriteElementString(Structure.Label.Assembly.Sha256, info.Sha256);
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    private static void WritePInvokeGroups(XmlWriter writer, IEnumerable<PInvokeMethodGroup> groups)
    {
        writer.WriteStartElement(Structure.Section.Name.PInvokeMethodGroups);

        foreach (var group in groups)
        {
            writer.WriteStartElement(Structure.Element.Group);
            writer.WriteAttributeString(Structure.Label.PInvoke.File, group.File.Name);

            foreach (var method in group.Methods)
            {
                writer.WriteStartElement(Structure.Element.Method);
                writer.WriteElementString(Structure.Label.PInvoke.Signature, method.Signature.ToString());
                writer.WriteElementString(Structure.Label.PInvoke.ImportName, method.ImportName);
                writer.WriteElementString(Structure.Label.PInvoke.ImportFlags, method.ImportFlags.ToString());
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    private static void WriteLibraryResolutions(
        XmlWriter writer,
        IEnumerable<LibraryResolutionResult> libraryResolutions
    )
    {
        writer.WriteStartElement(Structure.Section.Name.LibraryResolutionResults);

        foreach (var libraryResolution in libraryResolutions)
        {
            writer.WriteStartElement(Structure.Element.Library);

            writer.WriteElementString(Structure.Label.Library.Name, libraryResolution.LibraryName);
            writer.WriteElementString(Structure.Label.Library.State, libraryResolution.State.ToString());

            if (libraryResolution.Resolved is not null)
            {
                writer.WriteElementString(Structure.Label.Library.ResolvedPath, libraryResolution.Resolved.Path);

                writer.WriteElementString(
                    Structure.Label.Library.MechanismKind,
                    libraryResolution.Resolved.MechanismKind.ToString()
                );
            }

            writer.WriteStartElement(Structure.Label.Library.HeuristicCandidates);

            foreach (var heuristicCandidate in libraryResolution.HeuristicCandidates)
            {
                writer.WriteElementString(Structure.Element.Candidate, heuristicCandidate);
            }

            writer.WriteEndElement();

            writer.WriteElementString(
                Structure.Label.Library.NotSimulated,
                libraryResolution.NotSimulated.ToString()
            );

            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    private static void WriteSummary(XmlWriter writer, AnalysisSummary summary)
    {
        writer.WriteStartElement(Structure.Section.Name.Summary);

        writer.WriteElementString(Structure.Label.Summary.TotalFiles, summary.TotalFiles.ToString());
        writer.WriteElementString(Structure.Label.Summary.EcmaAssemblies, summary.EcmaAssemblies.ToString());
        writer.WriteElementString(Structure.Label.Summary.AssembliesWithPInvoke, summary.AssembliesWithPInvoke.ToString());
        writer.WriteElementString(Structure.Label.Summary.TotalPInvokeMethods, summary.TotalPInvokeMethods.ToString());
        writer.WriteElementString(Structure.Label.Summary.ResolvedLibraries, summary.ResolvedLibraries.ToString());
        writer.WriteElementString(Structure.Label.Summary.MissingLibraries, summary.MissingLibraries.ToString());
        writer.WriteElementString(Structure.Label.Summary.InconclusiveLibraries, summary.InconclusiveLibraries.ToString());

        writer.WriteEndElement();
    }
}