using Consyzer.Core.Models;
using Consyzer.Core.Extractors;

namespace Consyzer.Analyzers;

internal sealed class PInvokeMethodAnalyzer(
    IExtractor<FileInfo, IEnumerable<PInvokeMethod>> pInvokeMethodExtractor
) : IAnalyzer<IEnumerable<FileInfo>, IReadOnlyList<PInvokeMethodGroup>>
{
    public IReadOnlyList<PInvokeMethodGroup> Analyze(IEnumerable<FileInfo> files)
    {
        var groups = new List<PInvokeMethodGroup>();

        foreach (var file in files)
        {
            var methods = pInvokeMethodExtractor
                .Extract(file)
                .ToList();

            if (methods.Count == 0)
            {
                continue;
            }

            groups.Add(new PInvokeMethodGroup
            {
                File = file,
                Methods = methods
            });
        }

        return groups;
    }
}