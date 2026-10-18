namespace EventDesk.Contracts.Events;

public sealed record SaveEventDto(
    string Title,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string TimeZoneId,
    bool IsEnabled,
    IReadOnlyList<ReminderRuleDto> Reminders,
    string? Description = null,
    string? ColorHex = null);
