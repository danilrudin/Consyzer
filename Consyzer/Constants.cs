using Consyzer.Options;
using Consyzer.Helpers;
using Consyzer.Core.Models;

namespace Consyzer;

internal static class Constants
{
    public static class Search
    {
        public const char PatternSeparator = ',';
    }

    internal static class Output
    {
        public static class Destination
        {
            public static readonly string TargetDirectory = Path.Combine(AppContext.BaseDirectory, Directory);

            public static string Json => $"{Prefix}{Identifier}{JsonExtension}";
            public static string Csv => $"{Prefix}{Identifier}{CsvExtension}";
            public static string Xml => $"{Prefix}{Identifier}{XmlExtension}";

            private const string Prefix = "report_";
            private const string Directory = "Reports";
            private const string ReserveIdentifier = "fallback";

            private const string JsonExtension = ".json";
            private const string CsvExtension = ".csv";
            private const string XmlExtension = ".xml";

            private static readonly string Identifier = Path.GetFileNameWithoutExtension(
                LoggingHelper.GetLogFileName() ?? ReserveIdentifier);
        }

        public static class Structure
        {
            public static class Section
            {
                public static class Name
                {
                    public const string AnalysisOptions = nameof(Options.CommandLineOptions);
                    public const string FilesFound = "FilesFound";
                    public const string FileClassification = nameof(AnalysisFileClassification);
                    public const string NotEcma = nameof(AnalysisFileClassification.NonEcmaModules);
                    public const string NotAssemblies = nameof(AnalysisFileClassification.NonEcmaAssemblies);
                    public const string EcmaAssemblies = nameof(AnalysisFileClassification.EcmaAssemblies);
                    public const string AssemblyMetadataList = nameof(AnalysisOutcome.AssemblyMetadataList);
                    public const string PInvokeMethodGroups = nameof(AnalysisOutcome.PInvokeMethodGroups);
                    public const string LibraryResolutionResults = nameof(AnalysisOutcome.LibraryResolutions);
                    public const string Summary = nameof(AnalysisOutcome.Summary);
                }

                public static class Bracketed
                {
                    public const string AnalysisOptions = $"[{Name.AnalysisOptions}]";
                    public const string FilesFound = $"[{Name.FilesFound}]";
                    public const string FileClassification = $"[{Name.FileClassification}]";
                    public const string NotEcma = $"[{Name.NotEcma}]";
                    public const string NotAssemblies = $"[{Name.NotAssemblies}]";
                    public const string EcmaAssemblies = $"[{Name.EcmaAssemblies}]";
                    public const string AssemblyMetadataList = $"[{Name.AssemblyMetadataList}]";
                    public const string PInvokeMethodGroups = $"[{Name.PInvokeMethodGroups}]";
                    public const string LibraryResolutionResults = $"[{Name.LibraryResolutionResults}]";
                    public const string Summary = $"[{Name.Summary}]";
                }
            }

            public static class Element
            {
                public const string Assembly = nameof(Assembly);
                public const string Group = nameof(Group);
                public const string Method = nameof(Method);
                public const string Library = nameof(Library);
                public const string Candidate = nameof(Candidate);
            }

            public static class Label
            {
                public static class Options
                {
                    public const string AnalysisDirectory = nameof(CommandLineOptions.AnalysisDirectory);
                    public const string SearchPatterns = nameof(CommandLineOptions.SearchPatterns);
                }

                public static class Assembly
                {
                    public const string File = nameof(AssemblyMetadata.File);
                    public const string Version = nameof(AssemblyMetadata.Version);
                    public const string CreationDateUtc = nameof(AssemblyMetadata.CreationDateUtc);
                    public const string Sha256 = nameof(AssemblyMetadata.Sha256);
                }

                public static class PInvoke
                {
                    public const string File = nameof(PInvokeMethodGroup.File);
                    public const string Signature = nameof(PInvokeMethod.Signature);
                    public const string ImportName = nameof(PInvokeMethod.ImportName);
                    public const string ImportFlags = nameof(PInvokeMethod.ImportFlags);
                }

                public static class Library
                {
                    public const string TargetPath = nameof(LibraryResolutionResult.TargetPath);
                    public const string Name = nameof(LibraryResolutionResult.LibraryName);
                    public const string Platform = nameof(LibraryResolutionResult.Platform);
                    public const string ResolutionState = nameof(LibraryResolutionResult.ResolutionState);
                    public const string ResolvedPath = nameof(ResolvedPresence.Path);
                    public const string MechanismKind = nameof(ResolvedPresence.MechanismKind);
                    public const string HeuristicCandidates = nameof(LibraryResolutionResult.HeuristicCandidates);
                    public const string NotSimulated = nameof(LibraryResolutionResult.NotSimulated);
                }

                public static class Summary
                {
                    public const string TotalFiles = nameof(AnalysisSummary.TotalFiles);
                    public const string EcmaAssemblies = nameof(AnalysisSummary.EcmaAssemblies);
                    public const string AssembliesWithPInvoke = nameof(AnalysisSummary.AssembliesWithPInvoke);
                    public const string TotalPInvokeMethods = nameof(AnalysisSummary.TotalPInvokeMethods);
                    public const string ResolvedLibraries = nameof(AnalysisSummary.ResolvedLibraries);
                    public const string MissingLibraries = nameof(AnalysisSummary.MissingLibraries);
                    public const string InconclusiveLibraries = nameof(AnalysisSummary.InconclusiveLibraries);
                }
            }
        }
    }
}
