using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using Consyzer.Options;
using Consyzer.Application;
using Consyzer.Application.Analyzers;
using Consyzer.Core.Caching;
using Consyzer.Core.Extractors;
using Consyzer.Core.Classifiers;
using Consyzer.Core.Cryptography;
using Consyzer.Core.Models.Exit;
using Consyzer.Core.Models.Analysis;
using Consyzer.Core.Models.Resolution;
using Consyzer.Core.Models.Metadata;
using Consyzer.Output.Logging;

namespace Consyzer.DependencyInjection;

internal static partial class ServiceCollectionExtensions
{
    public static IServiceCollection AddConsyzerOptions(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<CommandLineOptions>(configuration);
        services.Configure<AppSettingsOptions>(configuration);

        return services;
    }

    public static IServiceCollection AddConsyzerLogging(this IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddNLog();
        });

        services.AddSingleton<IAnalysisLogBuilder, AnalysisLogBuilder>();

        return services;
    }

    public static IServiceCollection AddConsyzerCore(this IServiceCollection services)
    {
        services.AddSingleton<IResourceCache<FileInfo, PEReader>, MetadataOnlyPEReaderCache>();

        services.AddScoped<IFileHasher, Sha256FileHasher>();

        services.AddScoped<IExtractor<FileInfo, IEnumerable<PInvokeMethod>>, PInvokeMethodExtractor>();
        services.AddScoped<IExtractor<MethodDefinition, MethodSignature>, MethodSignatureExtractor>();
        services.AddScoped<IExtractor<FileInfo, AssemblyMetadata>, AssemblyMetadataExtractor>();

        services.AddScoped<IFileClassifier<AnalysisFileClassification>, EcmaFileClassifier>();

        return services;
    }

    public static IServiceCollection AddConsyzerApplication(this IServiceCollection services)
    {
        services.AddScoped<IAnalyzer<IEnumerable<FileInfo>, AnalysisFileClassification>, FileClassificationAnalyzer>();
        services.AddScoped<IAnalyzer<IEnumerable<FileInfo>, IEnumerable<AssemblyMetadata>>, AssemblyMetadataAnalyzer>();
        services.AddScoped<IAnalyzer<IEnumerable<FileInfo>, IReadOnlyList<PInvokeMethodGroup>>, PInvokeMethodAnalyzer>();
        services.AddScoped<IAnalyzer<IEnumerable<PInvokeMethodGroup>, LibraryResolutionOutcome>, LibraryResolutionAnalyzer>();
        services.AddScoped<IAnalyzer<IEnumerable<LibraryResolution>, AnalysisExitCode>, AnalysisExitCodeAnalyzer>();

        services.AddScoped<AnalysisOrchestrator>();

        return services;
    }

    public static IServiceCollection AddConsyzerOutput(
        this IServiceCollection services,
        CommandLineOptions.OutputFormats formats
    )
    {
        services.AddReportWriters(formats);

        return services;
    }
}
