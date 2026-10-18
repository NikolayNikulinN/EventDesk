using EventDesk.Domain.Reminders;

namespace EventDesk.Infrastructure.Persistence;

public sealed class ReminderDeliveryRecord
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public ReminderChannel Channel { get; set; }
    public DateTimeOffset DueAtUtc { get; set; }
    public DateTimeOffset NextAttemptAtUtc { get; set; }
    public ReminderDeliveryStatus Status { get; set; }
    public int AttemptCount { get; set; }
    public Guid? LeaseToken { get; set; }
    public DateTimeOffset? LeaseUntilUtc { get; set; }
    public DateTimeOffset? DeliveredAtUtc { get; set; }
    public string? LastErrorCode { get; set; }
}
