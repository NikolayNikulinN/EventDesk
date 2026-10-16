using System.Globalization;
using EventDesk.Constants;
using Serilog;
using Serilog.Events;

namespace EventDesk.Logger.Configuration;

internal static class FileLoggerConfigurator
{
    private static readonly object SyncRoot = new();
    private static volatile bool isConfigured;

    public static ILogger GetLogger()
    {
        EnsureConfigured();
        return Log.Logger;
    }

    public static void CloseAndFlush()
    {
        lock (SyncRoot)
        {
            Log.CloseAndFlush();
            isConfigured = false;
        }
    }

    private static void EnsureConfigured()
    {
        if (isConfigured)
        {
            return;
        }

        lock (SyncRoot)
        {
            if (isConfigured)
            {
                return;
            }

            var logFilePathPattern = Path.Combine(ResolveLogDirectoryPath(), LogConstants.LogFileName);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.File(
                    logFilePathPattern,
                    restrictedToMinimumLevel: LogEventLevel.Information,
                    outputTemplate: LogConstants.OutputTemplate,
                    formatProvider: CultureInfo.InvariantCulture,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: LogConstants.RetainedFileCountLimit)
                .CreateLogger();

            isConfigured = true;
        }
    }

    private static string ResolveLogDirectoryPath()
    {
        var applicationLogDirectory = Path.Combine(AppContext.BaseDirectory, LogConstants.LogDirectoryName);

        if (CanWriteToDirectory(applicationLogDirectory))
        {
            return applicationLogDirectory;
        }

        Directory.CreateDirectory(LogConstants.LogDirectory);
        return LogConstants.LogDirectory;
    }

    private static bool CanWriteToDirectory(string directoryPath)
    {
        try
        {
            Directory.CreateDirectory(directoryPath);
            var probeFileName = string.Format(
                CultureInfo.InvariantCulture,
                LogConstants.WriteProbeFileNameFormat,
                Guid.NewGuid());
            var probeFilePath = Path.Combine(directoryPath, probeFileName);

            using (File.Create(probeFilePath))
            {
            }

            File.Delete(probeFilePath);
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
    }
}
