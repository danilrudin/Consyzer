using Microsoft.Extensions.Logging;
using Consyzer.Application.Analyzers;
using Consyzer.Output.Logging;
using Consyzer.Output.Reporting;
using Consyzer.Core.Models.Exit;
using Consyzer.Core.Models.Analysis;
using Consyzer.Core.Models.Metadata;
using Consyzer.Core.Models.Resolution;

namespace Consyzer.Application;

internal sealed class AnalysisOrchestrator(
    ILogger<AnalysisOrchestrator> logger,
    IAnalysisLogBuilder analysisLogBuilder,
    IAnalyzer<IEnumerable<FileInfo>, AnalysisFileClassification> fileClassificationAnalyzer,
    IAnalyzer<IEnumerable<FileInfo>, IEnumerable<AssemblyMetadata>> metadataAnalyzer,
    IAnalyzer<IEnumerable<FileInfo>, IReadOnlyList<PInvokeMethodGroup>> pInvokeAnalyzer,
    IAnalyzer<IEnumerable<PInvokeMethodGroup>, LibraryResolutionOutcome> libraryResolutionAnalyzer,
    IAnalyzer<IEnumerable<LibraryResolution>, AnalysisExitCode> exitCodeAnalyzer,
    IEnumerable<IReportWriter> reportWriters
)
{
    public ExitStatus Run(IReadOnlyList<FileInfo> files)
    {
        logger.LogInformation("Analysis started.");

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("{Message}", analysisLogBuilder.BuildFoundFilesLog(files));
        }

        var fileClassification = fileClassificationAnalyzer.Analyze(files);
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("{Message}", analysisLogBuilder.BuildFileClassificationLog(fileClassification));
        }

        var ecmaAssemblies = fileClassification.EcmaAssemblies.ToList();
        if (ecmaAssemblies.Count == 0)
        {
            logger.LogWarning("No valid ECMA assemblies found.");
            return ExitStatus.InvalidInput(InvalidInputReason.AllFilesInvalid);
        }

        logger.LogInformation("Analyzing assembly metadata...");
        var metadataList = metadataAnalyzer.Analyze(ecmaAssemblies).ToList();

        logger.LogInformation("Analyzing P/Invoke methods...");
        var pInvokeGroups = pInvokeAnalyzer.Analyze(ecmaAssemblies).ToList();

        if (pInvokeGroups.Count == 0)
        {
            logger.LogInformation("No P/Invoke methods found in the assemblies.");
        }

        logger.LogInformation("Analyzing native library resolution...");
        var libraryResolutionAnalysis = libraryResolutionAnalyzer.Analyze(pInvokeGroups);
        
        var libraryResolutions = libraryResolutionAnalysis.Results;
        var summary = new AnalysisSummary
        {
            TotalFiles = files.Count,
            EcmaAssemblies = metadataList.Count,
            AssembliesWithPInvoke = pInvokeGroups.Count,
            TotalPInvokeMethods = pInvokeGroups.Sum(g => g.Methods.Count),
            ResolvedLibraries = libraryResolutions.Count(r => r.ResolutionState == ResolutionState.Resolved),
            MissingLibraries = libraryResolutions.Count(r => r.ResolutionState == ResolutionState.Missing),
            InconclusiveLibraries = libraryResolutions.Count(r => r.ResolutionState == ResolutionState.Inconclusive)
        };

        var outcome = new AnalysisOutcome
        {
            Platform = libraryResolutionAnalysis.Platform,
            AssemblyMetadataList = metadataList,
            PInvokeMethodGroups = pInvokeGroups,
            LibraryResolutions = libraryResolutions,
            Summary = summary
        };

        foreach (var writer in reportWriters)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Generating report using {WriterType}...", writer.GetType().Name);
            }

            var destination = writer.Write(outcome);
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Report written to {Destination}.", destination);
            }
        }

        var exitCode = exitCodeAnalyzer.Analyze(libraryResolutions);

        var logLevel = GetExitCodeLogLevel(exitCode);
        if (logger.IsEnabled(logLevel))
        {
            logger.Log(
                logLevel,
                "Analysis completed with exit code {ExitCode}.",
                exitCode
            );
        }

        return exitCode switch
        {
            AnalysisExitCode.Success => ExitStatus.Success(),
            AnalysisExitCode.Missing => ExitStatus.Missing(),
            AnalysisExitCode.Inconclusive => ExitStatus.Inconclusive(),
            AnalysisExitCode.ToolError => ExitStatus.ToolError(),
            _ => throw new InvalidOperationException($"Unsupported analysis exit code '{exitCode}'.")
        };
    }

    private static LogLevel GetExitCodeLogLevel(AnalysisExitCode exitCode) =>
        exitCode switch
        {
            AnalysisExitCode.Success => LogLevel.Information,
            AnalysisExitCode.Missing => LogLevel.Warning,
            AnalysisExitCode.Inconclusive => LogLevel.Warning,
            AnalysisExitCode.InvalidInput => LogLevel.Warning,
            AnalysisExitCode.ToolError => LogLevel.Error,
            _ => LogLevel.Information
        };
}
