using Consyzer.Helpers;

namespace Consyzer.Tests.TestSupport.Helpers;

internal static class FileAssertionHelper
{
    public static void EqualPath(FileInfo expected, FileInfo actual)
    {
        Assert.True(
            PlatformStringComparisonHelper.FilePathComparer.Equals(expected.FullName, actual.FullName),
            $"Expected file path '{expected.FullName}', but got '{actual.FullName}'."
        );
    }
}
