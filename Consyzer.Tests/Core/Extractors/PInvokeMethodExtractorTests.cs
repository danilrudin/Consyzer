using Consyzer.Core.Caching;
using Consyzer.Core.Extractors;
using static Consyzer.Tests.TestSupport.Samples.TestFiles;

namespace Consyzer.Tests.Core.Extractors;

public sealed class PInvokeMethodExtractorTests
{
    [Fact]
    public void Extract_ShouldReturnMethods_WhenPInvokeMethodsPresent()
    {
        using var peAccessor = new MetadataOnlyPEReaderCache();
        var extractor = new PInvokeMethodExtractor(peAccessor);

        var methods = extractor.Extract(AssemblyWithPInvoke);

        Assert.NotEmpty(methods);

        var method = methods.First();
        Assert.False(string.IsNullOrWhiteSpace(method.Signature.GetMethodLocation()));
        Assert.False(string.IsNullOrWhiteSpace(method.ImportName));
    }

    [Fact]
    public void Extract_ShouldReturnEmpty_WhenNoPInvokeMethods()
    {
        using var peAccessor = new MetadataOnlyPEReaderCache();
        var extractor = new PInvokeMethodExtractor(peAccessor);

        var result = extractor.Extract(AssemblyWithoutPInvoke);

        Assert.Empty(result);
    }
}
