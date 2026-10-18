using EventDesk.Domain.Events;

namespace EventDesk.Application.Events.Models;

public sealed record CalendarEventModel(
    Guid Id,
    string Title,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    string TimeZoneId,
    bool IsEnabled,
    IReadOnlyList<ReminderRuleModel> Reminders,
    string? Description = null,
    string? ColorHex = null,
    EventRecurrenceKind Recurrence = EventRecurrenceKind.None,
    int? RecurrenceMonth = null,
    int? RecurrenceDay = null,
    int? NotificationIntervalMinutes = null);
