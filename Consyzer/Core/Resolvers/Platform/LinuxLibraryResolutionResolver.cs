using Consyzer.Core.Models;

namespace Consyzer.Core.Resolvers.Platform;

internal sealed class LinuxLibraryResolutionResolver(string analyzedDirectory)
    : PlatformLibraryResolutionResolverBase
{
    private readonly string _analyzedDirectory = Path.GetFullPath(analyzedDirectory);

    // Linux has a lot of mechanisms we can't simulate.
    private const NotSimulatedMechanisms NotSimulated =
        NotSimulatedMechanisms.LinuxRPathRunPath
        | NotSimulatedMechanisms.LinuxLdSoCache
        | NotSimulatedMechanisms.LinuxLdSoConf
        | NotSimulatedMechanisms.LinuxSecureExecution
        | NotSimulatedMechanisms.LinuxTransitiveDependencies
        | NotSimulatedMechanisms.LinuxMultiarchDefaultPaths
        | NotSimulatedMechanisms.LinuxLdPreload;

    private static readonly string[] DefaultSystemLocations =
    [
        "/lib",
        "/usr/lib",
        "/lib64",
        "/usr/lib64",
        "/usr/local/lib"
    ];

    public override LibraryResolutionResult Resolve(string file)
    {
        var normalized = NormalizeLibraryName(file);

        // TO DO: make state-machine

        if (TryResolveExplicit(file, normalized, out var result)) return result;
        if (TryResolveLdLibraryPath(file, normalized, out result)) return result;
        if (TryResolveDefaultSystemLocations(file, normalized, out result)) return result;

        return CreateInconclusive(file, CollectHeuristicCandidates(normalized), NotSimulated);
    }

    private static bool TryResolveExplicit(string requestedName, string normalized, out LibraryResolutionResult result)
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

    private static bool TryResolveLdLibraryPath(
        string requestedName, 
        string normalized, 
        out LibraryResolutionResult result
    )
    {
        var candidate = TryResolveInDirectories(
            normalized,
            SplitSearchPath(Environment.GetEnvironmentVariable("LD_LIBRARY_PATH"))
        );

        if (candidate is null)
        {
            result = default!;
            return false;
        }

        result = CreateResolved(requestedName, candidate, MechanismKind.EnvironmentOverride);
        return true;
    }

    private static bool TryResolveDefaultSystemLocations(
        string requestedName, 
        string normalized, 
        out LibraryResolutionResult result
    )
    {
        var candidate = TryResolveInDirectories(normalized, DefaultSystemLocations);
        if (candidate is null)
        {
            result = default!;
            return false;
        }

        result = CreateResolved(requestedName, candidate, MechanismKind.DefaultSystemLocations);
        return true;
    }

    private static string NormalizeLibraryName(string input)
    {
        // Keep as-is if extension exists or contains ".so" somewhere (covers "libm.so.6" too).
        if (Path.HasExtension(input) || input.Contains(".so", StringComparison.Ordinal))
        {
            return input;
        }

        var extensioned = Path.ChangeExtension(input, "so");

        return input.StartsWith("lib", StringComparison.Ordinal)
            ? extensioned
            : "lib" + extensioned;
    }

    private string[] CollectHeuristicCandidates(string normalized)
    {
        string? a = GetCandidatePath(_analyzedDirectory, normalized);
        string? b = GetCandidatePath(Directory.GetCurrentDirectory(), normalized);

        if (a is null && b is null) return [];
        if (a is null) return [b!];
        if (b is null) return [a];
        return [a, b];
    }
}