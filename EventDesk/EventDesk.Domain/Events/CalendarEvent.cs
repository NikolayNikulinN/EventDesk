using EventDesk.Constants;
using EventDesk.Domain.Reminders;

namespace EventDesk.Domain.Events;

/// <summary>An event with UTC instants and the time zone used to enter and display them.</summary>
public sealed class CalendarEvent
{
    private readonly List<ReminderRule> reminders = [];

    private CalendarEvent(Guid id, string title, DateTimeOffset startsAtUtc, DateTimeOffset endsAtUtc,
        string timeZoneId, string? description, string? colorHex)
    {
        Id = id;
        Title = title;
        StartsAtUtc = startsAtUtc;
        EndsAtUtc = endsAtUtc;
        TimeZoneId = timeZoneId;
        Description = description;
        ColorHex = colorHex;
        RecurrenceKind = EventRecurrenceKind.None;
        IsEnabled = true;
    }

    public Guid Id { get; }

    public string Title { get; private set; }

    public string? Description { get; private set; }

    public string? ColorHex { get; private set; }

    public EventRecurrenceKind RecurrenceKind { get; private set; }

    public int? RecurrenceMonth { get; private set; }

    public int? RecurrenceDay { get; private set; }

    public int? NotificationIntervalMinutes { get; private set; }

    public DateTimeOffset StartsAtUtc { get; private set; }

    public DateTimeOffset EndsAtUtc { get; private set; }

    public string TimeZoneId { get; private set; }

    public bool IsEnabled { get; private set; }

    public bool IsDeleted { get; private set; }

    public IReadOnlyList<ReminderRule> Reminders => reminders.AsReadOnly();

    /// <summary>Accepts local wall times with explicit offsets valid for <paramref name="timeZoneId"/>.</summary>
    public static CalendarEvent Create(
        Guid id,
        string title,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        string timeZoneId,
        string? description = null,
        string? colorHex = null,
        EventRecurrenceKind recurrenceKind = EventRecurrenceKind.None,
        int? recurrenceMonth = null,
        int? recurrenceDay = null,
        int? notificationIntervalMinutes = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(DomainValidationMessages.EventIdRequired, nameof(id));
        }

        string validatedTitle = ValidateTitle(title);
        (DateTimeOffset startUtc, DateTimeOffset endUtc, string zoneId) = ValidateSchedule(startsAt, endsAt, timeZoneId);

