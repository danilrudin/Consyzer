using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
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

        var methods = new List<PInvokeMethod>();

        foreach (var handle in mdReader.MethodDefinitions)
        {
            var definition = mdReader.GetMethodDefinition(handle);

            if (!IsPInvokeMethod(definition)) continue;

            methods.Add(ToPInvokeMethod(mdReader, definition, signatureExtractor));
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
        MethodSignatureExtractor signatureExtractor
    )
    {
        var import = methodDef.GetImport();
        var module = mdReader.GetModuleReference(import.Module);

        return new PInvokeMethod
        {
            Signature = signatureExtractor.Extract(methodDef),
            ImportName = mdReader.GetString(module.Name),
            ImportFlags = import.Attributes
        };
    }
}
