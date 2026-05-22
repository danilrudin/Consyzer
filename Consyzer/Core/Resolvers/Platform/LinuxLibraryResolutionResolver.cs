using Consyzer.Helpers;
using Consyzer.Core.Models;

namespace Consyzer.Core.Resolvers.Platform;

internal sealed class LinuxLibraryResolutionResolver(
    string analyzedDirectory
) : PlatformLibraryResolutionResolverBase
{
    private const string PlatformName = "Linux";
    private const string LibraryExtension = ".so";
    private const string EnvironmentVariablePath = "LD_LIBRARY_PATH";

    private readonly string _analyzedDirectory = Path.GetFullPath(analyzedDirectory);

    // Linux has a lot of mechanisms we can't simulate.
    private const NotSimulatedMechanisms NotSimulated =
        NotSimulatedMechanisms.LinuxRPathRunPath
        | NotSimulatedMechanisms.LinuxLdSoCache
        | NotSimulatedMechanisms.LinuxLdSoConf
        | NotSimulatedMechanisms.LinuxSecureExecution
        | NotSimulatedMechanisms.LinuxTransitiveDependencies
        | NotSimulatedMechanisms.LinuxLdPreload;

    private static readonly string[] DefaultSystemLocations =
    [
        "/lib",
        "/usr/lib",
        "/lib64",
        "/usr/lib64",
        "/lib/x86_64-linux-gnu",
        "/usr/lib/x86_64-linux-gnu",
        "/lib/aarch64-linux-gnu",
        "/usr/lib/aarch64-linux-gnu",
        "/lib/arm-linux-gnueabihf",
        "/usr/lib/arm-linux-gnueabihf"
    ];

    public override LibraryResolutionResult Resolve(LibraryResolutionContext context)
    {
        var candidates = GetLibraryNameCandidates(context.LibraryName);
        var heuristicCandidates = IsExplicitPath(candidates[0])
            ? []
            : CollectHeuristicCandidates(candidates);

        var ldLibraryPath = Environment.GetEnvironmentVariable(EnvironmentVariablePath);
        var notSimulatedCaveats = ContainsDynamicStringToken(ldLibraryPath)
            ? NotSimulatedMechanisms.LinuxLdLibraryPathDynamicStringTokens
            : NotSimulatedMechanisms.None;

        if (TryResolveExplicit(context, candidates[0], heuristicCandidates, out var result)) return result;
        if (TryResolveLdLibraryPath(context, candidates, heuristicCandidates, ldLibraryPath, notSimulatedCaveats, out result)) return result;
        if (TryResolveDefaultSystemLocations(context, candidates, heuristicCandidates, notSimulatedCaveats, out result)) return result;

        return CreateInconclusive(context, PlatformName, heuristicCandidates, NotSimulated | notSimulatedCaveats);
    }

    private static IReadOnlyList<string> GetLibraryNameCandidates(string input)
    {
        if (IsExplicitPath(input))
        {
            return [input];
        }

        if (EndsWithSharedObjectName(input))
        {
            return DistinctCandidates([input, WithLibPrefix(input)], StringComparer.Ordinal);
        }

        var canonical = input + LibraryExtension;

        return DistinctCandidates(
        [
            canonical,
            WithLibPrefix(canonical),
            input,
            WithLibPrefix(input)
        ], StringComparer.Ordinal);
    }

    private List<string> CollectHeuristicCandidates(IReadOnlyList<string> candidates)
    {
        return CollectCandidatePaths(candidates, _analyzedDirectory, Directory.GetCurrentDirectory());
    }

    private static bool TryResolveExplicit(
        LibraryResolutionContext context,
        string normalizedExplicitPath,
        IReadOnlyList<string> heuristicCandidates,
        out LibraryResolutionResult result
    )
    {
        if (!TryGetExplicitPathCandidate(normalizedExplicitPath, out var candidate))
        {
            result = default!;
            return false;
        }

        result = candidate is not null
            ? CreateResolved(context, PlatformName, candidate, MechanismKind.ExplicitPath, heuristicCandidates)
            : CreateMissing(context, PlatformName, heuristicCandidates);

        return true;
    }

    private static bool TryResolveLdLibraryPath(
        LibraryResolutionContext context,
        IReadOnlyList<string> candidates,
        IReadOnlyList<string> heuristicCandidates,
        string? ldLibraryPath,
        NotSimulatedMechanisms notSimulatedCaveats,
        out LibraryResolutionResult result
    )
    {
        var candidate = TryResolveInDirectories(
            candidates,
            SplitSearchPath(ldLibraryPath, emptySegmentMeansCurrentDirectory: true)
        );

        if (candidate is null)
        {
            result = default!;
            return false;
        }

        result = CreateResolved(
            context,
            PlatformName,
            candidate,
            MechanismKind.EnvironmentOverride,
            heuristicCandidates,
            notSimulatedCaveats);
        return true;
    }

    private static bool TryResolveDefaultSystemLocations(
        LibraryResolutionContext context,
        IReadOnlyList<string> candidates,
        IReadOnlyList<string> heuristicCandidates,
        NotSimulatedMechanisms notSimulatedCaveats,
        out LibraryResolutionResult result
    )
    {
        var candidate = TryResolveInDirectories(candidates, DefaultSystemLocations);
        if (candidate is null)
        {
            result = default!;
            return false;
        }

        result = CreateResolved(
            context,
            PlatformName,
            candidate,
            MechanismKind.DefaultSystemLocations,
            heuristicCandidates,
            notSimulatedCaveats);
        return true;
    }

    private static bool EndsWithSharedObjectName(string input)
        => input.EndsWith(LibraryExtension, StringComparison.Ordinal)
        || input.Contains(LibraryExtension + ".", StringComparison.Ordinal);

    private static string WithLibPrefix(string input)
        => input.StartsWith("lib", StringComparison.Ordinal) ? input : "lib" + input;

    private static bool ContainsDynamicStringToken(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;

        return path.Contains("$ORIGIN", StringComparison.Ordinal)
            || path.Contains("$LIB", StringComparison.Ordinal)
            || path.Contains("$PLATFORM", StringComparison.Ordinal);
    }

    private static List<string> CollectCandidatePaths(
        IReadOnlyList<string> fileNames,
        params string?[] directories
    )
    {
        var seen = new HashSet<string>(PlatformStringComparisonHelper.FilePathComparer);

        return [.. EnumerateExistingCandidatePaths(fileNames, directories).Where(seen.Add)];
    }
}
