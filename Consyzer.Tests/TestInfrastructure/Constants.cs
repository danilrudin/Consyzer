using System.Reflection;
using System.Runtime.InteropServices;

namespace Consyzer.Tests.TestInfrastructure;

internal static class Constants
{
    public static FileInfo NonEcmaAssembly => new(Path.Combine(AppContext.BaseDirectory, "Consyzer.exe"));
    public static FileInfo EcmaAssemblyWithPInvoke => new(Assembly.GetExecutingAssembly().Location);
    public static FileInfo EcmaAssemblyWithoutPInvoke => new(Path.Combine(AppContext.BaseDirectory, "Consyzer.dll"));
}
