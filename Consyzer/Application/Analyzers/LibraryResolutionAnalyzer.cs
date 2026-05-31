using Microsoft.Extensions.Options;
using Consyzer.Options;
using Consyzer.Core.Resolvers;
using Consyzer.Core.Models.Metadata;
using Consyzer.Core.Models.Resolution;

namespace Consyzer.Application.Analyzers;

internal sealed class LibraryResolutionAnalyzer(
    IOptions<CommandLineOptions> options
) : IAnalyzer<IEnumerable<PInvokeMethodGroup>, LibraryResolutionOutcome>
{
    private readonly MultiPlatformLibraryResolutionResolver _resolver = new(options.Value.AnalysisDirectory);

    public LibraryResolutionOutcome Analyze(IEnumerable<PInvokeMethodGroup> methodGroups)
    {
        var results = methodGroups
            .SelectMany(group => group.Methods
                .Select(method => method.ImportName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(libraryName => _resolver.Resolve(new LibraryResolutionContext(
                    group.File,
                    libraryName
                )))
            )
            .ToList();

        return new LibraryResolutionOutcome
        {
            Platform = _resolver.PlatformName,
            Results = results
        };
    }
}
