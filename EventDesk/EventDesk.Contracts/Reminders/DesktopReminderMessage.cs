namespace EventDesk.Contracts.Reminders;

public sealed record DesktopReminderMessage(
    Guid DeliveryId, Guid EventId, string EventTitle, string? EventDescription);
