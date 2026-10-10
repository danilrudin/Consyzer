namespace Consyzer.Tests.TestSupport;

internal sealed class LinuxTheoryAttribute : TheoryAttribute
{
    public LinuxTheoryAttribute()
    {
        if (!OperatingSystem.IsLinux())
        {
            Skip = "Requires Linux.";
        }
    }
}
