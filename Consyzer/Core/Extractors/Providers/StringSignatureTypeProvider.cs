using System.Reflection.Metadata;
using System.Collections.Immutable;

namespace Consyzer.Core.Extractors.Providers;

internal sealed class StringSignatureTypeProvider : ISignatureTypeProvider<string, object>
{
    public string GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode.ToString();

    public string GetPinnedType(string elementType) => elementType;

    public string GetGenericMethodParameter(object genericContext, int index) => $"T{index}";

    public string GetGenericTypeParameter(object genericContext, int index) => $"T{index}";

    public string GetByReferenceType(string elementType) => $"{elementType}&";

    public string GetPointerType(string elementType) => $"{elementType}*";

    public string GetSZArrayType(string elementType) => $"{elementType}[]";

    public string GetArrayType(string elementType, ArrayShape shape)
    {
        string rank = new(',', shape.Rank - 1);
        return $"{elementType}[{rank}]";
    }

    public string GetFunctionPointerType(MethodSignature<string> signature)
    {
        string parameterTypes = string.Join(", ", signature.ParameterTypes);
        return $"{signature.ReturnType}*({parameterTypes})";
    }

    public string GetGenericInstantiation(string genericType, ImmutableArray<string> typeArguments)
    {
        string separatedArguments = string.Join(", ", typeArguments);
        return $"{genericType}<{separatedArguments}>";
    }

    public string GetModifiedType(string modifier, string unmodifiedType, bool isRequired)
    {
        string modifierText = isRequired ? "modreq" : "modopt";
        return $"{modifierText}({modifier}) {unmodifiedType}";
    }

    public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
    {
        return TypeToString(handle, reader);
    }

    public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
    {
        return TypeToString(handle, reader);
    }

    public string GetTypeFromSpecification(MetadataReader reader, object genericContext, TypeSpecificationHandle handle, byte rawTypeKind)
    {
        return TypeToString(handle, genericContext, reader);
    }

    private string TypeToString(EntityHandle handle, in object? typeContext, MetadataReader reader)
    {
        return handle.Kind switch
        {
            HandleKind.TypeDefinition => TypeToString((TypeDefinitionHandle)handle, reader),
            HandleKind.TypeReference => TypeToString((TypeReferenceHandle)handle, reader),
            HandleKind.TypeSpecification => TypeToString((TypeSpecificationHandle)handle, reader, typeContext!),
            _ => "?"
        };
    }

    private static string TypeToString(TypeDefinitionHandle handle, MetadataReader reader)
    {
        var typeDefinition = reader.GetTypeDefinition(handle);
        string @namespace = reader.GetString(typeDefinition.Namespace);
        string name = reader.GetString(typeDefinition.Name);

        if (typeDefinition.IsNested)
        {
            string declaringTypeName = TypeToString(typeDefinition.GetDeclaringType(), reader);
            name = declaringTypeName + "+" + name;
        }

        return string.IsNullOrEmpty(@namespace) ? name : $"{@namespace}.{name}";
    }

    private string TypeToString(TypeReferenceHandle handle, MetadataReader reader)
    {
        TypeReference typeReference = reader.GetTypeReference(handle);
        string @namespace = reader.GetString(typeReference.Namespace);
        string name = reader.GetString(typeReference.Name);

        if (typeReference.ResolutionScope.Kind == HandleKind.TypeDefinition || typeReference.ResolutionScope.Kind == HandleKind.TypeReference)
        {
            string declaringTypeName = TypeToString(typeReference.ResolutionScope, default, reader);
            name = declaringTypeName + "+" + name;
        }

        return string.IsNullOrEmpty(@namespace) ? name : $"{@namespace}.{name}";
    }

    private string TypeToString(TypeSpecificationHandle handle, MetadataReader reader, in object typeContext)
    {
        return reader.GetTypeSpecification(handle).DecodeSignature(this, typeContext);
    }
}
