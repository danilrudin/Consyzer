namespace Consyzer.Core.Models;

internal sealed record LibraryResolutionContext(
    FileInfo TargetFile,
    string LibraryName
);
