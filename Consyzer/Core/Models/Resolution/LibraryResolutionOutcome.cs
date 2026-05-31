namespace Consyzer.Core.Models.Resolution;

internal sealed class LibraryResolutionOutcome
{
    public required string Platform { get; init; }
    public required IReadOnlyList<LibraryResolution> Results { get; init; }
}