        CalendarEvent calendarEvent = new(id, validatedTitle, startUtc, endUtc, zoneId,
            ValidateDescription(description), ValidateColorHex(colorHex));
        calendarEvent.ConfigureRecurrence(recurrenceKind, recurrenceMonth, recurrenceDay, notificationIntervalMinutes);
        return calendarEvent;
    }

    public void UpdateTitle(string title)
    {
        EnsureNotDeleted();
        Title = ValidateTitle(title);
    }

    public void UpdateDetails(string? description, string? colorHex)
    {
        EnsureNotDeleted();
        string? validatedDescription = ValidateDescription(description);
        string? validatedColorHex = ValidateColorHex(colorHex);
        Description = validatedDescription;
        ColorHex = validatedColorHex;
    }

    public void ConfigureRecurrence(EventRecurrenceKind kind, int? month, int? day, int? notificationIntervalMinutes)
    {
        EnsureNotDeleted();
        ValidateRecurrence(kind, month, day, notificationIntervalMinutes, StartsAtUtc, EndsAtUtc, TimeZoneId);
        if (kind != EventRecurrenceKind.None && reminders.Count != 0)
        {
            throw new InvalidOperationException(DomainValidationMessages.EventRecurrenceInvalid);
        }

        RecurrenceKind = kind;
        RecurrenceMonth = month;
        RecurrenceDay = day;
        NotificationIntervalMinutes = notificationIntervalMinutes;
    }

    private static void ValidateRecurrence(EventRecurrenceKind kind, int? month, int? day,
        int? notificationIntervalMinutes, DateTimeOffset startsAtUtc, DateTimeOffset endsAtUtc, string timeZoneId)
    {
        if (kind == EventRecurrenceKind.None)
        {
            if (month is not null || day is not null || notificationIntervalMinutes is not null)
            {
                throw new ArgumentException(DomainValidationMessages.EventRecurrenceInvalid, nameof(kind));
            }
        }
        else
        {
            if (kind is not (EventRecurrenceKind.Yearly or EventRecurrenceKind.Monthly) ||
                day is < 1 or > 31 or null ||
                (kind == EventRecurrenceKind.Yearly &&
                 (month is < 1 or > 12 or null || day > DateTime.DaysInMonth(2024, month.Value))) ||
                (kind == EventRecurrenceKind.Monthly && month is not null))
            {
                throw new ArgumentException(DomainValidationMessages.EventRecurrenceInvalid, nameof(kind));
            }

            ValidateNotificationWindow(notificationIntervalMinutes, startsAtUtc, endsAtUtc, timeZoneId);
        }
    }

    public bool OccursOn(DateOnly date) => RecurrenceKind switch
    {
        EventRecurrenceKind.Yearly => date.Month == RecurrenceMonth && date.Day == RecurrenceDay,
        EventRecurrenceKind.Monthly => date.Day == RecurrenceDay,
        _ => false
    };

    public IReadOnlyList<DateTimeOffset> GetRecurringNotificationTimes(DateOnly date)
    {
        if (!IsEnabled || IsDeleted)
        {
            return [];
        }

        return GetRecurringOccurrenceTimes(date);
    }

    public IReadOnlyList<DateTimeOffset> GetRecurringOccurrenceTimes(DateOnly date)
    {
        if (IsDeleted || !OccursOn(date) || NotificationIntervalMinutes is not { } interval)
        {
            return [];
        }

        TimeZoneInfo zone = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
        TimeSpan first = TimeZoneInfo.ConvertTime(StartsAtUtc, zone).TimeOfDay;
        TimeSpan last = TimeZoneInfo.ConvertTime(EndsAtUtc, zone).TimeOfDay;
        List<DateTimeOffset> times = [];
        for (TimeSpan time = first; time <= last; time += TimeSpan.FromMinutes(interval))
        {
            DateTime local = date.ToDateTime(TimeOnly.MinValue).Add(time);
            if (zone.IsInvalidTime(local))
            {
                continue;
            }

            TimeSpan offset = zone.IsAmbiguousTime(local)
                ? zone.GetAmbiguousTimeOffsets(local).Max()
                : zone.GetUtcOffset(local);
            times.Add(new DateTimeOffset(local, offset).ToUniversalTime());
        }

        return times;
    }

    public void Reschedule(DateTimeOffset startsAt, DateTimeOffset endsAt, string timeZoneId)
        => UpdateScheduleAndRecurrence(startsAt, endsAt, timeZoneId,
            RecurrenceKind, RecurrenceMonth, RecurrenceDay, NotificationIntervalMinutes);

    public void UpdateScheduleAndRecurrence(DateTimeOffset startsAt, DateTimeOffset endsAt, string timeZoneId,
        EventRecurrenceKind kind, int? month, int? day, int? notificationIntervalMinutes)
    {
        EnsureNotDeleted();
        (DateTimeOffset startUtc, DateTimeOffset endUtc, string zoneId) = ValidateSchedule(startsAt, endsAt, timeZoneId);
        foreach (ReminderRule reminder in reminders)
        {
            ValidateReminderDate(startUtc, reminder);
        }

        ValidateRecurrence(kind, month, day, notificationIntervalMinutes, startUtc, endUtc, zoneId);
        if (kind != EventRecurrenceKind.None && reminders.Count != 0)
        {
            throw new InvalidOperationException(DomainValidationMessages.EventRecurrenceInvalid);
        }

        StartsAtUtc = startUtc;
        EndsAtUtc = endUtc;
        TimeZoneId = zoneId;
        RecurrenceKind = kind;
        RecurrenceMonth = month;
        RecurrenceDay = day;
        NotificationIntervalMinutes = notificationIntervalMinutes;
    }

    public void AddReminder(ReminderRule reminder)
    {
        EnsureNotDeleted();
        ArgumentNullException.ThrowIfNull(reminder);
        if (RecurrenceKind != EventRecurrenceKind.None)
        {
            throw new InvalidOperationException(DomainValidationMessages.EventRecurrenceInvalid);
        }

        ValidateReminderDate(StartsAtUtc, reminder);

        if (reminders.Contains(reminder))
        {
            throw new InvalidOperationException(DomainValidationMessages.DuplicateReminder);
        }

        reminders.Add(reminder);
    }

    public bool RemoveReminder(ReminderRule reminder)
    {
        EnsureNotDeleted();
        ArgumentNullException.ThrowIfNull(reminder);
        return reminders.Remove(reminder);
    }

    public void Enable()
    {
        EnsureNotDeleted();
        IsEnabled = true;
    }

    public void Disable()
    {
        EnsureNotDeleted();
        IsEnabled = false;
    }

    /// <summary>Soft-deletes the event while preserving its rules for future history references.</summary>
    public void Delete()
    {
        IsDeleted = true;
        IsEnabled = false;
    }

    public IReadOnlyList<ReminderOccurrence> GetReminderSchedule()
    {
        if (!IsEnabled || IsDeleted || RecurrenceKind != EventRecurrenceKind.None)
        {
            return [];
        }

        return reminders
            .SelectMany(reminder => reminder.GetTriggerTimes(StartsAtUtc)
                .Select(triggerAtUtc => new ReminderOccurrence(reminder.Channel, triggerAtUtc)))
            .OrderBy(occurrence => occurrence.TriggerAtUtc)
            .ThenBy(occurrence => occurrence.Channel)
            .ToArray();
    }

    private static string ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException(DomainValidationMessages.EventTitleRequired, nameof(title));
        }

        return title.Trim();
    }

    private static string? ValidateDescription(string? description)
    {
        string? trimmed = description?.Trim();
        if (trimmed?.Length > 2000)
        {
            throw new ArgumentException(DomainValidationMessages.EventDescriptionTooLong, nameof(description));
        }

        return trimmed;
    }

    private static string? ValidateColorHex(string? colorHex)
    {
        if (colorHex is not null &&
            (colorHex.Length != 7 || colorHex[0] != '#' || !colorHex.AsSpan(1).ToArray().All(Uri.IsHexDigit)))
        {
            throw new ArgumentException(DomainValidationMessages.EventColorInvalid, nameof(colorHex));
        }

        return colorHex?.ToUpperInvariant();
    }

    private static void ValidateNotificationWindow(
        int? interval, DateTimeOffset startsAtUtc, DateTimeOffset endsAtUtc, string timeZoneId)
    {
        TimeZoneInfo zone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        DateTimeOffset localStart = TimeZoneInfo.ConvertTime(startsAtUtc, zone);
        DateTimeOffset localEnd = TimeZoneInfo.ConvertTime(endsAtUtc, zone);
        TimeSpan duration = localEnd.DateTime - localStart.DateTime;
        if (interval is < 1 or > 1439 or null ||
            localStart.Date != localEnd.Date || duration <= TimeSpan.Zero ||
            duration.Ticks % TimeSpan.FromMinutes(interval.Value).Ticks != 0)
        {
            throw new ArgumentException(DomainValidationMessages.EventNotificationWindowInvalid, nameof(interval));
        }
    }

    private static (DateTimeOffset StartUtc, DateTimeOffset EndUtc, string TimeZoneId) ValidateSchedule(
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            throw new ArgumentException(DomainValidationMessages.TimeZoneRequired, nameof(timeZoneId));
        }

        string trimmedZoneId = timeZoneId.Trim();
        TimeZoneInfo timeZone;

        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(trimmedZoneId);
        }
        catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            throw new ArgumentException(DomainValidationMessages.TimeZoneNotFound, nameof(timeZoneId), exception);
        }

        ValidateOffset(startsAt, timeZone, nameof(startsAt));
        ValidateOffset(endsAt, timeZone, nameof(endsAt));

        if (endsAt <= startsAt)
        {
            throw new ArgumentException(DomainValidationMessages.EventEndMustFollowStart, nameof(endsAt));
        }

        return (startsAt.ToUniversalTime(), endsAt.ToUniversalTime(), trimmedZoneId);
    }

    private static void ValidateOffset(DateTimeOffset value, TimeZoneInfo timeZone, string parameterName)
    {
        if (timeZone.GetUtcOffset(value.UtcDateTime) != value.Offset)
        {
            throw new ArgumentException(DomainValidationMessages.EventTimeOffsetMismatch, parameterName);
        }
    }

    private static void ValidateReminderDate(DateTimeOffset startUtc, ReminderRule reminder)
    {
        if (startUtc.UtcTicks < reminder.LeadTime.Ticks)
        {
            throw new ArgumentException(DomainValidationMessages.ReminderBeforeSupportedDate, nameof(reminder));
        }
    }

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
        {
            throw new InvalidOperationException(DomainValidationMessages.DeletedEventCannotChange);
        }
    }
}
