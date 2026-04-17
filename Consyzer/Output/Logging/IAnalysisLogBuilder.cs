using Consyzer.Options;
using Consyzer.Core.Models;

namespace Consyzer.Output.Logging;

internal interface IAnalysisLogBuilder
{
    string BuildAnalysisOptionsLog(CommandLineOptions options);
    string BuildFoundFilesLog(IEnumerable<FileInfo> files);
    string BuildFileClassificationLog(AnalysisFileClassification fileClassification);

}