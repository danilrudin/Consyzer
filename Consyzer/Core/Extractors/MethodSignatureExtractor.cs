using System.Reflection;
using System.Reflection.Metadata;
using System.Collections.Immutable;
using Consyzer.Core.Extractors.Providers;
using Consyzer.Core.Models.Metadata;

namespace Consyzer.Core.Extractors;

internal sealed class MethodSignatureExtractor(
    MetadataReader mdReader
) : IExtractor<MethodDefinition, MethodSignature>
{
    private static readonly StringSignatureTypeProvider SignatureTypeProvider = new();

    public MethodSignature Extract(MethodDefinition methodDef)
    {
        var signature = DecodeSignature(methodDef);
        var typeDef = GetDeclaringTypeDefinition(methodDef);

        return new MethodSignature
        {
            ReturnType = GetReturnType(signature),
            IsStatic = IsStatic(methodDef),
            Namespace = GetNamespace(typeDef),
            Class = GetClassName(typeDef),
            Method = GetMethodName(methodDef),
            MethodArguments = GetArguments(signature)
        };
    }

    private static string GetReturnType(MethodSignature<string> signature)
    {
        return signature.ReturnType;
    }

    private static bool IsStatic(MethodDefinition methodDef)
    {
        return methodDef.Attributes.HasFlag(MethodAttributes.Static);
    }

    private string GetNamespace(TypeDefinition typeDef)
    {
        return mdReader.GetString(typeDef.Namespace);
    }

    private string GetClassName(TypeDefinition typeDef)
    {
        return mdReader.GetString(typeDef.Name);
    }

    private string GetMethodName(MethodDefinition methodDef)
    {
        return mdReader.GetString(methodDef.Name);
    }

    private static ImmutableArray<string> GetArguments(MethodSignature<string> signature)
    {
        return signature.ParameterTypes;
    }

    private TypeDefinition GetDeclaringTypeDefinition(MethodDefinition methodDef)
    {
        var decType = methodDef.GetDeclaringType();
        return mdReader.GetTypeDefinition(decType);
    }

    private static MethodSignature<string> DecodeSignature(MethodDefinition methodDef)
    {
        return methodDef.DecodeSignature(SignatureTypeProvider, new object());
    }
}
