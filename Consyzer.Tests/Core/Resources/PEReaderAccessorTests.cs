using Xunit;
using Consyzer.Core.Caching;
using static Consyzer.Tests.TestInfrastructure.Constants;

namespace Consyzer.Tests.Core.Resources;

public sealed class MetadataOnlyPEReaderCacheTests
{
    [Fact]
    public void Get_ShouldReturnPEReader_WhenCalledFirstTime()
    {
        using var accessor = new MetadataOnlyPEReaderCache();

        var reader = accessor.GetOrAdd(EcmaAssemblyWithPInvoke);

        Assert.NotNull(reader);
        Assert.True(reader.HasMetadata);
    }

    [Fact]
    public void Get_ShouldReturnCachedPEReader_WhenCalledMultipleTimesWithSameFile()
    {
        using var accessor = new MetadataOnlyPEReaderCache();

        var reader1 = accessor.GetOrAdd(EcmaAssemblyWithPInvoke);
        var reader2 = accessor.GetOrAdd(EcmaAssemblyWithPInvoke);

        Assert.Same(reader1, reader2);
    }

    [Fact]
    public void Dispose_ShouldReleasePEReaders()
    {
        var accessor = new MetadataOnlyPEReaderCache();

        var reader = accessor.GetOrAdd(EcmaAssemblyWithPInvoke);

        accessor.Dispose();

        Assert.Throws<ObjectDisposedException>(() => _ = reader.HasMetadata);
    }
}
