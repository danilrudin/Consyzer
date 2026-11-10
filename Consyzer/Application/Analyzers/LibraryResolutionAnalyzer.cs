using Consyzer.Core.Resolvers;
using Consyzer.Core.Models.Metadata;
using Consyzer.Core.Models.Resolution;

namespace Consyzer.Application.Analyzers;

internal sealed class LibraryResolutionAnalyzer(
    ILibraryResolutionResolver resolver
) : IAnalyzer<IEnumerable<PInvokeMethodGroup>, LibraryResolutionOutcome>
{
    private static readonly StringComparer ImportNameComparer =
        OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

    public LibraryResolutionOutcome Analyze(IEnumerable<PInvokeMethodGroup> methodGroups)
    {
        var results = methodGroups
            .SelectMany(group => group.Methods
                .GroupBy(
                    method => method.ImportName,
                    ImportNameComparer
                )
                .Select(methods => resolver.Resolve(new LibraryResolutionContext(
                    group.File,
                    methods.Key,
                    methods.Any(method => method.HasDllImportSearchPathOverride)
                )))
            )
            .ToList();

        return new LibraryResolutionOutcome
        {
            Platform = resolver.PlatformName,
            Results = results
        };
    }
}
