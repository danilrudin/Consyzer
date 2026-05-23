using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Consyzer.Core.Caching;
using Consyzer.Core.Cryptography;
using Consyzer.Core.Models.Metadata;

namespace Consyzer.Core.Extractors;

internal sealed class AssemblyMetadataExtractor(
    IFileHasher hasher,
    IResourceCache<FileInfo, PEReader> peReaderCache
) : IExtractor<FileInfo, AssemblyMetadata>
{
    public AssemblyMetadata Extract(FileInfo file)
    {
        return new AssemblyMetadata
        {
            File = file,
            Version = GetVersion(file),
            CreationDateUtc = GetCreationDate(file),
            Sha256 = GetHash(file)
        };
    }

    private string GetVersion(FileInfo file)
    {
        var peReader = peReaderCache.GetOrAdd(file);
        var mdReader = peReader.GetMetadataReader();

        if (!mdReader.IsAssembly)
        {
            return "unknown";
        }

        return mdReader
            .GetAssemblyDefinition()
            .Version
            .ToString();
    }

    private string GetHash(FileInfo file)
    {
        return hasher.CalculateHash(file);
    }

    private static DateTime GetCreationDate(FileInfo file)
    {
        return file.CreationTimeUtc;
    }
}
