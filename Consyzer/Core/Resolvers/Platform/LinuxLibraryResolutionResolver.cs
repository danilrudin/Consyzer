using System.Runtime.InteropServices;
using Consyzer.Core.Models.Resolution;

namespace Consyzer.Core.Resolvers.Platform;

internal sealed class LinuxLibraryResolutionResolver(
    string analysisDirectory
) : PlatformLibraryResolutionResolverBase
{
    private const string LibraryExtension = ".so";
    private const string EnvironmentVariablePath = "LD_LIBRARY_PATH";
    private readonly string _analysisDirectory = Path.GetFullPath(analysisDirectory);

    public override string PlatformName => "Linux";

    // Linux has loader mechanisms that can't be reproduced reliably by static probing.
    private const NotSimulatedMechanisms NotSimulated =
        NotSimulatedMechanisms.LinuxRPathRunPath
        | NotSimulatedMechanisms.LinuxLdSoCache
        | NotSimulatedMechanisms.LinuxLdSoConf
        | NotSimulatedMechanisms.LinuxSecureExecution
        | NotSimulatedMechanisms.LinuxTransitiveDependencies
        | NotSimulatedMechanisms.LinuxMultiarchDefaultPaths
        | NotSimulatedMechanisms.LinuxLdPreload
        | NotSimulatedMechanisms.LinuxDotNetSearchPathOverrides;

    private static readonly IReadOnlyList<string> DefaultSystemLocations = CreateDefaultSystemLocations();

    private static readonly string[] DynamicStringTokens =
    [
        "$ORIGIN",
        "${ORIGIN}",
        "$LIB",
        "${LIB}",
        "$PLATFORM",
        "${PLATFORM}"
    ];

    public override LibraryResolution Resolve(LibraryResolutionContext context)
    {
        var candidates = GetLibraryNameCandidates(context.LibraryName);
        var heuristicCandidates = IsExplicitPath(context.LibraryName)
            ? []
            : CollectHeuristicCandidates(
                context,
                candidates,
                _analysisDirectory,
                StringComparer.Ordinal
            );

        if (ContainsDynamicStringToken(context.LibraryName))
        {
            return CreateInconclusive(
                context,
                heuristicCandidates,
                NotSimulatedMechanisms.LinuxDependencyDynamicStringTokens
            );
        }

        if (Path.IsPathRooted(context.LibraryName))
        {
            return ResolveExplicitPath(
                context,
                candidates,
                heuristicCandidates
            );
        }

        if (IsExplicitPath(context.LibraryName))
        {
            return ResolveRelativeExplicitPath(context, candidates);
        }

        var ldLibraryPath = Environment.GetEnvironmentVariable(EnvironmentVariablePath);
        var notSimulatedCaveats = ContainsDynamicStringToken(ldLibraryPath)
            ? NotSimulatedMechanisms.LinuxLdLibraryPathDynamicStringTokens
            : NotSimulatedMechanisms.None;

        if (context.HasDllImportSearchPathOverride)
        {
            return CreateInconclusive(
                context,
                heuristicCandidates,
                NotSimulatedMechanisms.LinuxDotNetSearchPathOverrides
                | notSimulatedCaveats
            );
        }

        var ldLibraryPathDirectories = SplitSearchPath(
                ldLibraryPath,
                true,
                false,
                ':',
                ';'
            )
            .Where(path => !ContainsDynamicStringToken(path))
            .ToArray();

        foreach (var candidate in candidates)
        {
            if (TryResolveAssemblyDirectory(
                context,
                candidate,
                heuristicCandidates,
                out var result
            ))
            {
                return result;
            }

            if (TryResolveInDirectories(
                context,
                candidate,
                ldLibraryPathDirectories,
                MechanismKind.EnvironmentOverride,
                heuristicCandidates,
                out result,
                notSimulatedCaveats
            ))
            {
                return result;
            }

            if (TryResolveInDirectories(
                context,
                candidate,
                DefaultSystemLocations,
                MechanismKind.DefaultSystemLocations,
                heuristicCandidates,
                out result,
                notSimulatedCaveats
            ))
            {
                return result;
            }
        }

        return CreateInconclusive(
            context,
            heuristicCandidates,
            NotSimulated | notSimulatedCaveats
        );
    }

    private static LibraryResolution ResolveRelativeExplicitPath(
        LibraryResolutionContext context,
        IReadOnlyList<string> candidates
    )
    {
        if (context.HasDllImportSearchPathOverride)
        {
            return CreateInconclusive(
                context,
                [],
                NotSimulatedMechanisms.LinuxDotNetSearchPathOverrides
            );
        }

        var currentDirectory = Directory.GetCurrentDirectory();

        // .NET tries the assembly directory before passing a relative path to dlopen.
        // A path containing '/' bypasses LD_LIBRARY_PATH and system search directories.
        foreach (var candidate in candidates)
        {
            if (TryResolveAssemblyDirectory(context, candidate, [], out var result))
            {
                return result;
            }

            if (TryResolveInDirectories(
                context,
                candidate,
                [currentDirectory],
                MechanismKind.ExplicitPath,
                [],
                out result
            ))
            {
                return result;
            }
        }

        // Host-provided NATIVE_DLL_SEARCH_DIRECTORIES cannot be inferred from metadata.
        return CreateInconclusive(
            context,
            [],
            NotSimulatedMechanisms.LinuxDotNetSearchPathOverrides
        );
    }

    private static IReadOnlyList<string> GetLibraryNameCandidates(string input)
    {
        if (Path.IsPathRooted(input))
        {
            return [input];
        }

        var addLibPrefix = !IsExplicitPath(input);
        var candidates = new List<string>(4);

        if (ContainsSharedObjectSuffix(input))
        {
            var withCanonicalExtension = input + LibraryExtension;

            candidates.Add(input);
            if (addLibPrefix) candidates.Add(WithLibPrefix(input));
            candidates.Add(withCanonicalExtension);
            if (addLibPrefix) candidates.Add(WithLibPrefix(withCanonicalExtension));

            return DistinctCandidates(candidates, StringComparer.Ordinal);
        }

        var canonical = input + LibraryExtension;

        candidates.Add(canonical);
        if (addLibPrefix) candidates.Add(WithLibPrefix(canonical));
        candidates.Add(input);
        if (addLibPrefix) candidates.Add(WithLibPrefix(input));

        return DistinctCandidates(candidates, StringComparer.Ordinal);
    }

    private static List<string> CreateDefaultSystemLocations()
    {
        var directories = new List<string>
        {
            "/lib",
            "/usr/lib",
            "/lib64",
            "/usr/lib64"
        };

        string[] multiarchDirectories = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 =>
            [
                "/lib/x86_64-linux-gnu",
                "/usr/lib/x86_64-linux-gnu"
            ],
            Architecture.X86 =>
            [
                "/lib/i386-linux-gnu",
                "/usr/lib/i386-linux-gnu"
            ],
            Architecture.Arm64 =>
            [
                "/lib/aarch64-linux-gnu",
                "/usr/lib/aarch64-linux-gnu"
            ],
            Architecture.Arm =>
            [
                "/lib/arm-linux-gnueabihf",
                "/usr/lib/arm-linux-gnueabihf"
            ],
            _ => []
        };

        directories.AddRange(multiarchDirectories);

        return directories;
    }

    private static bool ContainsSharedObjectSuffix(string input)
    {
        // CoreCLR checks only the first occurrence, including directory components.
        var extensionIndex = input.IndexOf(LibraryExtension, StringComparison.Ordinal);
        if (extensionIndex < 0) return false;

        var suffixEnd = extensionIndex + LibraryExtension.Length;
        return suffixEnd == input.Length || input[suffixEnd] == '.';
    }

    private static string WithLibPrefix(string input) => "lib" + input;

    private static bool ContainsDynamicStringToken(string? path)
    {
        if (path is null) return false;

        return DynamicStringTokens.Any(token => path.Contains(token, StringComparison.Ordinal));
    }
}
