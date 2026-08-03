using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using Consyzer.Core.Caching;
using Consyzer.Core.Models.Metadata;

namespace Consyzer.Core.Extractors;

internal sealed class PInvokeMethodExtractor(
    IResourceCache<FileInfo, PEReader> peReaderCache
) : IExtractor<FileInfo, IEnumerable<PInvokeMethod>>
{
    public IEnumerable<PInvokeMethod> Extract(FileInfo file)
    {
        var peReader = peReaderCache.GetOrAdd(file);
        var mdReader = peReader.GetMetadataReader();

        return GetPInvokeMethods(mdReader);
    }

    private static List<PInvokeMethod> GetPInvokeMethods(
        MetadataReader mdReader
    )
    {
        var signatureExtractor = new MethodSignatureExtractor(mdReader);
        var assemblyHasDllImportSearchPathOverride =
            mdReader.IsAssembly
            && HasAttribute<DefaultDllImportSearchPathsAttribute>(
                mdReader,
                mdReader.GetAssemblyDefinition().GetCustomAttributes()
            );

        var methods = new List<PInvokeMethod>();

        foreach (var handle in mdReader.MethodDefinitions)
        {
            var definition = mdReader.GetMethodDefinition(handle);

            if (!IsPInvokeMethod(definition)) continue;

            methods.Add(ToPInvokeMethod(
                mdReader,
                definition,
                signatureExtractor,
                assemblyHasDllImportSearchPathOverride
            ));
        }

        return methods;
    }

    private static bool IsPInvokeMethod(MethodDefinition methodDef)
    {
        return methodDef.Attributes.HasFlag(MethodAttributes.PinvokeImpl);
    }

    private static PInvokeMethod ToPInvokeMethod(
        MetadataReader mdReader,
        MethodDefinition methodDef,
        MethodSignatureExtractor signatureExtractor,
        bool assemblyHasDllImportSearchPathOverride
    )
    {
        var import = methodDef.GetImport();
        var module = mdReader.GetModuleReference(import.Module);

        return new PInvokeMethod
        {
            Signature = signatureExtractor.Extract(methodDef),
            ImportName = mdReader.GetString(module.Name),
            ImportFlags = import.Attributes,
            HasDllImportSearchPathOverride =
                assemblyHasDllImportSearchPathOverride
                || HasAttribute<DefaultDllImportSearchPathsAttribute>(
                    mdReader,
                    methodDef.GetCustomAttributes()
                )
        };
    }

    private static bool HasAttribute<TAttribute>(
        MetadataReader mdReader,
        CustomAttributeHandleCollection attributes
    )
        where TAttribute : Attribute
    {
        var expectedType = typeof(TAttribute);

        foreach (var handle in attributes)
        {
            var attribute = mdReader.GetCustomAttribute(handle);
            if (attribute.Constructor.Kind != HandleKind.MemberReference)
            {
                continue;
            }

            var constructor = mdReader.GetMemberReference(
                (MemberReferenceHandle)attribute.Constructor
            );
            if (constructor.Parent.Kind != HandleKind.TypeReference)
            {
                continue;
            }

            var attributeType = mdReader.GetTypeReference(
                (TypeReferenceHandle)constructor.Parent
            );
            if (mdReader.StringComparer.Equals(attributeType.Name, expectedType.Name)
                && mdReader.StringComparer.Equals(
                    attributeType.Namespace,
                    expectedType.Namespace!
                ))
            {
                return true;
            }
        }

        return false;
    }
}
