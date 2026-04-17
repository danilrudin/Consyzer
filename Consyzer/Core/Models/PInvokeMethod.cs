using System.Reflection;

namespace Consyzer.Core.Models;

internal sealed class PInvokeMethodGroup
{
    public required FileInfo File { get; set; }
    public required IReadOnlyList<PInvokeMethod> Methods { get; set; }
}

internal sealed class PInvokeMethod
{
    public required MethodSignature Signature { get; init; }
    public required string ImportName { get; init; }
    public required MethodImportAttributes ImportFlags { get; init; }
}
