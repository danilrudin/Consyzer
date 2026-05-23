namespace Consyzer.Core.Models.Resolution;

internal sealed record LibraryResolutionContext(
    FileInfo TargetFile,
    string LibraryName
);
