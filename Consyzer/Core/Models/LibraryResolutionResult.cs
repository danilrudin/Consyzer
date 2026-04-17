namespace Consyzer.Core.Models;

internal sealed class LibraryResolutionResult
{
    public required string LibraryName { get; init; }

    public required ResolutionState State { get; init; }

    /// <summary>
    /// Filled only when State is Resolved.
    /// </summary>
    public ResolvedPresence? Resolved { get; init; }

    /// <summary>
    /// Paths found by Consyzer heuristics (not counted as strict success).
    /// </summary>
    public required IReadOnlyList<string> HeuristicCandidates { get; init; }

    /// <summary>
    /// Flags indicating which loader mechanisms are NOT simulated in current strict version.
    /// Used to justify Inconclusive.
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
    PlatformSpecificMechanism = 4
}

[Flags]
internal enum NotSimulatedMechanisms
{
    None = 0,

    // Windows (bit range 0..9)
    WindowsSxS = 1 << 0,
    WindowsKnownDlls = 1 << 1,
    WindowsDllRedirection = 1 << 2,
    WindowsProcessDirectoryOverrides = 1 << 3,
    WindowsApiSetSchema = 1 << 4,
    WindowsPackageGraph = 1 << 5,
    WindowsLoadedModuleList = 1 << 6,
    WindowsSafeSearchModeAndFlags = 1 << 7,

    WindowsAppPathsRegistry = 1 << 8,
    WindowsDotNetSearchPathOverrides = 1 << 9,

    // Linux (bit range 10..16)
    LinuxRPathRunPath = 1 << 10,
    LinuxLdSoCache = 1 << 11,
    LinuxLdSoConf = 1 << 12,
    LinuxSecureExecution = 1 << 13,
    LinuxTransitiveDependencies = 1 << 14,
    LinuxMultiarchDefaultPaths = 1 << 15,
    LinuxLdPreload = 1 << 16,

    // macOS (bit range 17..23)
    MacOsAtRPathLoaderExecutablePath = 1 << 17,
    MacOsDyldFallbackLibraryPath = 1 << 18,
    MacOsProtectedProcessRestrictions = 1 << 19,
    MacOsDyldSharedCacheOrOverrides = 1 << 20,
    MacOsTransitiveDependencies = 1 << 21,

    MacOsDyldFrameworkPathOrFrameworkSearch = 1 << 22,
    MacOsDyldInsertLibraries = 1 << 23
}
