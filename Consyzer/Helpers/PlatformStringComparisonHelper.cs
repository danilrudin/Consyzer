namespace Consyzer.Helpers;

internal static class PlatformStringComparisonHelper
{
    public static StringComparer FilePathComparer =>
        OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
}
