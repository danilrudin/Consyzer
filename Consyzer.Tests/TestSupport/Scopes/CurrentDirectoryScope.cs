namespace Consyzer.Tests.TestSupport.Scopes;

internal sealed class CurrentDirectoryScope : IDisposable
{
    private readonly string _originalDirectory = Directory.GetCurrentDirectory();

    public CurrentDirectoryScope(string directory)
    {
        Directory.SetCurrentDirectory(directory);
    }

    public void Dispose()
    {
        Directory.SetCurrentDirectory(_originalDirectory);
    }
}
