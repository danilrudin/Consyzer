using Consyzer.Core.Caching;
using Consyzer.Core.Classifiers;
using static Consyzer.Tests.TestSupport.Samples.TestFiles;
using static Consyzer.Tests.TestSupport.Helpers.FileAssertionHelper;

namespace Consyzer.Tests.Core.Classifiers;

public sealed class AnalysisFileClassifierTests
{
    [Fact]
    public void Resolve_ShouldClassifyFilesCorrectly_WhenGivenMixedInput()
    {
        using var peAccessor = new MetadataOnlyPEReaderCache();
        var resolver = new EcmaFileClassifier(peAccessor);

        var files = new[]
        {
            AssemblyWithPInvoke,
            NonEcmaModule
        };
        
        var result = resolver.Classify(files);

        Assert.Single(result.EcmaAssemblies);
        Assert.Single(result.NonEcmaModules);

        Assert.Empty(result.NonEcmaAssemblies);

        ContainsPath(AssemblyWithPInvoke, result.EcmaAssemblies);
        ContainsPath(NonEcmaModule, result.NonEcmaModules);
    }
}
