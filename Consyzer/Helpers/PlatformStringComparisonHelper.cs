namespace Consyzer.Helpers;

internal static class PlatformStringComparisonHelper
{
    public static readonly StringComparer FilePathComparer =
        OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
}
