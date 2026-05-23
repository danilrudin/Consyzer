namespace Consyzer.Core.Models.Metadata;

internal sealed class MethodSignature
{
    public required string ReturnType { get; init; }
    public required bool IsStatic { get; init; }
    public required string Namespace { get; init; }
    public required string Class { get; init; }
    public required string Method { get; init; }
    public required IReadOnlyList<string> MethodArguments { get; init; }

    public override string ToString()
    {
        string args = string.Join(", ", MethodArguments);
        string isStatic = IsStatic ? " static" : string.Empty;
        return $"{ReturnType}{isStatic} {GetMethodLocation()}({args})";
    }

    public string GetMethodLocation() => $"{Namespace}.{Class}.{Method}";
}
