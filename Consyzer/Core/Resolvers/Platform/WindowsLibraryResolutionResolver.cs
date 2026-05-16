using Consyzer.Core.Models;

namespace Consyzer.Core.Resolvers.Platform;

internal sealed class WindowsLibraryResolutionResolver(
    string analyzedDirectory
) : PlatformLibraryResolutionResolverBase
{
    private const string LibraryExtension = ".dll";
    private const string ExecutableExtension = ".exe";
    private const string EnvironmentVariablePath = "PATH";

    private readonly string _analyzedDirectory = Path.GetFullPath(analyzedDirectory);

    // Windows has a lot of mechanisms we can't simulate.
    private const NotSimulatedMechanisms NotSimulated =
        NotSimulatedMechanisms.WindowsSxS
        | NotSimulatedMechanisms.WindowsKnownDlls
        | NotSimulatedMechanisms.WindowsDllRedirection
        | NotSimulatedMechanisms.WindowsProcessDirectoryOverrides
        | NotSimulatedMechanisms.WindowsApiSetSchema
        | NotSimulatedMechanisms.WindowsPackageGraph
        | NotSimulatedMechanisms.WindowsLoadedModuleList
        | NotSimulatedMechanisms.WindowsSafeSearchModeAndFlags
        | NotSimulatedMechanisms.WindowsAppPathsRegistry
        | NotSimulatedMechanisms.WindowsDotNetSearchPathOverrides;

    public override LibraryResolutionResult Resolve(string file)
    {
        var candidates = GetLibraryNameCandidates(file);
        var heuristicCandidates = Array.Empty<string>();

        if (TryResolveExplicit(file, candidates[0], heuristicCandidates, out var result)) return result;
        if (TryResolveApplicationDirectory(file, candidates, heuristicCandidates, out result)) return result;
        if (TryResolveDefaultSystemLocations(file, candidates, heuristicCandidates, out result)) return result;
        if (TryResolveCurrentDirectory(file, candidates, heuristicCandidates, out result)) return result;
        if (TryResolveEnvironmentPath(file, candidates, heuristicCandidates, out result)) return result;

        return CreateInconclusive(file, heuristicCandidates, NotSimulated);
    }

    private static IReadOnlyList<string> GetLibraryNameCandidates(string input)
    {
        if (IsExplicitPath(input))
        {
            return [input];
        }

        if (input.EndsWith(LibraryExtension, StringComparison.OrdinalIgnoreCase)
            || input.EndsWith(ExecutableExtension, StringComparison.OrdinalIgnoreCase))
        {
            return [input];
        }

        return DistinctCandidates([input, input + LibraryExtension], StringComparer.OrdinalIgnoreCase);
    }

    private static bool TryResolveExplicit(
        string requestedName,
        string normalized,
        IReadOnlyList<string> heuristicCandidates,
        out LibraryResolutionResult result
    )
    {
        if (!TryGetExplicitPathCandidate(normalized, out var candidate))
        {
            result = default!;
            return false;
        }

        result = candidate is not null
            ? CreateResolved(requestedName, candidate, MechanismKind.ExplicitPath, heuristicCandidates)
            : CreateMissing(requestedName, heuristicCandidates);

        return true;
    }

    private bool TryResolveApplicationDirectory(
        string requestedName,
        IReadOnlyList<string> candidates,
        IReadOnlyList<string> heuristicCandidates,
        out LibraryResolutionResult result
    )
    {
        var candidate = TryResolveInDirectories(candidates, [_analyzedDirectory]);
        if (candidate is null)
        {
            result = default!;
            return false;
        }

        result = CreateResolved(requestedName, candidate, MechanismKind.ApplicationDirectory, heuristicCandidates);
        return true;
    }

    private static bool TryResolveDefaultSystemLocations(
        string requestedName,
        IReadOnlyList<string> candidates,
        IReadOnlyList<string> heuristicCandidates,
        out LibraryResolutionResult result
    )
    {
        var sysDir = Environment.SystemDirectory;
        var winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

        var candidate = TryResolveInDirectories(candidates, [sysDir, winDir]);
        if (candidate is null)
        {
            result = default!;
            return false;
        }

        result = CreateResolved(requestedName, candidate, MechanismKind.DefaultSystemLocations, heuristicCandidates);
        return true;
    }

    private static bool TryResolveCurrentDirectory(
        string requestedName,
        IReadOnlyList<string> candidates,
        IReadOnlyList<string> heuristicCandidates,
        out LibraryResolutionResult result
    )
    {
        var candidate = TryResolveInDirectories(candidates, [Directory.GetCurrentDirectory()]);
        if (candidate is null)
        {
            result = default!;
            return false;
        }

        result = CreateResolved(requestedName, candidate, MechanismKind.CurrentDirectory, heuristicCandidates);
        return true;
    }

    private static bool TryResolveEnvironmentPath(
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
}
