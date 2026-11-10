using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Consyzer.Core.Caching;
using Consyzer.Core.Models.Analysis;

namespace Consyzer.Core.Classifiers;

internal sealed class EcmaFileClassifier(
    IResourceCache<FileInfo, PEReader> peReaderCache
) : IFileClassifier<AnalysisFileClassification>
{
    public AnalysisFileClassification Classify(IEnumerable<FileInfo> files)
    {
        var nonEcmaModules = new List<FileInfo>();
        var ecmaAssemblies = new List<FileInfo>();
        var nonEcmaAssemblies = new List<FileInfo>();

        foreach (var file in files)
        {
            switch (ClassifyFile(file))
            {
                case FileKind.NonEcmaModule:
                    nonEcmaModules.Add(file);
                    break;

                case FileKind.EcmaAssembly:
                    ecmaAssemblies.Add(file);
                    break;

                case FileKind.NonEcmaAssembly:
                    nonEcmaAssemblies.Add(file);
                    break;
            }
        }

        return new AnalysisFileClassification
        {
            NonEcmaModules = nonEcmaModules,
            EcmaAssemblies = ecmaAssemblies,
            NonEcmaAssemblies = nonEcmaAssemblies
        };
    }

    private FileKind ClassifyFile(FileInfo file)
    {
        try
        {
            var peReader = peReaderCache.GetOrAdd(file);

            if (!HasEcmaMetadata(peReader))
            {
                return FileKind.NonEcmaModule;
            }

            return IsAssembly(peReader)
                ? FileKind.EcmaAssembly
                : FileKind.NonEcmaAssembly;
        }
        catch (BadImageFormatException)
        {
            return FileKind.NonEcmaModule;
        }
    }

    private static bool HasEcmaMetadata(PEReader peReader)
    {
        return peReader.HasMetadata && peReader.PEHeaders is not null;
    }

    private static bool IsAssembly(PEReader peReader)
    {
        return peReader
            .GetMetadataReader()
            .IsAssembly;
    }

    private enum FileKind
    {
        NonEcmaModule,
        EcmaAssembly,
        NonEcmaAssembly
    }
}
