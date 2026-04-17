using System.Runtime.InteropServices;

namespace Consyzer.Tests.TestInfrastructure;

internal static class NativeMethods
{
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr GetModuleHandle(string lpModuleName);
}
