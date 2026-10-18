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
        IsEnabled = true;
    }

    public Guid Id { get; }

    public string Title { get; private set; }

    public string? Description { get; private set; }

    public string? ColorHex { get; private set; }

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
        string? colorHex = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(DomainValidationMessages.EventIdRequired, nameof(id));
        }

        string validatedTitle = ValidateTitle(title);
        (DateTimeOffset startUtc, DateTimeOffset endUtc, string zoneId) = ValidateSchedule(startsAt, endsAt, timeZoneId);

        return new CalendarEvent(id, validatedTitle, startUtc, endUtc, zoneId,
            ValidateDescription(description), ValidateColorHex(colorHex));
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

    public void Reschedule(DateTimeOffset startsAt, DateTimeOffset endsAt, string timeZoneId)
    {
        EnsureNotDeleted();
        (DateTimeOffset startUtc, DateTimeOffset endUtc, string zoneId) = ValidateSchedule(startsAt, endsAt, timeZoneId);
        foreach (ReminderRule reminder in reminders)
        {
            ValidateReminderDate(startUtc, reminder);
        }

        StartsAtUtc = startUtc;
        EndsAtUtc = endUtc;
        TimeZoneId = zoneId;
    }

    public void AddReminder(ReminderRule reminder)
    {
        EnsureNotDeleted();
        ArgumentNullException.ThrowIfNull(reminder);
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
        if (!IsEnabled || IsDeleted)
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
