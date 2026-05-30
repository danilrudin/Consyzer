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

        var reader1 = cache.GetOrAdd(AssemblyWithPInvoke);
        var reader2 = cache.GetOrAdd(AssemblyWithPInvoke);

        Assert.Same(reader1, reader2);
    }

    [Fact]
    public void GetOrAdd_ShouldReturnCachedPEReader_WhenCalledWithDifferentFileInfoForSamePath()
    {
        using var cache = new MetadataOnlyPEReaderCache();

        var sameFile = new FileInfo(AssemblyWithPInvoke.FullName);

        var reader1 = cache.GetOrAdd(AssemblyWithPInvoke);
        var reader2 = cache.GetOrAdd(sameFile);

        Assert.Same(reader1, reader2);
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
