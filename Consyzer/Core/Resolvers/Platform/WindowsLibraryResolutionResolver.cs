using Consyzer.Core.Models.Resolution;

namespace Consyzer.Core.Resolvers.Platform;

internal sealed class WindowsLibraryResolutionResolver(
    string analysisDirectory
) : PlatformLibraryResolutionResolverBase
{
    private const string LibraryExtension = ".dll";
    private const string ExecutableExtension = ".exe";
    private const string EnvironmentVariablePath = "PATH";
    private readonly string _analysisDirectory = Path.GetFullPath(analysisDirectory);

    public override string PlatformName => "Windows";

    // Windows has a lot of mechanisms we can't simulate statically.
    private const NotSimulatedMechanisms NotSimulated =
        NotSimulatedMechanisms.WindowsSxS
        | NotSimulatedMechanisms.WindowsKnownDlls
        | NotSimulatedMechanisms.WindowsDllRedirection
        | NotSimulatedMechanisms.WindowsProcessDirectoryOverrides
        | NotSimulatedMechanisms.WindowsApiSetSchema
        | NotSimulatedMechanisms.WindowsPackageGraph
        | NotSimulatedMechanisms.WindowsLoadedModuleList
        | NotSimulatedMechanisms.WindowsSafeSearchModeAndFlags
        | NotSimulatedMechanisms.WindowsDotNetSearchPathOverrides
        | NotSimulatedMechanisms.WindowsProcessApplicationDirectory;

    public override LibraryResolution Resolve(LibraryResolutionContext context)
    {
        var candidates = GetLibraryNameCandidates(context.LibraryName);
        var heuristicCandidates = IsExplicitPath(context.LibraryName)
            ? []
            : CollectHeuristicCandidates(
                context,
                candidates,
                _analysisDirectory,
                StringComparer.OrdinalIgnoreCase
            );

        if (TryResolveExplicit(
            context,
            context.LibraryName,
            candidates,
            heuristicCandidates,
            out var result
        ))
        {
            if (context.HasDllImportSearchPathOverride
                && result.ResolutionState == ResolutionState.Resolved
                && !Path.IsPathRooted(context.LibraryName))
            {
                return CreateInconclusive(
                    context,
                    heuristicCandidates,
                    NotSimulatedMechanisms.WindowsDotNetSearchPathOverrides
                );
            }

            return result;
        }

        if (context.HasDllImportSearchPathOverride)
        {
            return CreateInconclusive(
                context,
                heuristicCandidates,
                NotSimulatedMechanisms.WindowsDotNetSearchPathOverrides
            );
        }

        var defaultSystemLocations = GetDefaultSystemLocations();
        var currentDirectory = Directory.GetCurrentDirectory();
        var environmentDirectories = SplitSearchPath(
            Environment.GetEnvironmentVariable(EnvironmentVariablePath)
        ).ToArray();

        foreach (var candidate in candidates)
        {
            if (TryResolveAssemblyDirectory(
                context,
                candidate,
                heuristicCandidates,
                out result
            ))
            {
                return result;
            }

            if (TryResolveInDirectories(
                context,
                candidate,
                defaultSystemLocations,
                MechanismKind.DefaultSystemLocations,
                heuristicCandidates,
                out result
            ))
            {
                return result;
            }

            if (TryResolveInDirectories(
                context,
                candidate,
                [currentDirectory],
                MechanismKind.CurrentDirectory,
                heuristicCandidates,
                out result
            ))
            {
                return result;
            }

            if (TryResolveInDirectories(
                context,
                candidate,
                environmentDirectories,
                MechanismKind.EnvironmentOverride,
                heuristicCandidates,
                out result
            ))
            {
                return result;
            }
        }

        return CreateInconclusive(context, heuristicCandidates, NotSimulated);
    }

    private static IReadOnlyList<string> GetLibraryNameCandidates(string input)
    {
        if (input.EndsWith(LibraryExtension, StringComparison.OrdinalIgnoreCase)
            || input.EndsWith(ExecutableExtension, StringComparison.OrdinalIgnoreCase)
            || input.EndsWith('.'))
        {
            return [input];
        }

        if (Path.IsPathRooted(input))
        {
            return Path.HasExtension(input)
                ? [input]
                : [input + LibraryExtension];
        }

        if (!Path.HasExtension(input))
        {
            // LoadLibrary resolves the first runtime variation to the physical .dll file too.
            return [input + LibraryExtension];
        }

        return DistinctCandidates(
            [input, input + LibraryExtension],
            StringComparer.OrdinalIgnoreCase
        );
    }

    private static string?[] GetDefaultSystemLocations() =>
    [
        Environment.SystemDirectory,
        Environment.GetFolderPath(Environment.SpecialFolder.Windows)
    ];
}
