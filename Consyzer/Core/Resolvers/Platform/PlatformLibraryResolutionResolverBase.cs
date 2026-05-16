using Consyzer.Core.Models;

namespace Consyzer.Core.Resolvers.Platform;

internal abstract class PlatformLibraryResolutionResolverBase
{
    public abstract LibraryResolutionResult Resolve(string file);

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

    protected static IEnumerable<string> SplitSearchPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) yield break;

        var parts = path.Split(
            Path.PathSeparator,
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        );

        foreach (var part in parts)
        {
            var p = part.Trim('"');
            if (p.Length == 0) continue;

            yield return p;
        }
    }

    protected static LibraryResolutionResult CreateResolved(
        string requestedName,
        string path,
        MechanismKind kind,
        IReadOnlyList<string>? heuristicCandidates = null
    )
        => new()
        {
            LibraryName = requestedName,
            State = ResolutionState.Resolved,
            Resolved = new ResolvedPresence(path, kind),
            HeuristicCandidates = heuristicCandidates ?? [],
            NotSimulated = NotSimulatedMechanisms.None
        };

    protected static LibraryResolutionResult CreateMissing(
        string requestedName,
        IReadOnlyList<string>? heuristicCandidates = null
    )
        => new()
        {
            LibraryName = requestedName,
            State = ResolutionState.Missing,
            Resolved = null,
            HeuristicCandidates = heuristicCandidates ?? [],
            NotSimulated = NotSimulatedMechanisms.None
        };

    protected static LibraryResolutionResult CreateInconclusive(
        string requestedName,
        IReadOnlyList<string> heuristicCandidates,
        NotSimulatedMechanisms notSimulated
    )
        => new()
        {
            LibraryName = requestedName,
            State = ResolutionState.Inconclusive,
            Resolved = null,
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
        var result = new List<string>();
        var seen = new HashSet<string>(comparer);

        foreach (var candidate in candidates)
        {
            if (seen.Add(candidate))
            {
                result.Add(candidate);
            }
        }

        return result;
    }
}
