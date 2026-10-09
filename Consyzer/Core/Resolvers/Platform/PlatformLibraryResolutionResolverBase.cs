using Consyzer.Core.Models.Resolution;

namespace Consyzer.Core.Resolvers.Platform;

internal abstract class PlatformLibraryResolutionResolverBase
{
    public abstract string PlatformName { get; }
    public abstract LibraryResolution Resolve(LibraryResolutionContext context);

    protected static IReadOnlyList<string> CollectHeuristicCandidates(
        LibraryResolutionContext context,
        IReadOnlyList<string> candidates,
        string analysisDirectory,
        StringComparer pathComparer
    )
    {
        var targetDirectory = context.TargetFile.DirectoryName;
        if (string.IsNullOrEmpty(targetDirectory))
        {
            return [];
        }

        var normalizedTargetDirectory = Path.GetFullPath(targetDirectory);
        if (pathComparer.Equals(
            normalizedTargetDirectory,
            analysisDirectory
        ))
        {
            return [];
        }

        return DistinctCandidates(
            EnumerateExistingCandidatePaths(candidates, [analysisDirectory]),
            pathComparer
        );
    }

    protected static LibraryResolution ResolveExplicitPath(
        LibraryResolutionContext context,
        IReadOnlyList<string> candidates,
        IReadOnlyList<string> heuristicCandidates,
        NotSimulatedMechanisms notSimulatedWhenMissing = NotSimulatedMechanisms.None
    )
    {
        foreach (var candidatePath in candidates)
        {
            var candidate = GetCandidatePath(null, candidatePath);
            if (candidate is not null)
            {
                return CreateResolved(
                    context,
                    candidate,
                    MechanismKind.ExplicitPath,
                    heuristicCandidates
                );
            }
        }

        return notSimulatedWhenMissing == NotSimulatedMechanisms.None
            ? CreateMissing(context, heuristicCandidates)
            : CreateInconclusive(context, heuristicCandidates, notSimulatedWhenMissing);
    }

    protected static bool TryResolveAssemblyDirectory(
        LibraryResolutionContext context,
        string libraryNameCandidate,
        IReadOnlyList<string> heuristicCandidates,
        out LibraryResolution result
    )
        => TryResolveInDirectories(
            context,
            libraryNameCandidate,
            [context.TargetFile.DirectoryName],
            MechanismKind.AssemblyDirectory,
            heuristicCandidates,
            out result
        );

    protected static bool TryResolveInDirectories(
        LibraryResolutionContext context,
        string fileName,
        IEnumerable<string?> directories,
        MechanismKind mechanismKind,
        IReadOnlyList<string> heuristicCandidates,
        out LibraryResolution result,
        NotSimulatedMechanisms notSimulated = NotSimulatedMechanisms.None
    )
    {
        foreach (var directory in directories)
        {
            if (string.IsNullOrEmpty(directory)) continue;

            var candidate = GetCandidatePath(directory, fileName);
            if (candidate is not null)
            {
                result = CreateResolved(
                    context,
                    candidate,
                    mechanismKind,
                    heuristicCandidates,
                    notSimulated
                );
                return true;
            }
        }

        result = default!;
        return false;
    }

    protected static IEnumerable<string> SplitSearchPath(
        string? path,
        bool emptySegmentMeansCurrentDirectory = false,
        bool trimQuotes = true,
        params char[] separators
    )
    {
        if (string.IsNullOrEmpty(path)) yield break;

        char[] effectiveSeparators = separators.Length == 0
            ? [Path.PathSeparator]
            : separators;

        foreach (var rawPart in path.Split(effectiveSeparators, StringSplitOptions.None))
        {
            var part = trimQuotes ? rawPart.Trim('"') : rawPart;

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

    protected static LibraryResolution CreateResolved(
        LibraryResolutionContext context,
        string path,
        MechanismKind kind,
        IReadOnlyList<string> heuristicCandidates,
        NotSimulatedMechanisms notSimulated = NotSimulatedMechanisms.None
    )
        => new()
        {
            TargetPath = context.TargetFile.FullName,
            LibraryName = context.LibraryName,
            ResolutionState = ResolutionState.Resolved,
            ResolvedPresence = new ResolvedPresence(path, kind),
            HeuristicCandidates = heuristicCandidates,
            NotSimulated = notSimulated
        };

    protected static LibraryResolution CreateMissing(
        LibraryResolutionContext context,
        IReadOnlyList<string> heuristicCandidates
    )
        => new()
        {
            TargetPath = context.TargetFile.FullName,
            LibraryName = context.LibraryName,
            ResolutionState = ResolutionState.Missing,
            ResolvedPresence = null,
            HeuristicCandidates = heuristicCandidates,
            NotSimulated = NotSimulatedMechanisms.None
        };

    protected static LibraryResolution CreateInconclusive(
        LibraryResolutionContext context,
        IReadOnlyList<string> heuristicCandidates,
        NotSimulatedMechanisms notSimulated
    )
        => new()
        {
            TargetPath = context.TargetFile.FullName,
            LibraryName = context.LibraryName,
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
            if (string.IsNullOrEmpty(dir)) continue;

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
        var candidate = string.IsNullOrEmpty(baseDirectory)
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
    ) => [.. candidates.Distinct(comparer)];
}
