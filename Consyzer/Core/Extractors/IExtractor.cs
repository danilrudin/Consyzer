namespace Consyzer.Core.Extractors;

internal interface IExtractor<in TIn, out TOut>
{
    TOut Extract(TIn input);
}
