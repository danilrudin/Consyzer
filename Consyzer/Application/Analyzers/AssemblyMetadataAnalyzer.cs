using Consyzer.Core.Extractors;
using Consyzer.Core.Models.Metadata;

namespace Consyzer.Application.Analyzers;

internal sealed class AssemblyMetadataAnalyzer(
    IExtractor<FileInfo, AssemblyMetadata> assemblyMetadataExtractor
) : IAnalyzer<IEnumerable<FileInfo>, IEnumerable<AssemblyMetadata>>
{
    public IEnumerable<AssemblyMetadata> Analyze(IEnumerable<FileInfo> files)
    {
        foreach (var file in files)
        {
            yield return assemblyMetadataExtractor.Extract(file);
        }
    }
}
