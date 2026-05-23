namespace Consyzer.Input;

internal static class AnalysisFileFinder
{
    public static IEnumerable<FileInfo> FindBySeparatedPatterns(
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
            .SelectMany(p => FindByPattern(directory, p, isRecursive));
    }

    public static IEnumerable<FileInfo> FindByPattern(
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
