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
using Consyzer.Core.Resources;
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

var rawOptions = configuration.Get<AnalysisOptions>()!;

var serviceProvider = new ServiceCollection()
    // Options
    .Configure<AnalysisOptions>(configuration)
    .Configure<AppOptions>(configuration)

    // Resources
    .AddSingleton<IResourceAccessor<FileInfo, Stream>, FileStreamAccessor>()
    .AddSingleton<IResourceAccessor<FileInfo, PEReader>, PEReaderAccessor>()

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
    .AddScoped<IFileClassifier<AnalysisFileClassification>, AnalysisFileClassifier>()

    // Analyzers
    .AddScoped<IAnalyzer<IEnumerable<FileInfo>, AnalysisFileClassification>, FileClassificationAnalyzer>()
    .AddScoped<IAnalyzer<IEnumerable<FileInfo>, IEnumerable<AssemblyMetadata>>, AssemblyMetadataAnalyzer>()
    .AddScoped<IAnalyzer<IEnumerable<FileInfo>, IEnumerable<PInvokeMethodGroup>>, PInvokeMethodAnalyzer>()
    .AddScoped<IAnalyzer<IEnumerable<PInvokeMethodGroup>, IEnumerable<LibraryPresence>>, LibraryPresenceAnalyzer>()
    .AddScoped<IAnalyzer<IEnumerable<LibraryPresence>, LibraryLocationKind>, LibraryPresenceStatusAnalyzer>()

    // Reporting
    .AddReportWriters(rawOptions.ReportFormats)

    // Orchestrator
    .AddScoped<AnalysisOrchestrator>()

    .BuildServiceProvider();

var options = serviceProvider.GetRequiredService<IOptions<AnalysisOptions>>().Value;
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

if (string.IsNullOrWhiteSpace(options.AnalysisDirectory))
{
    logger.LogError("Required {Parameter} parameter is not specified.", nameof(options.AnalysisDirectory));
    return (int)AppFailureCode.NoAnalysisDirectory;
}

if (string.IsNullOrWhiteSpace(options.SearchPatterns))
{
    logger.LogError("Required {Parameter} parameter is not specified.", nameof(options.SearchPatterns));
    return (int)AppFailureCode.NoSearchPatterns;
}

var orchestrator = serviceProvider.GetRequiredService<AnalysisOrchestrator>();

var files = FileSearchHelper.GetFilesBySeparatedPatterns(
    options.AnalysisDirectory,
    options.SearchPatterns,
    PatternSeparator,
    options.RecursiveSearch
);
return orchestrator.Run(files);
