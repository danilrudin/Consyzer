using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Consyzer.Input;
using Consyzer.Options;
using Consyzer.Application;
using Consyzer.Output.Logging;
using Consyzer.Core.Models.Exit;
using Consyzer.DependencyInjection;

IConfigurationRoot configuration;
CommandLineOptions options;

try
{
    configuration = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: false)
        .AddCommandLine(args)
        .Build();

    options = configuration.Get<CommandLineOptions>()
        ?? throw new InvalidOperationException("Command-line options could not be bound.");
}
catch (Exception exception) when (exception is InvalidOperationException or ArgumentException or FormatException)
{
    await Console.Error.WriteLineAsync($"Invalid command-line options: {exception.Message}");
    return ExitStatus.InvalidInput(InvalidInputReason.InvalidOptionValue).ProcessExitCode;
}
catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
{
    await Console.Error.WriteLineAsync($"Could not read configuration: {exception.Message}");
    return ExitStatus.ToolError().ProcessExitCode;
}

using var serviceProvider = new ServiceCollection()
    .AddOptions(configuration, options)
    .AddAnalysisLogging()
    .AddCore()
    .AddRequiredServices()
    .AddReportWriters(options.ReportFormats)
    .BuildServiceProvider();

using var scope = serviceProvider.CreateScope();
var scopedServices = scope.ServiceProvider;

var logger = scopedServices.GetRequiredService<ILogger<Program>>();
var analysisLogBuilder = scopedServices.GetRequiredService<IAnalysisLogBuilder>();

if (string.IsNullOrWhiteSpace(options.AnalysisDirectory))
{
    logger.LogError(
        "Required {Parameter} parameter is not specified.",
        nameof(options.AnalysisDirectory)
    );

    return ExitStatus.InvalidInput(InvalidInputReason.NoAnalysisDirectory).ProcessExitCode;
}

if (string.IsNullOrWhiteSpace(options.SearchPatterns))
{
    logger.LogError(
        "Required {Parameter} parameter is not specified.",
        nameof(options.SearchPatterns)
    );

    return ExitStatus.InvalidInput(InvalidInputReason.NoSearchPatterns).ProcessExitCode;
}

if (!Directory.Exists(options.AnalysisDirectory))
{
    logger.LogError(
        "Analysis directory '{AnalysisDirectory}' does not exist.",
        options.AnalysisDirectory
    );

    return ExitStatus.InvalidInput(
        InvalidInputReason.AnalysisDirectoryNotFound
    ).ProcessExitCode;
}

if (logger.IsEnabled(LogLevel.Debug))
{
    logger.LogDebug("{Message}", analysisLogBuilder.BuildAnalysisOptionsLog(options));
}

const char SearchPatternSeparator = ',';

try
{
    var files = AnalysisFileFinder.FindBySeparatedPatterns(
        options.AnalysisDirectory,
        options.SearchPatterns,
        SearchPatternSeparator,
        options.RecursiveSearch
    ).ToList();

    if (files.Count == 0)
    {
        logger.LogInformation("No files found matching the search patterns.");
        return ExitStatus.InvalidInput(InvalidInputReason.NoFilesFound).ProcessExitCode;
    }

    var orchestrator = scopedServices.GetRequiredService<AnalysisOrchestrator>();

    var status = orchestrator.Run(files);

    return status.ProcessExitCode;
}
catch (Exception exception)
{
    logger.LogError(exception, "Unhandled error during analysis.");
    return ExitStatus.ToolError().ProcessExitCode;
}
