using EventDesk.Constants;

namespace EventDesk.Domain.Reminders;

/// <summary>A delivery channel and an elapsed-time schedule relative to an event's start.</summary>
public sealed record ReminderRule
{
    private ReminderRule(TimeSpan leadTime, ReminderChannel channel, int occurrenceCount, TimeSpan? repeatInterval)
    {
        LeadTime = leadTime;
        Channel = channel;
        OccurrenceCount = occurrenceCount;
        RepeatInterval = repeatInterval;
    }

    public TimeSpan LeadTime { get; }

    public ReminderChannel Channel { get; }

    public int OccurrenceCount { get; }

    public TimeSpan? RepeatInterval { get; }

    public static ReminderRule Create(
        TimeSpan leadTime,
        ReminderChannel channel,
        int occurrenceCount = 1,
        TimeSpan? repeatInterval = null)
    {
        if (leadTime < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(leadTime), DomainValidationMessages.ReminderLeadTimeNegative);
        }

        if (channel is not (ReminderChannel.Desktop or ReminderChannel.Email))
        {
            throw new ArgumentOutOfRangeException(nameof(channel), DomainValidationMessages.ReminderChannelInvalid);
        }

        if (occurrenceCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(occurrenceCount), DomainValidationMessages.ReminderOccurrenceCountInvalid);
        }

        if (occurrenceCount == 1 && repeatInterval is not null)
        {
            throw new ArgumentException(DomainValidationMessages.ReminderRepeatIntervalUnexpected, nameof(repeatInterval));
        }

        if (occurrenceCount > 1 && (repeatInterval is null || repeatInterval <= TimeSpan.Zero))
        {
            throw new ArgumentException(DomainValidationMessages.ReminderRepeatIntervalRequired, nameof(repeatInterval));
        }

        if (repeatInterval is not null && repeatInterval.Value.Ticks > leadTime.Ticks / (occurrenceCount - 1))
        {
            throw new ArgumentException(DomainValidationMessages.ReminderRepeatAfterEventStart, nameof(repeatInterval));
        }

        return new ReminderRule(leadTime, channel, occurrenceCount, repeatInterval);
    }

    public IEnumerable<DateTimeOffset> GetTriggerTimes(DateTimeOffset eventStart)
    {
        DateTimeOffset startUtc = eventStart.ToUniversalTime();
        if (startUtc.UtcTicks < LeadTime.Ticks)
        {
            throw new ArgumentException(DomainValidationMessages.ReminderBeforeSupportedDate, nameof(eventStart));
        }

        DateTimeOffset firstTrigger = startUtc - LeadTime;

        for (int index = 0; index < OccurrenceCount; index++)
        {
            yield return firstTrigger + TimeSpan.FromTicks((RepeatInterval?.Ticks ?? 0) * index);
        }
    }
}
