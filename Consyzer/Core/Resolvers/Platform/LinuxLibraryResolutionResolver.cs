using Consyzer.Core.Models;

namespace Consyzer.Core.Resolvers.Platform;

internal sealed class LinuxLibraryResolutionResolver(
    string analyzedDirectory
) : PlatformLibraryResolutionResolverBase
{
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
        "/usr/local/lib",
        "/lib/x86_64-linux-gnu",
        "/usr/lib/x86_64-linux-gnu",
        "/lib/aarch64-linux-gnu",
        "/usr/lib/aarch64-linux-gnu",
        "/lib/arm-linux-gnueabihf",
        "/usr/lib/arm-linux-gnueabihf"
    ];

    public override LibraryResolutionResult Resolve(string file)
    {
        var candidates = GetLibraryNameCandidates(file);
        var heuristicCandidates = IsExplicitPath(candidates[0])
            ? []
            : CollectHeuristicCandidates(candidates);

        if (TryResolveExplicit(file, candidates[0], heuristicCandidates, out var result)) return result;
        if (TryResolveLdLibraryPath(file, candidates, heuristicCandidates, out result)) return result;
        if (TryResolveDefaultSystemLocations(file, candidates, heuristicCandidates, out result)) return result;

        return CreateInconclusive(file, heuristicCandidates, NotSimulated);
    }

    private static bool TryResolveExplicit(
        string requestedName,
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
            ? CreateResolved(requestedName, candidate, MechanismKind.ExplicitPath, heuristicCandidates)
            : CreateMissing(requestedName, heuristicCandidates);

        return true;
    }

    private static bool TryResolveLdLibraryPath(
        string requestedName,
        IReadOnlyList<string> candidates,
        IReadOnlyList<string> heuristicCandidates,
        out LibraryResolutionResult result
    )
    {
        var candidate = TryResolveInDirectories(
            candidates,
            SplitSearchPath(Environment.GetEnvironmentVariable(EnvironmentVariablePath))
        );

        if (candidate is null)
        {
            result = default!;
            return false;
        }

        result = CreateResolved(requestedName, candidate, MechanismKind.EnvironmentOverride, heuristicCandidates);
        return true;
    }

    private static bool TryResolveDefaultSystemLocations(
        string requestedName,
        IReadOnlyList<string> candidates,
        IReadOnlyList<string> heuristicCandidates,
        out LibraryResolutionResult result
    )
    {
        var candidate = TryResolveInDirectories(candidates, DefaultSystemLocations);
        if (candidate is null)
        {
            result = default!;
            return false;
        }

        result = CreateResolved(requestedName, candidate, MechanismKind.DefaultSystemLocations, heuristicCandidates);
        return true;
    }

    private static IReadOnlyList<string> GetLibraryNameCandidates(string input)
    {
        if (IsExplicitPath(input))
        {
            return [input];
        }

        if (EndsWithSharedObjectName(input))
        {
            var extensioned = input + LibraryExtension;
            return DistinctCandidates(
            [
                input,
                WithLibPrefix(input),
                extensioned,
                WithLibPrefix(extensioned)
            ], StringComparer.Ordinal);
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

    private static bool EndsWithSharedObjectName(string input)
        => input.EndsWith(LibraryExtension, StringComparison.Ordinal)
        || input.Contains(LibraryExtension, StringComparison.Ordinal);

    private static string WithLibPrefix(string input) => "lib" + input;

    private IReadOnlyList<string> CollectHeuristicCandidates(IReadOnlyList<string> candidates)
    {
        return CollectCandidatePaths(candidates, _analyzedDirectory, Directory.GetCurrentDirectory());
    }
}
