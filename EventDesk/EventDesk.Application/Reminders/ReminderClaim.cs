using EventDesk.Domain.Reminders;

namespace EventDesk.Application.Reminders;

public sealed record ReminderClaim(
    Guid DeliveryId, Guid LeaseToken, Guid EventId, string EventTitle,
    ReminderChannel Channel, DateTimeOffset DueAtUtc, int AttemptNumber);
