namespace EventDesk.Resources.Abstract;

/// <summary>Provides localized application strings and shared image paths.</summary>
public interface IResourceWrapper
{
    string AppTitle { get; }
    string TrayTooltip { get; }
    string OpenApplication { get; }
    string ExitApplication { get; }
    string ReminderNotificationTitle { get; }
    string AppTrayIconPath { get; }
    string TrayOpenIconPath { get; }
    string TrayExitIconPath { get; }

    string MinimizeToolTip { get; }

    string MaximizeToolTip { get; }

    string RestoreToolTip { get; }

    string CloseToolTip { get; }

    string Calendar { get; }

    string CollapseMiniCalendar { get; }

    string ExpandMiniCalendar { get; }

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

    string EmailReminder { get; }

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
    string HistoryEmpty { get; }
    string HistoryLoading { get; }
    string HistoryLoadFailed { get; }
    string HistoryDueFormat { get; }
    string HistoryAttemptsFormat { get; }
    string HistoryChannelDesktop { get; }
    string HistoryChannelEmail { get; }
    string HistoryStatusPending { get; }
    string HistoryStatusDelivering { get; }
    string HistoryStatusDelivered { get; }
    string HistoryStatusFailed { get; }
    string HistoryStatusCanceled { get; }
    string HistoryErrorNotConfigured { get; }
    string HistoryErrorAuthentication { get; }
    string HistoryErrorConnection { get; }
    string HistoryErrorRejected { get; }
    string HistoryErrorDelivery { get; }
    string HistoryErrorDesktopUnavailable { get; }

    string Settings { get; }
    string EmailSettingsHeading { get; }
    string EmailSettingsDescription { get; }
    string EmailEnabled { get; }
    string EmailHost { get; }
    string EmailHostExample { get; }
    string EmailHostRequired { get; }
    string EmailHostInvalid { get; }
    string EmailPort { get; }
    string EmailPortExampleStartTls { get; }
    string EmailPortExampleImplicitTls { get; }
    string EmailPortInvalidStartTls { get; }
    string EmailPortInvalidImplicitTls { get; }
    string EmailSecurity { get; }
    string EmailImplicitTls { get; }
    string EmailUserName { get; }
    string EmailUserNameExample { get; }
    string EmailUserNameRequired { get; }
    string EmailUserNameInvalid { get; }
    string EmailPassword { get; }
    string EmailPasswordExample { get; }
    string EmailPasswordRequired { get; }
    string EmailPasswordReenter { get; }
    string EmailPasswordInvalid { get; }
    string EmailPasswordSaved { get; }
    string EmailPasswordMissing { get; }
    string EmailFromAddress { get; }
    string EmailFromAddressExample { get; }
    string EmailFromAddressRequired { get; }
    string EmailFromAddressInvalid { get; }
    string EmailRecipientAddress { get; }
    string EmailRecipientExample { get; }
    string EmailRecipientRequired { get; }
    string EmailRecipientInvalid { get; }
    string EmailRecipientDuplicate { get; }
    string EmailRecipientsRequired { get; }
    string EmailRecipientsLimit { get; }
    string EmailRecipientsHeading { get; }
    string AddRecipient { get; }
    string EditRecipient { get; }
    string DeleteRecipient { get; }
    string SaveRecipient { get; }
    string ConfirmDeleteRecipient { get; }
    string RecipientChangesHint { get; }
    string RecipientsEmpty { get; }
    string SaveEmailSettings { get; }
    string EmailSettingsLoading { get; }
    string EmailSettingsSaving { get; }
    string EmailSettingsSaved { get; }
    string EmailSettingsInvalid { get; }
    string EmailSettingsLoadFailed { get; }
    string EmailSettingsSaveFailed { get; }

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
