using Consyzer.Core.Caching;
using Consyzer.Core.Classifiers;
using static Consyzer.Tests.TestSupport.Samples.TestFiles;
using static Consyzer.Tests.TestSupport.Helpers.FileAssertionHelper;

namespace Consyzer.Tests.Core.Classifiers;

public sealed class AnalysisFileClassifierTests
{
    [Fact]
    public void Classify_ShouldClassifyFilesCorrectly_WhenGivenMixedInput()
    {
        using var peAccessor = new MetadataOnlyPEReaderCache();
        var classifier = new EcmaFileClassifier(peAccessor);

        var files = new[]
        {
            AssemblyWithPInvoke,
            NonEcmaModule
        };
        
        var result = classifier.Classify(files);

        EqualPath(AssemblyWithPInvoke, Assert.Single(result.EcmaAssemblies));
        EqualPath(NonEcmaModule, Assert.Single(result.NonEcmaModules));
        Assert.Empty(result.NonEcmaAssemblies);
    }
}
