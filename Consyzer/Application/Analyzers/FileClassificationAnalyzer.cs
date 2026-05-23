using Consyzer.Core.Classifiers;
using Consyzer.Core.Models.Analysis;

namespace Consyzer.Application.Analyzers;

internal sealed class FileClassificationAnalyzer(
    IFileClassifier<AnalysisFileClassification> fileClassifier
) : IAnalyzer<IEnumerable<FileInfo>, AnalysisFileClassification>
{
    public AnalysisFileClassification Analyze(IEnumerable<FileInfo> files)
    {
        return fileClassifier.Classify(files);
    }
}
