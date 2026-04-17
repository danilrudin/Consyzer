using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Consyzer.Options;
using Consyzer.Analyzers;
using Consyzer.Core.Models;
using Consyzer.Output.Logging;
using Consyzer.Output.Reporting;

namespace Consyzer;

internal sealed class AnalysisOrchestrator(
    ILogger<AnalysisOrchestrator> logger,
    IAnalysisLogBuilder analysisLogBuilder,
    IOptions<CommandLineOptions> analysisOptions,
    IAnalyzer<IEnumerable<FileInfo>, AnalysisFileClassification> fileClassificationAnalyzer,
    IAnalyzer<IEnumerable<FileInfo>, IEnumerable<AssemblyMetadata>> metadataAnalyzer,
    IAnalyzer<IEnumerable<FileInfo>, IReadOnlyList<PInvokeMethodGroup>> pInvokeAnalyzer,
    IAnalyzer<IEnumerable<PInvokeMethodGroup>, IReadOnlyList<LibraryResolutionResult>> libraryResolutionAnalyzer,
    IAnalyzer<IEnumerable<LibraryResolutionResult>, AnalysisExitCode> exitCodeAnalyzer,
    IEnumerable<IReportWriter> reportWriters
)
{
    public ExitStatus Run(IEnumerable<FileInfo> files)
    {
        var fileList = files.ToList();

        logger.LogDebug("{Message}", analysisLogBuilder.BuildAnalysisOptionsLog(analysisOptions.Value));
        logger.LogInformation("Analysis started.");

        if (fileList.Count == 0)
        {
            return new ExitStatus(
                AnalysisExitCode.InvalidInput,
                InvalidInputReason.NoFilesFound,
                "No files found matching the search patterns."
            );
        }

        logger.LogDebug("{Message}", analysisLogBuilder.BuildFoundFilesLog(fileList));

        var fileClassification = fileClassificationAnalyzer.Analyze(fileList);
        logger.LogInformation("{Message}", analysisLogBuilder.BuildFileClassificationLog(fileClassification));

        var ecmaAssemblies = fileClassification.EcmaAssemblies.ToList();
        if (ecmaAssemblies.Count == 0)
        {
            logger.LogError("No valid ECMA assemblies found.");

            return new ExitStatus(
                AnalysisExitCode.InvalidInput,
                InvalidInputReason.AllFilesInvalid,
                "No valid ECMA assemblies found."
            );
        }

        logger.LogInformation("Analyzing assembly metadata...");
        var metadataList = metadataAnalyzer.Analyze(ecmaAssemblies).ToList();

        logger.LogInformation("Analyzing P/Invoke methods...");
        var pInvokeGroups = pInvokeAnalyzer.Analyze(ecmaAssemblies).ToList();

        if (pInvokeGroups.Count == 0)
        {
            logger.LogError("No P/Invoke methods found in the assemblies.");

            return new ExitStatus(
                AnalysisExitCode.InvalidInput,
                InvalidInputReason.NoPInvokeMethodsFound,
                "No P/Invoke methods found in the assemblies."
            );
        }

        logger.LogInformation("Analyzing native library resolution...");
        var libraryResolutions = libraryResolutionAnalyzer.Analyze(pInvokeGroups).ToList();

        var summary = new AnalysisSummary
        {
            TotalFiles = fileList.Count,
            EcmaAssemblies = metadataList.Count,
            AssembliesWithPInvoke = pInvokeGroups.Count,
            TotalPInvokeMethods = pInvokeGroups.Sum(g => g.Methods.Count),
            ResolvedLibraries = libraryResolutions.Count(r => r.State == ResolutionState.Resolved),
            MissingLibraries = libraryResolutions.Count(r => r.State == ResolutionState.Missing),
            InconclusiveLibraries = libraryResolutions.Count(r => r.State == ResolutionState.Inconclusive)
        };

        var outcome = new AnalysisOutcome
        {
            AssemblyMetadataList = metadataList,
            PInvokeMethodGroups = pInvokeGroups,
            LibraryResolutions = libraryResolutions,
            Summary = summary
        };

        foreach (var writer in reportWriters)
        {
            logger.LogInformation("Generating report using {WriterType}...", writer.GetType().Name);
            var destination = writer.Write(outcome);
            logger.LogInformation("Report written to {Destination}.", destination);
        }

        var exitCode = exitCodeAnalyzer.Analyze(libraryResolutions);

        logger.LogInformation("Analysis completed with exit code {ExitCode}.", exitCode);

        return new ExitStatus(exitCode);
    }
}
