using Xunit;
using Consyzer.Core.Caching;
using Consyzer.Core.Classifiers;
using static Consyzer.Tests.TestInfrastructure.Constants;
using static Consyzer.Tests.TestInfrastructure.Helpers.MatchesHelper;

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
            EcmaAssemblyWithPInvoke,
            NonEcmaAssembly
        };
        
        var result = resolver.Classify(files);

        Assert.Single(result.EcmaAssemblies);
        Assert.Single(result.NonEcmaModules);

        Assert.Empty(result.NonEcmaAssemblies);

        Assert.True(Matches(EcmaAssemblyWithPInvoke, result.EcmaAssemblies));
        Assert.True(Matches(NonEcmaAssembly, result.NonEcmaModules));
    }
}
