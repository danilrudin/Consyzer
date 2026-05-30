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

    public static void ContainsPath(FileInfo expected, IEnumerable<FileInfo> actual)
    {
        Assert.Contains(
            actual,
            file => PlatformStringComparisonHelper.FilePathComparer.Equals(expected.FullName, file.FullName)
        );
    }

    public static void EqualCreationTimeUtc(FileInfo expected, DateTime actual)
    {
        Assert.Equal(expected.CreationTimeUtc, actual);
    }
}
