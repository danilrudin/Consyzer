using System.Reflection.Metadata;
using Consyzer.Core.Caching;
using Consyzer.Core.Extractors;
using Consyzer.Tests.TestSupport.Samples;
using static Consyzer.Tests.TestSupport.Samples.TestFiles;

namespace Consyzer.Tests.Core.Extractors;

public sealed class MethodSignatureExtractorTests
{
    [Fact]
    public void Extract_ShouldReturnSignature_WhenValidMethodProvided()
    {
        using var peAccessor = new MetadataOnlyPEReaderCache();
        var mdReader = peAccessor.GetOrAdd(AssemblyWithPInvoke).GetMetadataReader();

        var methodDef = mdReader.MethodDefinitions
            .Select(mdReader.GetMethodDefinition)
            .Single(method => mdReader.GetString(method.Name) == nameof(PInvokeTestMethods.Invoke)
                && mdReader.GetString(mdReader.GetTypeDefinition(method.GetDeclaringType()).Name)
                    == nameof(PInvokeTestMethods));

        var extractor = new MethodSignatureExtractor(mdReader);
        var result = extractor.Extract(methodDef);

        Assert.Equal(typeof(PInvokeTestMethods).Namespace, result.Namespace);
        Assert.Equal(nameof(PInvokeTestMethods), result.Class);
        Assert.Equal(nameof(PInvokeTestMethods.Invoke), result.Method);
        Assert.Equal("Int32", result.ReturnType);
        Assert.True(result.IsStatic);
        Assert.Equal("Int32", Assert.Single(result.MethodArguments));
    }
}
