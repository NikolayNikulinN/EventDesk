using System.Globalization;
using EventDesk.Resources.Abstract;
using EventDesk.Resources.Resources;

namespace EventDesk.Resources.Wrappers;

/// <summary>Provides localized application resources to bindings and services.</summary>
public sealed class ResourceWrapper : IResourceWrapper
{
    private const string ImagePath = "/EventDesk.Resources;component/Images/";

    public string AppTitle => GetString(nameof(AppTitle));

    public string MinimizeToolTip => GetString(nameof(MinimizeToolTip));

    public string MaximizeToolTip => GetString(nameof(MaximizeToolTip));

    public string RestoreToolTip => GetString(nameof(RestoreToolTip));

    public string CloseToolTip => GetString(nameof(CloseToolTip));

    public string Calendar => GetString(nameof(Calendar));

    public string RefreshEvents => GetString(nameof(RefreshEvents));

    public string LoadingEvents => GetString(nameof(LoadingEvents));

    public string NoEventsThisWeek => GetString(nameof(NoEventsThisWeek));

    public string ServiceUnavailable => GetString(nameof(ServiceUnavailable));

    public string ServiceRequestTimedOut => GetString(nameof(ServiceRequestTimedOut));

    public string CalendarLoadFailed => GetString(nameof(CalendarLoadFailed));

    public string History => GetString(nameof(History));

    public string Settings => GetString(nameof(Settings));

    public string About => GetString(nameof(About));

    public string AppIconPath => $"{ImagePath}appIcon.png";

    public string MinimizeIconPath => $"{ImagePath}minimize.png";

    public string MaximizeIconPath => $"{ImagePath}maximize.png";

    public string RestoreIconPath => $"{ImagePath}restore.png";

    public string CloseIconPath => $"{ImagePath}close.png";

    private static string GetString(string key) => AppResource.ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? key;
}
