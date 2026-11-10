using System.Runtime.CompilerServices;

namespace Consyzer.Tests.TestSupport;

internal sealed class LinuxFactAttribute : FactAttribute
{
    public LinuxFactAttribute(
        [CallerFilePath] string? sourceFilePath = null,
        [CallerLineNumber] int sourceLineNumber = -1
    ) : base(sourceFilePath, sourceLineNumber)
    {
        if (!OperatingSystem.IsLinux())
        {
            Skip = "Requires Linux.";
        }
    }
}
