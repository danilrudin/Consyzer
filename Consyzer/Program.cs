using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Consyzer.Input;
using Consyzer.Options;
using Consyzer.Application;
using Consyzer.Output.Logging;
using Consyzer.Core.Models.Exit;
using Consyzer.DependencyInjection;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddCommandLine(args)
    .Build();

CommandLineOptions options;

try
{
    options = configuration.Get<CommandLineOptions>()
        ?? throw new InvalidOperationException("Command-line options could not be bound.");
}
catch (InvalidOperationException exception)
{
    Console.Error.WriteLine($"Invalid command-line options: {exception.Message}");
    return ExitStatus.InvalidInput(InvalidInputReason.InvalidOptionValue).ProcessExitCode;
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

if (!Directory.Exists(options.AnalysisDirectory))
{
    logger.LogWarning(
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
        logger.LogWarning("No files found matching the search patterns.");
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
