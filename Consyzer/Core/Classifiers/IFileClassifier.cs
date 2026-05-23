namespace Consyzer.Core.Classifiers;

internal interface IFileClassifier<out TOut>
{
    TOut Classify(IEnumerable<FileInfo> files);
}
