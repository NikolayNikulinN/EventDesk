using EventDesk.Domain.Reminders;

namespace EventDesk.Application.Reminders;

public sealed record ReminderHistoryItem(
    Guid DeliveryId, string EventTitle, ReminderChannel Channel,
    DateTimeOffset DueAtUtc, ReminderDeliveryStatus Status, int AttemptCount,
    DateTimeOffset? DeliveredAtUtc, string? LastErrorCode);
