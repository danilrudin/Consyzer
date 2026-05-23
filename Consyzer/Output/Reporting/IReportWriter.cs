using Consyzer.Core.Models.Analysis;

namespace Consyzer.Output.Reporting;

internal interface IReportWriter
{
    string Write(AnalysisOutcome outcome);
}
