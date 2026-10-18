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

    public string PreviousWeek => GetString(nameof(PreviousWeek));
    public string NextWeek => GetString(nameof(NextWeek));
    public string Today => GetString(nameof(Today));
    public string NewEvent => GetString(nameof(NewEvent));
    public string EditEvent => GetString(nameof(EditEvent));
    public string EventTitle => GetString(nameof(EventTitle));
    public string EventDescription => GetString(nameof(EventDescription));
    public string EventColor => GetString(nameof(EventColor));
    public string EventRepeat => GetString(nameof(EventRepeat));
    public string EventRepeatOnce => GetString(nameof(EventRepeatOnce));
    public string EventRepeatYearly => GetString(nameof(EventRepeatYearly));
    public string EventRepeatMonthly => GetString(nameof(EventRepeatMonthly));
    public string MonthAndDay => GetString(nameof(MonthAndDay));
    public string DayOfMonth => GetString(nameof(DayOfMonth));
    public string FirstNotification => GetString(nameof(FirstNotification));
    public string LastNotification => GetString(nameof(LastNotification));
    public string NotificationInterval => GetString(nameof(NotificationInterval));
    public string NotificationTimes => GetString(nameof(NotificationTimes));
    public string EveryThirtyMinutes => GetString(nameof(EveryThirtyMinutes));
    public string EveryHour => GetString(nameof(EveryHour));
    public string EveryTwoHours => GetString(nameof(EveryTwoHours));
    public string EveryThreeHours => GetString(nameof(EveryThreeHours));
    public string EveryFourHours => GetString(nameof(EveryFourHours));
    public string EverySixHours => GetString(nameof(EverySixHours));
    public string NotificationWindowInvalid => GetString(nameof(NotificationWindowInvalid));
    public string MissingDatesSkipped => GetString(nameof(MissingDatesSkipped));
    public string ColorBlue => GetString(nameof(ColorBlue));
    public string ColorGreen => GetString(nameof(ColorGreen));
    public string ColorOrange => GetString(nameof(ColorOrange));
    public string ColorPurple => GetString(nameof(ColorPurple));
    public string ColorRed => GetString(nameof(ColorRed));
    public string StartDate => GetString(nameof(StartDate));
    public string StartTime => GetString(nameof(StartTime));
    public string EndDate => GetString(nameof(EndDate));
    public string EndTime => GetString(nameof(EndTime));
    public string Reminder => GetString(nameof(Reminder));
    public string NoReminder => GetString(nameof(NoReminder));
    public string ReminderFiveMinutes => GetString(nameof(ReminderFiveMinutes));
    public string ReminderFifteenMinutes => GetString(nameof(ReminderFifteenMinutes));
    public string ReminderThirtyMinutes => GetString(nameof(ReminderThirtyMinutes));
    public string ReminderOneHour => GetString(nameof(ReminderOneHour));
    public string EventEnabled => GetString(nameof(EventEnabled));
    public string SaveEvent => GetString(nameof(SaveEvent));
    public string SaveAnyway => GetString(nameof(SaveAnyway));
    public string EventOverlapWarningFormat => GetString(nameof(EventOverlapWarningFormat));
    public string Cancel => GetString(nameof(Cancel));
    public string DeleteEvent => GetString(nameof(DeleteEvent));
    public string ConfirmDeleteEvent => GetString(nameof(ConfirmDeleteEvent));
    public string EventTitleRequired => GetString(nameof(EventTitleRequired));
    public string EventDescriptionRequired => GetString(nameof(EventDescriptionRequired));
    public string EventDateTimeRequired => GetString(nameof(EventDateTimeRequired));
    public string EventTimeInvalid => GetString(nameof(EventTimeInvalid));
    public string EventEndAfterStart => GetString(nameof(EventEndAfterStart));
    public string EventSaveFailed => GetString(nameof(EventSaveFailed));
    public string EventDeleteFailed => GetString(nameof(EventDeleteFailed));
    public string InvalidEventData => GetString(nameof(InvalidEventData));
    public string EventNoLongerExists => GetString(nameof(EventNoLongerExists));
    public string WeekRangeFormat => GetString(nameof(WeekRangeFormat));
    public string Time24HourFormat => GetString(nameof(Time24HourFormat));
    public string EventTimeRangeFormat => GetString(nameof(EventTimeRangeFormat));
    public string ApplyingEventChanges => GetString(nameof(ApplyingEventChanges));

    public string History => GetString(nameof(History));

    public string Settings => GetString(nameof(Settings));

    public string About => GetString(nameof(About));

    public string AppIconPath => $"{ImagePath}appIcon.png";

    public string MenuCalendarIconPath => $"{ImagePath}menuCalendar.png";

    public string MenuHistoryIconPath => $"{ImagePath}menuHistory.png";

    public string MenuSettingsIconPath => $"{ImagePath}menuSettings.png";

    public string MenuAboutIconPath => $"{ImagePath}menuAbout.png";

    public string MinimizeIconPath => $"{ImagePath}minimize.png";

    public string MaximizeIconPath => $"{ImagePath}maximize.png";

    public string RestoreIconPath => $"{ImagePath}restore.png";

    public string CloseIconPath => $"{ImagePath}close.png";

    public string CancelIconPath => $"{ImagePath}cancel.png";

    public string AddEventIconPath => $"{ImagePath}addEvent.png";

    public string NextWeekIconPath => $"{ImagePath}nextWeek.png";

    public string PreviousWeekIconPath => $"{ImagePath}previousWeek.png";

    public string TodayIconPath => $"{ImagePath}today.png";

    public string UpdateEventIconPath => $"{ImagePath}updateEvent.png";

    private static string GetString(string key) => AppResource.ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? key;
}
