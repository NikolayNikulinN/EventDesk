namespace EventDesk.Domain.Reminders;

public readonly record struct ReminderOccurrence(ReminderChannel Channel, DateTimeOffset TriggerAtUtc);
