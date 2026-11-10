using Consyzer.Input;
using Consyzer.Tests.TestSupport.FileSystem;

namespace Consyzer.Tests.Input;

public sealed class AnalysisFileFinderTests
{
    [Fact]
    public void FindBySeparatedPatterns_ShouldReturnEachFileOnce_WhenPatternsOverlap()
    {
        using var directory = new TemporaryDirectory("consyzer-finder-");
        var file = directory.CreateFile("Target.dll");

        var result = AnalysisFileFinder
            .FindBySeparatedPatterns(
                directory.Path,
                "*.dll, Target.*",
                ',',
                isRecursive: false
            )
            .ToList();

        var foundFile = Assert.Single(result);
        Assert.Equal(file.FullName, foundFile.FullName);
    }
}
