using Consyzer.Core.Models.Analysis;
using Consyzer.Core.Models.Metadata;
using Consyzer.Core.Models.Resolution;

namespace Consyzer.Output;

internal static class AnalysisOutputStructure
{
    public static class Section
    {
        public static class Name
        {
            public const string Analysis = nameof(Analysis);
            public const string AssemblyMetadataList = nameof(AnalysisOutcome.AssemblyMetadataList);
            public const string PInvokeMethodGroups = nameof(AnalysisOutcome.PInvokeMethodGroups);
            public const string LibraryResolutionResults = nameof(AnalysisOutcome.LibraryResolutions);
            public const string Summary = nameof(AnalysisOutcome.Summary);
        }

        public static class Bracketed
        {
            public const string Analysis = $"[{Name.Analysis}]";
            public const string AssemblyMetadataList = $"[{Name.AssemblyMetadataList}]";
            public const string PInvokeMethodGroups = $"[{Name.PInvokeMethodGroups}]";
            public const string LibraryResolutionResults = $"[{Name.LibraryResolutionResults}]";
            public const string Summary = $"[{Name.Summary}]";
        }
    }

    public static class Label
    {
        public static class Analysis
        {
            public const string Platform = nameof(AnalysisOutcome.Platform);
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
            public const string ReturnType = nameof(MethodSignature.ReturnType);
            public const string IsStatic = nameof(MethodSignature.IsStatic);
            public const string Namespace = nameof(MethodSignature.Namespace);
            public const string Class = nameof(MethodSignature.Class);
            public const string Method = nameof(MethodSignature.Method);
            public const string MethodArguments = nameof(MethodSignature.MethodArguments);
            public const string ImportName = nameof(PInvokeMethod.ImportName);
            public const string ImportFlags = nameof(PInvokeMethod.ImportFlags);
        }

        public static class Library
        {
            public const string TargetPath = nameof(LibraryResolution.TargetPath);
            public const string Name = nameof(LibraryResolution.LibraryName);
            public const string ResolutionState = nameof(LibraryResolution.ResolutionState);
            public const string ResolvedPath = nameof(ResolvedPresence.Path);
            public const string MechanismKind = nameof(ResolvedPresence.MechanismKind);
            public const string HeuristicCandidates = nameof(LibraryResolution.HeuristicCandidates);
            public const string NotSimulated = nameof(LibraryResolution.NotSimulated);
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
