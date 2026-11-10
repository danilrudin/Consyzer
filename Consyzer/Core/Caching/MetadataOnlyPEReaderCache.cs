using System.Reflection.PortableExecutable;
using static Consyzer.Helpers.PlatformStringComparisonHelper;

namespace Consyzer.Core.Caching;

internal sealed class MetadataOnlyPEReaderCache : IResourceCache<FileInfo, PEReader>
{
    private readonly Dictionary<string, PEReader> _cache = new(FilePathComparer);

    public PEReader GetOrAdd(FileInfo file)
    {
        string path = file.FullName;

        if (_cache.TryGetValue(path, out var reader))
        {
            return reader;
        }

        var fStream = File.OpenRead(path);
        reader = new PEReader(fStream, PEStreamOptions.PrefetchMetadata);

        _cache.Add(path, reader);

        return reader;
    }

    public void Dispose()
    {
        foreach (var reader in _cache.Values)
        {
            reader.Dispose();
        }

        _cache.Clear();
    }
}
