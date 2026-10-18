using EventDesk.Domain.Reminders;

namespace EventDesk.Infrastructure.Persistence;

public sealed class ReminderAttemptRecord
{
    public Guid Id { get; set; }
    public Guid DeliveryId { get; set; }
    public int Number { get; set; }
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset? FinishedAtUtc { get; set; }
    public ReminderAttemptStatus Status { get; set; }
    public string? ErrorCode { get; set; }
}
