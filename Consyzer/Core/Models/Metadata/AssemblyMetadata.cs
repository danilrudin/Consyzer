namespace Consyzer.Core.Models.Metadata;

internal sealed class AssemblyMetadata
{
    public required FileInfo File { get; init; }
    public required string Version { get; init; }
    public required DateTime CreationDateUtc { get; init; }
    public required string Sha256 { get; init; }
}
