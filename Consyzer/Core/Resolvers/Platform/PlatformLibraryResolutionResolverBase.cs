using Consyzer.Core.Models;

namespace Consyzer.Core.Resolvers.Platform;

internal abstract class PlatformLibraryResolutionResolverBase
{
    public abstract LibraryResolutionResult Resolve(string file);

    protected static string? TryResolveInDirectories(string fileName, IEnumerable<string?> directories)
    {
        foreach (var dir in directories)
        {
            if (string.IsNullOrWhiteSpace(dir)) continue;

            var candidate = GetCandidatePath(dir, fileName);
            if (candidate is not null)
            {
                return candidate;
            }
        }

        return null;
    }

    protected static string? GetCandidatePath(string? baseDirectory, string file)
    {
        var candidate = string.IsNullOrWhiteSpace(baseDirectory)
            ? file
            : Path.Combine(baseDirectory, file);

        return File.Exists(candidate) ? Path.GetFullPath(candidate) : null;
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

    protected static LibraryResolutionResult CreateResolved(
        string requestedName,
        string path,
        MechanismKind kind
    )
        => new()
        {
            LibraryName = requestedName,
            State = ResolutionState.Resolved,
            Resolved = new ResolvedPresence(path, kind),
            HeuristicCandidates = [],
            NotSimulated = NotSimulatedMechanisms.None
        };

    protected static LibraryResolutionResult CreateMissing(string requestedName)
        => new()
        {
            LibraryName = requestedName,
            State = ResolutionState.Missing,
            Resolved = null,
            HeuristicCandidates = [],
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

    protected static bool IsExplicitPath(string path) 
        => Path.IsPathRooted(path) 
        || path.Contains(Path.DirectorySeparatorChar)
        || path.Contains(Path.AltDirectorySeparatorChar);
}
