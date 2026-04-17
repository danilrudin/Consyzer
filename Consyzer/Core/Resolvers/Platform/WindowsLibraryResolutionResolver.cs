using Consyzer.Core.Models;

namespace Consyzer.Core.Resolvers.Platform;

internal sealed class WindowsLibraryResolutionResolver(
    string analyzedDirectory
) : PlatformLibraryResolutionResolverBase
{
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
        var normalized = NormalizeLibraryName(file);

        // TO DO: make state-machine

        if (TryResolveExplicit(file, normalized, out var result)) return result;
        if (TryResolveApplicationDirectory(file, normalized, out result)) return result;
        if (TryResolveDefaultSystemLocations(file, normalized, out result)) return result;
        if (TryResolveEnvironmentPath(file, normalized, out result)) return result;

        return CreateInconclusive(file, CollectHeuristicCandidates(normalized), NotSimulated);
    }

    private static bool TryResolveExplicit(
        string requestedName, 
        string normalized, 
        out LibraryResolutionResult result
    )
    {
        if (!TryGetExplicitPathCandidate(normalized, out var candidate))
        {
            result = default!;
            return false;
        }

        result = candidate is not null
            ? CreateResolved(requestedName, candidate, MechanismKind.ExplicitPath)
            : CreateMissing(requestedName);

        return true;
    }

    private bool TryResolveApplicationDirectory(
        string requestedName, 
        string normalized, 
        out LibraryResolutionResult result
    )
    {
        var candidate = GetCandidatePath(_analyzedDirectory, normalized);
        if (candidate is null)
        {
            result = default!;
            return false;
        }

        result = CreateResolved(requestedName, candidate, MechanismKind.ApplicationDirectory);
        return true;
    }

    private static bool TryResolveDefaultSystemLocations(
        string requestedName, 
        string normalized,
        out LibraryResolutionResult result
    )
    {
        var sysDir = Environment.SystemDirectory;
        var winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

        var candidate = TryResolveInDirectories(normalized, [sysDir, winDir]);
        if (candidate is null)
        {
            result = default!;
            return false;
        }

        result = CreateResolved(requestedName, candidate, MechanismKind.DefaultSystemLocations);
        return true;
    }

    private static bool TryResolveEnvironmentPath(
        string requestedName, 
        string normalized, 
        out LibraryResolutionResult result
    )
    {
        var candidate = TryResolveInDirectories(
            normalized,
            SplitSearchPath(Environment.GetEnvironmentVariable("PATH"))
        );

        if (candidate is null)
        {
            result = default!;
            return false;
        }

        result = CreateResolved(requestedName, candidate, MechanismKind.EnvironmentOverride);
        return true;
    }

    private static string NormalizeLibraryName(string name)
    {
        return Path.HasExtension(name) ? name : Path.ChangeExtension(name, ".dll");
    }

    private static string[] CollectHeuristicCandidates(string normalized)
    {
        var cwd = Directory.GetCurrentDirectory();
        var candidate = GetCandidatePath(cwd, normalized);
        return candidate is null ? [] : [candidate];
    }
}
