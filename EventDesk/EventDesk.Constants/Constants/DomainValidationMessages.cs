namespace EventDesk.Constants;

public static class DomainValidationMessages
{
    public const string EventIdRequired = "An event ID is required.";
    public const string EventTitleRequired = "An event title is required.";
    public const string EventDescriptionTooLong = "An event description cannot exceed 2000 characters.";
    public const string EventColorInvalid = "An event color must be a six-digit hex color.";
    public const string EventEndMustFollowStart = "The event end must be after its start.";
    public const string TimeZoneRequired = "A time zone ID is required.";
    public const string TimeZoneNotFound = "The time zone ID is not available on this system.";
    public const string EventTimeOffsetMismatch = "The supplied offset does not match the time zone at this instant.";
    public const string DeletedEventCannotChange = "A deleted event cannot be changed.";
    public const string DuplicateReminder = "This reminder rule already exists on the event.";
    public const string ReminderLeadTimeNegative = "Reminder lead time cannot be negative.";
    public const string ReminderChannelInvalid = "The reminder channel is not supported.";
    public const string ReminderOccurrenceCountInvalid = "A reminder must have at least one occurrence.";
    public const string ReminderRepeatIntervalRequired = "Repeated reminders require a positive repeat interval.";
    public const string ReminderRepeatIntervalUnexpected = "A one-time reminder cannot have a repeat interval.";
    public const string ReminderRepeatAfterEventStart = "The last reminder occurrence cannot be after the event starts.";
    public const string ReminderBeforeSupportedDate = "The reminder would occur before the supported date range.";
}
