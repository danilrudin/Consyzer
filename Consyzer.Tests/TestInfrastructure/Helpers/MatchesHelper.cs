namespace Consyzer.Tests.TestInfrastructure.Helpers;

internal static class MatchesHelper
{
    public static bool Matches(FileInfo a, FileInfo b)
    {
        return string.Equals(a.FullName, b.FullName, StringComparison.OrdinalIgnoreCase);
    }

    public static bool Matches(FileInfo a, IEnumerable<FileInfo> b)
    {
        return b.Any(f => string.Equals(f.FullName, a.FullName, StringComparison.OrdinalIgnoreCase));
    }

    public static bool Matches(FileInfo a, DateTime b)
    {
        return a.CreationTimeUtc == b;
    }
}
