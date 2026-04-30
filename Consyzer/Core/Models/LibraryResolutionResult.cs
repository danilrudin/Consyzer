namespace Consyzer.Core.Models;

internal sealed class LibraryResolutionResult
{
    public required string LibraryName { get; init; }

    public required ResolutionState State { get; init; }

    /// <summary>
    /// Resolved library information.
    /// Set only when <see cref="State"/> is <see cref="ResolutionState.Resolved"/>.
    /// </summary>
    public ResolvedPresence? Resolved { get; init; }

    /// <summary>
    /// Candidate paths discovered by heuristic checks.
    /// Reported for diagnostics only; do not affect the resolution result.
    /// </summary>
    public required IReadOnlyList<string> HeuristicCandidates { get; init; }

    /// <summary>
    /// Flags indicating which loader mechanisms are not simulated by the current resolver.
    /// These flags explain why the resolution may be inconclusive.
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
internal enum NotSimulatedMechanisms : ulong
{
    None = 0,

    // Windows (bit range 0..9)
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

    // Linux (bit range 10..16)
    LinuxRPathRunPath = 1UL << 10,
    LinuxLdSoCache = 1UL << 11,
    LinuxLdSoConf = 1UL << 12,
    LinuxSecureExecution = 1UL << 13,
    LinuxTransitiveDependencies = 1UL << 14,
    LinuxMultiarchDefaultPaths = 1UL << 15,
    LinuxLdPreload = 1UL << 16,

    // macOS (bit range 17..23)
    MacOsAtRPathLoaderExecutablePath = 1UL << 17,
    MacOsDyldFallbackLibraryPath = 1UL << 18,
    MacOsProtectedProcessRestrictions = 1UL << 19,
    MacOsDyldSharedCacheOrOverrides = 1UL << 20,
    MacOsTransitiveDependencies = 1UL << 21,
    MacOsDyldFrameworkPathOrFrameworkSearch = 1UL << 22,
    MacOsDyldInsertLibraries = 1UL << 23
}
