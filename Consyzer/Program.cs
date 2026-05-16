using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using NLog.Extensions.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Consyzer;
using Consyzer.Options;
using Consyzer.Helpers;
using Consyzer.Analyzers;
using Consyzer.Core.Models;
using Consyzer.Core.Classifiers;
using Consyzer.Core.Caching;
using Consyzer.Core.Extractors;
using Consyzer.Core.Cryptography;
using Consyzer.Output.Logging;
using Consyzer.DependencyInjection;
using static Consyzer.Constants.Search;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddCommandLine(args)
    .Build();

var rawOptions = configuration.Get<CommandLineOptions>()!;

using var serviceProvider = new ServiceCollection()

    // Options
    .Configure<CommandLineOptions>(configuration)
    .Configure<AppSettingsOptions>(configuration)

    // Resources
    .AddSingleton<IResourceCache<FileInfo, PEReader>, MetadataOnlyPEReaderCache>()

    // Cryptography
    .AddScoped<IFileHasher, Sha256FileHasher>()

    // Logging
    .AddLogging(builder =>
    {
        builder.ClearProviders();
        builder.AddNLog();
    })
    .AddSingleton<IAnalysisLogBuilder, AnalysisLogBuilder>()

    // Extractors
    .AddScoped<IExtractor<FileInfo, IEnumerable<PInvokeMethod>>, PInvokeMethodExtractor>()
    .AddScoped<IExtractor<MethodDefinition, MethodSignature>, MethodSignatureExtractor>()
    .AddScoped<IExtractor<FileInfo, AssemblyMetadata>, AssemblyMetadataExtractor>()

    // Classifiers
    .AddScoped<IFileClassifier<AnalysisFileClassification>, EcmaFileClassifier>()

    // Analyzers
    .AddScoped<IAnalyzer<IEnumerable<FileInfo>, AnalysisFileClassification>, FileClassificationAnalyzer>()
    .AddScoped<IAnalyzer<IEnumerable<FileInfo>, IEnumerable<AssemblyMetadata>>, AssemblyMetadataAnalyzer>()
    .AddScoped<IAnalyzer<IEnumerable<FileInfo>, IReadOnlyList<PInvokeMethodGroup>>, PInvokeMethodAnalyzer>()
    .AddScoped<IAnalyzer<IEnumerable<PInvokeMethodGroup>, IReadOnlyList<LibraryResolutionResult>>, LibraryResolutionAnalyzer>()
    .AddScoped<IAnalyzer<IEnumerable<LibraryResolutionResult>, AnalysisExitCode>, AnalysisExitCodeAnalyzer>()

    // Reporting
    .AddReportWriters(rawOptions.ReportFormats)
    
    // Orchestrator
    .AddScoped<AnalysisOrchestrator>()

    .BuildServiceProvider();

using var scope = serviceProvider.CreateScope();
var services = scope.ServiceProvider;

var options = services.GetRequiredService<IOptions<CommandLineOptions>>().Value;
var logger = services.GetRequiredService<ILogger<Program>>();
var analysisLogBuilder = services.GetRequiredService<IAnalysisLogBuilder>();

if (string.IsNullOrWhiteSpace(options.AnalysisDirectory))
{
    logger.LogWarning(
        "Required {Parameter} parameter is not specified.",
        nameof(options.AnalysisDirectory)
    );

    return ExitStatus.InvalidInput(InvalidInputReason.NoAnalysisDirectory).ProcessExitCode;
}

if (string.IsNullOrWhiteSpace(options.SearchPatterns))
{
    logger.LogWarning(
        "Required {Parameter} parameter is not specified.",
        nameof(options.SearchPatterns)
    );

    return ExitStatus.InvalidInput(InvalidInputReason.NoSearchPatterns).ProcessExitCode;
}

if (logger.IsEnabled(LogLevel.Debug))
{
    logger.LogDebug("{Message}", analysisLogBuilder.BuildAnalysisOptionsLog(options));
}

try
{
    var files = FileSearchHelper.GetFilesBySeparatedPatterns(
        options.AnalysisDirectory,
        options.SearchPatterns,
        PatternSeparator,
        options.RecursiveSearch
    ).ToList();

    if (files.Count == 0)
    {
        logger.LogWarning("No files found matching the search patterns.");
        return ExitStatus.InvalidInput(InvalidInputReason.NoFilesFound).ProcessExitCode;
    }

    var orchestrator = services.GetRequiredService<AnalysisOrchestrator>();

    var status = orchestrator.Run(files);

    return status.ProcessExitCode;
}
catch (Exception exception)
{
    logger.LogError(exception, "Unhandled error during analysis.");
    return ExitStatus.ToolError().ProcessExitCode;
}
