using System.Reflection;
using System.Reflection.Metadata;
using Consyzer.Core.Caching;
using Consyzer.Core.Extractors;
using static Consyzer.Tests.TestInfrastructure.Constants;

namespace Consyzer.Tests.Core.Extractors;

public sealed class MetadataSignatureExtractorTest
{
    [Fact]
    public void Extract_ShouldReturnSignature_WhenValidMethodProvided()
    {
        using var peAccessor = new MetadataOnlyPEReaderCache();
        var mdReader = peAccessor.GetOrAdd(EcmaAssemblyWithPInvoke).GetMetadataReader();

        var methodDef = mdReader.MethodDefinitions
            .Select(mdReader.GetMethodDefinition)
            .First(m => m.Attributes.HasFlag(MethodAttributes.PinvokeImpl));

        var extractor = new MethodSignatureExtractor(mdReader);
        var result = extractor.Extract(methodDef);

        // Namespace can be an empty string for global-scope types, but must not be null
        Assert.NotNull(result.Namespace);
        Assert.False(string.IsNullOrWhiteSpace(result.Class));
        Assert.False(string.IsNullOrWhiteSpace(result.Method));
        Assert.False(string.IsNullOrWhiteSpace(result.ReturnType));
        Assert.NotNull(result.MethodArguments);
    }
}
