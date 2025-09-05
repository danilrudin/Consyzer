using Microsoft.Extensions.Options;
using Consyzer.Options;
using Consyzer.Core.Models;
using Consyzer.Core.Resolvers;

namespace Consyzer.Analyzers;

internal sealed class LibraryPresenceAnalyzer(
    IOptions<AnalysisOptions> options
) : IAnalyzer<IEnumerable<PInvokeMethodGroup>, IEnumerable<LibraryPresence>>
{
    private readonly PlatformLibraryPresenceResolver _libraryPresenceResolver = new(options.Value.AnalysisDirectory);

    public IEnumerable<LibraryPresence> Analyze(IEnumerable<PInvokeMethodGroup> methodGroups)
    {
        var distinctImportNames = methodGroups
            .SelectMany(g => g.Methods.Select(m => m.ImportName))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var importName in distinctImportNames)
        {
            yield return _libraryPresenceResolver.Resolve(importName);
        }
    }
}
