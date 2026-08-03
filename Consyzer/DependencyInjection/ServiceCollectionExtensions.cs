using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Consyzer.Options;
using Consyzer.Application;
using Consyzer.Application.Analyzers;
using Consyzer.Core.Caching;
using Consyzer.Core.Resolvers;
using Consyzer.Core.Extractors;
using Consyzer.Core.Classifiers;
using Consyzer.Core.Cryptography;
using Consyzer.Output.Logging;
using Consyzer.Output.Reporting;
using Consyzer.Core.Models.Exit;
using Consyzer.Core.Models.Analysis;
using Consyzer.Core.Models.Metadata;
using Consyzer.Core.Models.Resolution;
using NLog.Extensions.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Consyzer.DependencyInjection;

internal static partial class ServiceCollectionExtensions
{
    public static IServiceCollection AddRequiredServices(this IServiceCollection services)
    {
        services.AddScoped<IAnalyzer<IEnumerable<FileInfo>, AnalysisFileClassification>, FileClassificationAnalyzer>();
        services.AddScoped<IAnalyzer<IEnumerable<FileInfo>, IEnumerable<AssemblyMetadata>>, AssemblyMetadataAnalyzer>();
        services.AddScoped<IAnalyzer<IEnumerable<FileInfo>, IReadOnlyList<PInvokeMethodGroup>>, PInvokeMethodAnalyzer>();
        services.AddScoped<IAnalyzer<IEnumerable<PInvokeMethodGroup>, LibraryResolutionOutcome>, LibraryResolutionAnalyzer>();
        services.AddScoped<IAnalyzer<AnalysisOutcomeInput, AnalysisOutcome>, AnalysisOutcomeAnalyzer>();
        services.AddScoped<IAnalyzer<IEnumerable<LibraryResolution>, ExitStatus>, AnalysisExitCodeAnalyzer>();

        services.AddScoped<AnalysisOrchestrator>();

        return services;
    }

    public static IServiceCollection AddOptions(
        this IServiceCollection services,
        IConfiguration configuration,
        CommandLineOptions commandLineOptions
    )
    {
        services.AddSingleton(
            Microsoft.Extensions.Options.Options.Create(commandLineOptions)
        );
        services.Configure<AppSettingsOptions>(configuration);

        return services;
    }

    public static IServiceCollection AddCore(this IServiceCollection services)
    {
        services.AddSingleton<IResourceCache<FileInfo, PEReader>, MetadataOnlyPEReaderCache>();

        services.AddScoped<IFileHasher, Sha256FileHasher>();

        services.AddScoped<IExtractor<FileInfo, IEnumerable<PInvokeMethod>>, PInvokeMethodExtractor>();
        services.AddScoped<IExtractor<MethodDefinition, MethodSignature>, MethodSignatureExtractor>();
        services.AddScoped<IExtractor<FileInfo, AssemblyMetadata>, AssemblyMetadataExtractor>();

        services.AddScoped<IFileClassifier<AnalysisFileClassification>, EcmaFileClassifier>();
        services.AddScoped<ILibraryResolutionResolver>(provider =>
        {
            var options = provider
                .GetRequiredService<Microsoft.Extensions.Options.IOptions<CommandLineOptions>>()
                .Value;

            return new MultiPlatformLibraryResolutionResolver(options.AnalysisDirectory);
        });

        return services;
    }

    public static IServiceCollection AddAnalysisLogging(this IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddNLog();
        });

        services.AddSingleton<IAnalysisLogBuilder, AnalysisLogBuilder>();

        return services;
    }

    public static IServiceCollection AddReportWriters(
        this IServiceCollection services,
        CommandLineOptions.OutputFormats formats
    )
    {
        if (formats.HasFlag(CommandLineOptions.OutputFormats.Console))
            services.AddSingleton<IReportWriter, ConsoleReportWriter>();

        if (formats.HasFlag(CommandLineOptions.OutputFormats.Json))
            services.AddSingleton<IReportWriter, JsonReportWriter>();

        if (formats.HasFlag(CommandLineOptions.OutputFormats.Csv))
            services.AddSingleton<IReportWriter, CsvReportWriter>();

        if (formats.HasFlag(CommandLineOptions.OutputFormats.Xml))
            services.AddSingleton<IReportWriter, XmlReportWriter>();

        return services;
    }
}
