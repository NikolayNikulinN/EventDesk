namespace EventDesk.Contracts.Events;

public sealed record CalendarEventResponse(
    Guid Id,
    string Title,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    string TimeZoneId,
    bool IsEnabled,
    IReadOnlyList<ReminderRuleDto> Reminders,
    string? Description = null,
    string? ColorHex = null);
