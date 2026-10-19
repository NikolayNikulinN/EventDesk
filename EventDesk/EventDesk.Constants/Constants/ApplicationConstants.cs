namespace EventDesk.Constants;

public static class ApplicationConstants
{
    public const string BackgroundArgument = "--background";

    public const string InstanceMutexPrefix = @"Local\EventDesk.App.Instance.";

    public const string ActivationEventPrefix = @"Local\EventDesk.App.Activate.";

    public const string CurrentUserSidUnavailable = "The current Windows user SID is unavailable.";

    public const string InitialCalendarNavigationFailed = "Initial calendar navigation failed.";

    public const string DesktopApplicationStarted = "EventDesk desktop application started";

    public const string DesktopApplicationFailedToStart = "EventDesk desktop application failed to start";

    public const string DesktopApplicationStopping = "EventDesk desktop application stopping";

    public const string DesktopApplicationStopFailed = "EventDesk desktop application failed to stop cleanly";
}
