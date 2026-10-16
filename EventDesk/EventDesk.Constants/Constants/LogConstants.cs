namespace EventDesk.Constants;
public static class LogConstants
{
    public const int RetainedFileCountLimit = 10;

    public const string ApplicationFolderName = "EventDesk";

    public const string LogDirectoryName = "Logs";

    public const string LogFileName = "EventDesk-.log";

    public const string WriteProbeFileNameFormat = ".write-test-{0:N}.tmp";

    public const string OutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}";

    public static string LogDirectory { get; } = Path.Combine(
                                                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                                        ApplicationFolderName,
                                                        LogDirectoryName);
}
