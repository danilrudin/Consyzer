namespace Consyzer.Core.Models.Resolution;

internal sealed class LibraryResolutionResult
{
    /// <summary>
    /// Path to the managed binary where the native dependency was declared.
    /// </summary>
    public required string TargetPath { get; init; }

    /// <summary>
    /// Native library name declared by the target binary.
    /// </summary>
    public required string LibraryName { get; init; }

    /// <summary>
    /// Platform whose native library loading rules were used for resolution.
    /// </summary>
    public required string Platform { get; init; }

    /// <summary>
    /// Final resolution state for the native dependency.
    /// </summary>
    public required ResolutionState ResolutionState { get; init; }

    /// <summary>
    /// Resolved native library information.
    /// Set only when <see cref="ResolutionState"/> is <see cref="ResolutionState.Resolved"/>.
    /// </summary>
    public ResolvedPresence? ResolvedPresence { get; init; }

    /// <summary>
    /// Candidate paths discovered by heuristic checks.
    /// These candidates are reported for diagnostics only and do not affect <see cref="ResolutionState"/>.
    /// </summary>
    public required IReadOnlyList<string> HeuristicCandidates { get; init; }

    /// <summary>
    /// Loader mechanisms that are not simulated by the current resolver.
    /// These flags explain why unresolved results can be inconclusive.
    /// </summary>
    public required NotSimulatedMechanisms NotSimulated { get; init; }
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
    ApplicationDirectory = 1,
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

    // Linux (bit range 16..31)
    LinuxRPathRunPath = 1UL << 16,
    LinuxLdSoCache = 1UL << 17,
    LinuxLdSoConf = 1UL << 18,
    LinuxSecureExecution = 1UL << 19,
    LinuxTransitiveDependencies = 1UL << 20,
    LinuxMultiarchDefaultPaths = 1UL << 21,
    LinuxLdPreload = 1UL << 22,
    LinuxLdLibraryPathDynamicStringTokens = 1UL << 23,

    // macOS (bit range 32..47)
    MacOsAtRPathLoaderExecutablePath = 1UL << 32,
    MacOsDyldFallbackLibraryPath = 1UL << 33,
    MacOsProtectedProcessRestrictions = 1UL << 34,
    MacOsDyldSharedCacheOrOverrides = 1UL << 35,
    MacOsTransitiveDependencies = 1UL << 36,
    MacOsDyldFrameworkPathOrFrameworkSearch = 1UL << 37,
    MacOsDyldInsertLibraries = 1UL << 38
}
