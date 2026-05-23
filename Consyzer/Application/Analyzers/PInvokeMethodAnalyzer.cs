using Consyzer.Core.Extractors;
using Consyzer.Core.Models.Metadata;

namespace Consyzer.Application.Analyzers;

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
