using Microsoft.Extensions.Options;
using Consyzer.Options;
using Consyzer.Core.Resolvers;
using Consyzer.Core.Models.Resolution;
using Consyzer.Core.Models.Metadata;

namespace Consyzer.Application.Analyzers;

internal sealed class LibraryResolutionAnalyzer(
    IOptions<CommandLineOptions> options
) : IAnalyzer<IEnumerable<PInvokeMethodGroup>, IReadOnlyList<LibraryResolutionResult>>
{
    private readonly MultiPlatformLibraryResolutionResolver _resolver = new(options.Value.AnalysisDirectory);

    public IReadOnlyList<LibraryResolutionResult> Analyze(IEnumerable<PInvokeMethodGroup> methodGroups)
    {
        return [.. methodGroups
            .SelectMany(group => group.Methods
                .Select(method => method.ImportName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(libraryName => _resolver.Resolve(new LibraryResolutionContext(
                    group.File,
                    libraryName
                )))
            )
        ];
    }
}
