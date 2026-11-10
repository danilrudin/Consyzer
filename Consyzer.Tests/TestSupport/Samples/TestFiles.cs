namespace Consyzer.Tests.TestSupport.Samples;

internal static class TestFiles
{
    private const string ProductAssemblyFileName = "Consyzer.dll";
    private const string TestDataDirectoryName = "TestData";
    private const string NonEcmaModuleFileName = "non-ecma-module.bin";
    private const string NonEcmaModuleContent = "This file intentionally is not a .NET assembly.";

    private static readonly Lazy<FileInfo> NonEcmaModuleFile = new(
        () => GetOrCreateFile(NonEcmaModuleFileName, NonEcmaModuleContent)
    );

    public static FileInfo AssemblyWithPInvoke =>
        new(typeof(PInvokeTestMethods).Assembly.Location);

    public static FileInfo AssemblyWithoutPInvoke =>
        new(Path.Combine(AppContext.BaseDirectory, ProductAssemblyFileName));

    public static FileInfo NonEcmaModule => NonEcmaModuleFile.Value;

    private static FileInfo GetOrCreateFile(string name, string content)
    {
        var directory = Path.Combine(AppContext.BaseDirectory, TestDataDirectoryName);
        Directory.CreateDirectory(directory);

        var path = Path.Combine(directory, name);
        if (!File.Exists(path))
        {
            File.WriteAllText(path, content);
        }

        return new FileInfo(path);
    }
}
