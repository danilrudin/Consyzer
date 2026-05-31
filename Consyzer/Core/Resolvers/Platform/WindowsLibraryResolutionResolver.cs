using Consyzer.Core.Models.Resolution;

namespace Consyzer.Core.Resolvers.Platform;

internal sealed class WindowsLibraryResolutionResolver(
    string analyzedDirectory
) : PlatformLibraryResolutionResolverBase
{
    private const string LibraryExtension = ".dll";
    private const string ExecutableExtension = ".exe";
    private const string EnvironmentVariablePath = "PATH";
    private readonly string _analyzedDirectory = Path.GetFullPath(analyzedDirectory);

    public override string PlatformName => "Windows";

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

    public override LibraryResolution Resolve(LibraryResolutionContext context)
    {
        var candidates = GetLibraryNameCandidates(context.LibraryName);

        if (TryResolveExplicit(context, candidates[0], out var result)) return result;
        if (TryResolveApplicationDirectory(context, candidates, out result)) return result;
        if (TryResolveDefaultSystemLocations(context, candidates, out result)) return result;
        if (TryResolveCurrentDirectory(context, candidates, out result)) return result;
        if (TryResolveEnvironmentPath(context, candidates, out result)) return result;

        return CreateInconclusive(context, NotSimulated);
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
        LibraryResolutionContext context,
        string normalized,
        out LibraryResolution result
    )
    {
        if (!TryGetExplicitPathCandidate(normalized, out var candidate))
        {
            result = default!;
            return false;
        }

        result = candidate is not null
            ? CreateResolved(context, candidate, MechanismKind.ExplicitPath)
            : CreateMissing(context);

        return true;
    }

    private bool TryResolveApplicationDirectory(
        LibraryResolutionContext context,
        IReadOnlyList<string> candidates,
        out LibraryResolution result
    )
    {
        var candidate = TryResolveInDirectories(candidates, [_analyzedDirectory]);
        if (candidate is null)
        {
            result = default!;
            return false;
        }

        result = CreateResolved(context, candidate, MechanismKind.ApplicationDirectory);
        return true;
    }

    private static bool TryResolveDefaultSystemLocations(
        LibraryResolutionContext context,
        IReadOnlyList<string> candidates,
        out LibraryResolution result
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

        result = CreateResolved(context, candidate, MechanismKind.DefaultSystemLocations);
        return true;
    }

    private static bool TryResolveCurrentDirectory(
        LibraryResolutionContext context,
        IReadOnlyList<string> candidates,
        out LibraryResolution result
    )
    {
        var candidate = TryResolveInDirectories(candidates, [Directory.GetCurrentDirectory()]);
        if (candidate is null)
        {
            result = default!;
            return false;
        }

        result = CreateResolved(context, candidate, MechanismKind.CurrentDirectory);
        return true;
    }

    private static bool TryResolveEnvironmentPath(
        LibraryResolutionContext context,
        IReadOnlyList<string> candidates,
        out LibraryResolution result
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

        result = CreateResolved(context, candidate, MechanismKind.EnvironmentOverride);
        return true;
    }
}
