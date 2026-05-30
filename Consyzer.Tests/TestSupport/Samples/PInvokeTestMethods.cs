using System.Runtime.InteropServices;

namespace Consyzer.Tests.TestSupport.Samples;

// Exists only to embed deterministic P/Invoke metadata into the test assembly.
// The method is never called.
internal static class PInvokeTestMethods
{
    private const string NativeLibraryName = "consyzer-test-native-library";
    private const string NativeEntryPointName = "consyzer_test_method";

    [DllImport(NativeLibraryName, EntryPoint = NativeEntryPointName)]
    public static extern int Invoke(int _);
}
