namespace EventDesk.Contracts.Reminders;

public sealed record ReminderHistoryResponse(IReadOnlyList<ReminderHistoryEntry> Deliveries);

public sealed record ReminderHistoryEntry(Guid DeliveryId, string EventTitle,
    ReminderHistoryChannelCode Channel, DateTimeOffset DueAtUtc,
    ReminderHistoryStatusCode Status, int AttemptCount,
    DateTimeOffset? DeliveredAtUtc, string? LastErrorCode);

public enum ReminderHistoryChannelCode { Desktop = 1, Email = 2 }
public enum ReminderHistoryStatusCode
{
    Pending = 1, Delivering = 2, Delivered = 3, Failed = 4, Canceled = 5
}
