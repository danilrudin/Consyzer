namespace Consyzer.Helpers;

internal static class FileSearchHelper
{
    public static IEnumerable<FileInfo> GetFilesBySeparatedPatterns(
        string directory,
        string searchPatterns,
        char separator,
        bool isRecursive
    )
    {
        return searchPatterns
            .Split(
                separator,
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            )
            .SelectMany(p => GetFilesByPattern(directory, p, isRecursive));
    }

    public static IEnumerable<FileInfo> GetFilesByPattern(
        string directory,
        string searchPattern,
        bool isRecursive
    )
    {
        var searchOption = isRecursive
            ? SearchOption.AllDirectories
            : SearchOption.TopDirectoryOnly;

        return Directory
            .EnumerateFiles(directory, searchPattern.Trim(), searchOption)
            .Select(f => new FileInfo(f));
    }
}
