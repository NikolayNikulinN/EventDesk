namespace EventDesk.Application.Events.Models;

public sealed record SaveCalendarEventModel(
    string Title,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string TimeZoneId,
    bool IsEnabled,
    IReadOnlyList<ReminderRuleModel> Reminders,
    string? Description = null,
    string? ColorHex = null);
