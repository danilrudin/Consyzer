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
    .AddCommandLine(args)
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var rawOptions = configuration.Get<CommandLineOptions>()!;

var serviceProvider = new ServiceCollection()
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

var options = serviceProvider.GetRequiredService<IOptions<CommandLineOptions>>().Value;
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

if (string.IsNullOrWhiteSpace(options.AnalysisDirectory))
{
    logger.LogError("Required {Parameter} parameter is not specified.", nameof(options.AnalysisDirectory));
    return (int)InvalidInputReason.NoAnalysisDirectory;
}

if (string.IsNullOrWhiteSpace(options.SearchPatterns))
{
    logger.LogError("Required {Parameter} parameter is not specified.", nameof(options.SearchPatterns));
    return (int)InvalidInputReason.NoSearchPatterns;
}

var orchestrator = serviceProvider.GetRequiredService<AnalysisOrchestrator>();

var files = FileSearchHelper.GetFilesBySeparatedPatterns(
    options.AnalysisDirectory,
    options.SearchPatterns,
    PatternSeparator,
    options.RecursiveSearch
);

var status = orchestrator.Run(files);
return (int)status.Code;
