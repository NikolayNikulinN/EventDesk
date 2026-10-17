namespace EventDesk.Contracts.Events;

public sealed record ReminderRuleDto(
    TimeSpan LeadTime,
    ReminderChannelCode Channel,
    int OccurrenceCount,
    TimeSpan? RepeatInterval);
