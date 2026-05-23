using Consyzer.Output.Reporting;
using Consyzer.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Consyzer.DependencyInjection;

internal static partial class ServiceCollectionExtensions
{
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