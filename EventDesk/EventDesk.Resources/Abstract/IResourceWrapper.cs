namespace EventDesk.Resources.Abstract;

/// <summary>Provides localized application strings and shared image paths.</summary>
public interface IResourceWrapper
{
    string AppTitle { get; }

    string MinimizeToolTip { get; }

    string MaximizeToolTip { get; }

    string RestoreToolTip { get; }

    string CloseToolTip { get; }

    string Calendar { get; }

    string RefreshEvents { get; }

    string LoadingEvents { get; }

    string NoEventsThisWeek { get; }

    string ServiceUnavailable { get; }

    string ServiceRequestTimedOut { get; }

    string CalendarLoadFailed { get; }

    string PreviousWeek { get; }

    string NextWeek { get; }

    string Today { get; }

    string NewEvent { get; }

    string EditEvent { get; }

    string EventTitle { get; }

    string EventDescription { get; }

    string EventColor { get; }

    string EventRepeat { get; }

    string EventRepeatOnce { get; }

    string EventRepeatYearly { get; }

    string EventRepeatMonthly { get; }

    string MonthAndDay { get; }

    string DayOfMonth { get; }

    string FirstNotification { get; }

    string LastNotification { get; }

    string NotificationInterval { get; }

    string NotificationTimes { get; }

    string EveryThirtyMinutes { get; }

    string EveryHour { get; }

    string EveryTwoHours { get; }

    string EveryThreeHours { get; }

    string EveryFourHours { get; }

    string EverySixHours { get; }

    string NotificationWindowInvalid { get; }

    string MissingDatesSkipped { get; }

    string ColorBlue { get; }

    string ColorGreen { get; }

    string ColorOrange { get; }

    string ColorPurple { get; }

    string ColorRed { get; }

    string StartDate { get; }

    string StartTime { get; }

    string EndDate { get; }

    string EndTime { get; }

    string Reminder { get; }

    string NoReminder { get; }

    string ReminderFiveMinutes { get; }

    string ReminderFifteenMinutes { get; }

    string ReminderThirtyMinutes { get; }

    string ReminderOneHour { get; }

    string EventEnabled { get; }

    string SaveEvent { get; }

    string SaveAnyway { get; }

    string EventOverlapWarningFormat { get; }

    string Cancel { get; }

    string DeleteEvent { get; }

    string ConfirmDeleteEvent { get; }

    string EventTitleRequired { get; }

    string EventDescriptionRequired { get; }

    string EventDateTimeRequired { get; }

    string EventTimeInvalid { get; }

    string EventEndAfterStart { get; }

    string EventSaveFailed { get; }

    string EventDeleteFailed { get; }

    string InvalidEventData { get; }

    string EventNoLongerExists { get; }

    string WeekRangeFormat { get; }

    string Time24HourFormat { get; }

    string EventTimeRangeFormat { get; }

    string ApplyingEventChanges { get; }

    string History { get; }

    string Settings { get; }

    string About { get; }

    string AppIconPath { get; }

    string MenuCalendarIconPath { get; }

    string MenuHistoryIconPath { get; }

    string MenuSettingsIconPath { get; }

    string MenuAboutIconPath { get; }

    string MinimizeIconPath { get; }

    string MaximizeIconPath { get; }

    string RestoreIconPath { get; }

    string CloseIconPath { get; }

    string CancelIconPath { get; }

    string AddEventIconPath { get; }

    string NextWeekIconPath { get; }

    string PreviousWeekIconPath { get; }

    string TodayIconPath { get; }

    string UpdateEventIconPath { get; }
}
