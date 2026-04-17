namespace Consyzer.Core.Caching;

internal interface IResourceCache<in TKey, out TValue> : IDisposable
{
    TValue GetOrAdd(TKey key);
}