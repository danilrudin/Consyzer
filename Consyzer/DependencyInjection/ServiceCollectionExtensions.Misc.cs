using Consyzer.Options;
using Consyzer.Output.Reporting;
using Microsoft.Extensions.DependencyInjection;

namespace Consyzer.DependencyInjection;

internal static partial class ServiceCollectionExtensions
{
    public static IServiceCollection AddReportWriters(this IServiceCollection services, OutputFormats formats)
    {
        if (formats.HasFlag(OutputFormats.Console))
            services.AddSingleton<IReportWriter, ConsoleReportWriter>();

        if (formats.HasFlag(OutputFormats.Json))
            services.AddSingleton<IReportWriter, JsonReportWriter>();

        if (formats.HasFlag(OutputFormats.Csv))
            services.AddSingleton<IReportWriter, CsvReportWriter>();

        if (formats.HasFlag(OutputFormats.Xml))
            services.AddSingleton<IReportWriter, XmlReportWriter>();

        return services;
    }
}