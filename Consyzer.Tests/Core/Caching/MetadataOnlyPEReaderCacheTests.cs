using Consyzer.Core.Caching;
using static Consyzer.Tests.TestInfrastructure.Constants;

namespace Consyzer.Tests.Core.Caching;

public sealed class MetadataOnlyPEReaderCacheTests
{
    [Fact]
    public void GetOrAdd_ShouldReturnPEReader_WhenCalledFirstTime()
    {
        using var cache = new MetadataOnlyPEReaderCache();

        var reader = cache.GetOrAdd(EcmaAssemblyWithPInvoke);

        Assert.NotNull(reader);
        Assert.True(reader.HasMetadata);
    }

    [Fact]
    public void GetOrAdd_ShouldReturnCachedPEReader_WhenCalledMultipleTimesWithSameFile()
    {
        using var cache = new MetadataOnlyPEReaderCache();

        var reader1 = cache.GetOrAdd(EcmaAssemblyWithPInvoke);
        var reader2 = cache.GetOrAdd(EcmaAssemblyWithPInvoke);

        Assert.Same(reader1, reader2);
    }

    [Fact]
    public void GetOrAdd_ShouldReturnCachedPEReader_WhenCalledWithDifferentFileInfoForSamePath()
    {
        using var cache = new MetadataOnlyPEReaderCache();

        var sameFile = new FileInfo(EcmaAssemblyWithPInvoke.FullName);

        var reader1 = cache.GetOrAdd(EcmaAssemblyWithPInvoke);
        var reader2 = cache.GetOrAdd(sameFile);

        Assert.Same(reader1, reader2);
    }

    [Fact]
    public void Dispose_ShouldDisposeCachedPEReaders()
    {
        var cache = new MetadataOnlyPEReaderCache();

        var reader = cache.GetOrAdd(EcmaAssemblyWithPInvoke);

        cache.Dispose();

        Assert.Throws<ObjectDisposedException>(() => _ = reader.HasMetadata);
    }
}
