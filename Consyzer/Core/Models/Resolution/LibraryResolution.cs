namespace Consyzer.Core.Models.Resolution;

internal sealed class LibraryResolution
{
    private LibraryResolution(
        string targetPath,
        string libraryName,
        ResolutionState resolutionState,
        ResolvedPresence? resolvedPresence,
        IReadOnlyList<string> heuristicCandidates,
        NotSimulatedMechanisms notSimulated
    )
    {
        TargetPath = targetPath;
        LibraryName = libraryName;
        ResolutionState = resolutionState;
        ResolvedPresence = resolvedPresence;
        HeuristicCandidates = heuristicCandidates;
        NotSimulated = notSimulated;
    }

    /// <summary>
    /// Path to the managed binary where the native dependency was declared.
    /// </summary>
    public string TargetPath { get; }

    /// <summary>
    /// Native library name declared by the target binary.
    /// </summary>
    public string LibraryName { get; }

    /// <summary>
    /// Final resolution state for the native dependency.
    /// </summary>
    public ResolutionState ResolutionState { get; }

    /// <summary>
    /// Resolved native library information.
    /// Set only when <see cref="ResolutionState"/> is <see cref="ResolutionState.Resolved"/>.
    /// </summary>
    public ResolvedPresence? ResolvedPresence { get; }

    /// <summary>
    /// Candidate paths discovered by heuristic checks.
    /// These candidates are reported for diagnostics only and do not affect <see cref="ResolutionState"/>.
    /// </summary>
    public IReadOnlyList<string> HeuristicCandidates { get; }

    /// <summary>
    /// Loader mechanisms that are not simulated by the current resolver.
    /// These flags explain why unresolved results can be inconclusive.
    /// </summary>
    public NotSimulatedMechanisms NotSimulated { get; }

    public static LibraryResolution CreateResolved(
        string targetPath,
        string libraryName,
        ResolvedPresence resolvedPresence,
        IReadOnlyList<string> heuristicCandidates,
        NotSimulatedMechanisms notSimulated = NotSimulatedMechanisms.None
    )
    {
        ArgumentNullException.ThrowIfNull(resolvedPresence);

        return new LibraryResolution(
            targetPath,
            libraryName,
            ResolutionState.Resolved,
            resolvedPresence,
            heuristicCandidates,
            notSimulated
        );
    }

    public static LibraryResolution CreateMissing(
        string targetPath,
        string libraryName,
        IReadOnlyList<string> heuristicCandidates
    )
        => new(
            targetPath,
            libraryName,
            ResolutionState.Missing,
            null,
            heuristicCandidates,
            NotSimulatedMechanisms.None
        );

    public static LibraryResolution CreateInconclusive(
        string targetPath,
        string libraryName,
        IReadOnlyList<string> heuristicCandidates,
        NotSimulatedMechanisms notSimulated
    )
    {
        if (notSimulated == NotSimulatedMechanisms.None)
        {
            throw new ArgumentException(
                "Inconclusive resolution requires at least one unsimulated mechanism.",
                nameof(notSimulated)
            );
        }

        return new LibraryResolution(
            targetPath,
            libraryName,
            ResolutionState.Inconclusive,
            null,
            heuristicCandidates,
            notSimulated
        );
    }
}

internal enum ResolutionState
{
    Resolved = 0,
    Missing = 1,
    Inconclusive = 2
}

internal sealed record ResolvedPresence(
    string Path,
    MechanismKind MechanismKind
);

/// <summary>
/// Loader mechanism category for reporting only.
/// </summary>
internal enum MechanismKind
{
    ExplicitPath = 0,
    AssemblyDirectory = 1,
    DefaultSystemLocations = 2,
    EnvironmentOverride = 3,
    PlatformSpecificMechanism = 4,
    CurrentDirectory = 5
}

[Flags]
internal enum NotSimulatedMechanisms : ulong
{
    None = 0,

    // Windows (bit range 0..15)
    WindowsSxS = 1UL << 0,
    WindowsKnownDlls = 1UL << 1,
    WindowsDllRedirection = 1UL << 2,
    WindowsProcessDirectoryOverrides = 1UL << 3,
    WindowsApiSetSchema = 1UL << 4,
    WindowsPackageGraph = 1UL << 5,
    WindowsLoadedModuleList = 1UL << 6,
    WindowsSafeSearchModeAndFlags = 1UL << 7,
    WindowsAppPathsRegistry = 1UL << 8,
    WindowsDotNetSearchPathOverrides = 1UL << 9,
    WindowsProcessApplicationDirectory = 1UL << 10,

    // Linux (bit range 16..31)
    LinuxRPathRunPath = 1UL << 16,
    LinuxLdSoCache = 1UL << 17,
    LinuxLdSoConf = 1UL << 18,
    LinuxSecureExecution = 1UL << 19,
    LinuxTransitiveDependencies = 1UL << 20,
    LinuxMultiarchDefaultPaths = 1UL << 21,
    LinuxLdPreload = 1UL << 22,
    LinuxLdLibraryPathDynamicStringTokens = 1UL << 23,
    LinuxDependencyDynamicStringTokens = 1UL << 24,
    LinuxDotNetSearchPathOverrides = 1UL << 25,

    // macOS (bit range 32..47)
    MacOsAtRPathLoaderExecutablePath = 1UL << 32,
    MacOsDyldFallbackLibraryPath = 1UL << 33,
    MacOsProtectedProcessRestrictions = 1UL << 34,
    MacOsDyldSharedCacheOrOverrides = 1UL << 35,
    MacOsTransitiveDependencies = 1UL << 36,
    MacOsDyldFrameworkPathOrFrameworkSearch = 1UL << 37,
    MacOsDyldInsertLibraries = 1UL << 38
}
