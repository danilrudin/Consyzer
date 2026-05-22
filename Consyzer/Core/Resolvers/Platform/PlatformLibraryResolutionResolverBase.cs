using Consyzer.Core.Models;

namespace Consyzer.Core.Resolvers.Platform;

internal abstract class PlatformLibraryResolutionResolverBase
{
    public abstract LibraryResolutionResult Resolve(LibraryResolutionContext context);

    protected static bool TryGetExplicitPathCandidate(string path, out string? candidate)
    {
        if (!IsExplicitPath(path))
        {
            candidate = null;
            return false;
        }

        candidate = GetCandidatePath(null, path);
        return true;
    }

    protected static string? TryResolveInDirectories(
        IReadOnlyList<string> fileNames,
        IEnumerable<string?> directories
    )
    {
        return EnumerateExistingCandidatePaths(fileNames, directories).FirstOrDefault();
    }

    protected static IEnumerable<string> SplitSearchPath(
        string? path,
        bool emptySegmentMeansCurrentDirectory = false
    )
    {
        if (string.IsNullOrWhiteSpace(path)) yield break;

        foreach (var rawPart in path.Split(Path.PathSeparator, StringSplitOptions.None))
        {
            var part = rawPart.Trim('"');

            if (part.Length == 0)
            {
                if (emptySegmentMeansCurrentDirectory)
                {
                    yield return Directory.GetCurrentDirectory();
                }

                continue;
            }

            yield return part;
        }
    }

    protected static LibraryResolutionResult CreateResolved(
        LibraryResolutionContext context,
        string platform,
        string path,
        MechanismKind kind,
        IReadOnlyList<string>? heuristicCandidates = null,
        NotSimulatedMechanisms notSimulated = NotSimulatedMechanisms.None
    )
        => new()
        {
            TargetPath = context.TargetFile.FullName,
            LibraryName = context.LibraryName,
            Platform = platform,
            ResolutionState = ResolutionState.Resolved,
            ResolvedPresence = new ResolvedPresence(path, kind),
            HeuristicCandidates = heuristicCandidates ?? [],
            NotSimulated = notSimulated
        };

    protected static LibraryResolutionResult CreateMissing(
        LibraryResolutionContext context,
        string platform,
        IReadOnlyList<string>? heuristicCandidates = null
    )
        => new()
        {
            TargetPath = context.TargetFile.FullName,
            LibraryName = context.LibraryName,
            Platform = platform,
            ResolutionState = ResolutionState.Missing,
            ResolvedPresence = null,
            HeuristicCandidates = heuristicCandidates ?? [],
            NotSimulated = NotSimulatedMechanisms.None
        };

    protected static LibraryResolutionResult CreateInconclusive(
        LibraryResolutionContext context,
        string platform,
        NotSimulatedMechanisms notSimulated
    )
        => CreateInconclusive(context, platform, [], notSimulated);

    protected static LibraryResolutionResult CreateInconclusive(
        LibraryResolutionContext context,
        string platform,
        IReadOnlyList<string> heuristicCandidates,
        NotSimulatedMechanisms notSimulated
    )
        => new()
        {
            TargetPath = context.TargetFile.FullName,
            LibraryName = context.LibraryName,
            Platform = platform,
            ResolutionState = ResolutionState.Inconclusive,
            ResolvedPresence = null,
            HeuristicCandidates = heuristicCandidates,
            NotSimulated = notSimulated
        };

    protected static IEnumerable<string> EnumerateExistingCandidatePaths(
        IEnumerable<string> fileNames,
        IEnumerable<string?> directories
    )
    {
        foreach (var dir in directories)
        {
            if (string.IsNullOrWhiteSpace(dir)) continue;

            foreach (var fileName in fileNames)
            {
                var candidate = GetCandidatePath(dir, fileName);
                if (candidate is not null)
                {
                    yield return candidate;
                }
            }
        }
    }

    protected static string? GetCandidatePath(string? baseDirectory, string file)
    {
        var candidate = string.IsNullOrWhiteSpace(baseDirectory)
            ? file
            : Path.Combine(baseDirectory, file);

        return File.Exists(candidate) ? Path.GetFullPath(candidate) : null;
    }

    protected static bool IsExplicitPath(string path)
        => Path.IsPathRooted(path)
        || path.Contains(Path.DirectorySeparatorChar)
        || path.Contains(Path.AltDirectorySeparatorChar);

    protected static IReadOnlyList<string> DistinctCandidates(
        IEnumerable<string> candidates,
        StringComparer comparer
    )
    {
        var seen = new HashSet<string>(comparer);

        return [.. candidates.Where(seen.Add)];
    }
}
