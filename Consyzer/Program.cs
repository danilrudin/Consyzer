using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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

var rawOptions = configuration.Get<CommandLineOptions>()!;

using var serviceProvider = new ServiceCollection()
    .AddConsyzerOptions(configuration)
    .AddConsyzerLogging()
    .AddConsyzerCore()
    .AddConsyzerApplication()
    .AddConsyzerOutput(rawOptions.ReportFormats)
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

    var orchestrator = services.GetRequiredService<AnalysisOrchestrator>();

    var status = orchestrator.Run(files);

    return status.ProcessExitCode;
}
catch (Exception exception)
{
    logger.LogError(exception, "Unhandled error during analysis.");
    return ExitStatus.ToolError().ProcessExitCode;
}
