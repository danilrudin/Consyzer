using Consyzer.Core.Models;
using Consyzer.Core.Classifiers;

namespace Consyzer.Analyzers;

internal sealed class FileClassificationAnalyzer(
    IFileClassifier<AnalysisFileClassification> fileClassifier
) : IAnalyzer<IEnumerable<FileInfo>, AnalysisFileClassification>
{
    public AnalysisFileClassification Analyze(IEnumerable<FileInfo> files)
    {
        return fileClassifier.Classify(files);
    }
}