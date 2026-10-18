namespace EventDesk.Domain.Reminders;

public enum ReminderDeliveryStatus
{
    Pending = 1,
    Delivering = 2,
    Delivered = 3,
    Failed = 4,
    Canceled = 5
}
