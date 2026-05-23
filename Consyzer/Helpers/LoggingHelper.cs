using NLog;
using NLog.Targets;

namespace Consyzer.Helpers;

internal static class LoggingHelper
{
    public static string? GetLogFileName()
    {
        var config = LogManager.Configuration;
        if (config is null) return null;

        var fileTarget = config.AllTargets
            .OfType<FileTarget>()
            .FirstOrDefault();

        if (fileTarget is null) return null;

        var logEventInfo = new LogEventInfo { TimeStamp = DateTime.UtcNow };
        return fileTarget.FileName.Render(logEventInfo);
    }
}
