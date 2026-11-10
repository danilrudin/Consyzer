using Consyzer.Core.Caching;
using static Consyzer.Tests.TestSupport.Samples.TestFiles;

namespace Consyzer.Tests.Core.Caching;

public sealed class MetadataOnlyPEReaderCacheTests
{
    [Fact]
    public void GetOrAdd_ShouldReturnPEReader_WhenCalledFirstTime()
    {
        using var cache = new MetadataOnlyPEReaderCache();

        var reader = cache.GetOrAdd(AssemblyWithPInvoke);

        Assert.NotNull(reader);
        Assert.True(reader.HasMetadata);
    }

    [Fact]
    public void GetOrAdd_ShouldReturnCachedPEReader_WhenCalledMultipleTimesWithSameFile()
    {
        using var cache = new MetadataOnlyPEReaderCache();
        var file = AssemblyWithPInvoke;

        var reader1 = cache.GetOrAdd(file);
        var reader2 = cache.GetOrAdd(file);
        var reader3 = cache.GetOrAdd(new FileInfo(file.FullName));

        Assert.Same(reader1, reader2);
        Assert.Same(reader1, reader3);
    }

    [Fact]
    public void Dispose_ShouldDisposeCachedPEReaders()
    {
        var cache = new MetadataOnlyPEReaderCache();

        var reader = cache.GetOrAdd(AssemblyWithPInvoke);

        cache.Dispose();

        Assert.Throws<ObjectDisposedException>(() => _ = reader.HasMetadata);
    }
}
