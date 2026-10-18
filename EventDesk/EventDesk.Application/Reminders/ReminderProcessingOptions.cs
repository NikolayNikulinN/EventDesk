namespace EventDesk.Application.Reminders;

public sealed class ReminderProcessingOptions
{
    public TimeSpan PollInterval { get; init; } = TimeSpan.FromSeconds(15);
    public TimeSpan LeaseDuration { get; init; } = TimeSpan.FromMinutes(2);
    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromMinutes(1);
    public int MaxAttempts { get; init; } = 5;
    public int BatchSize { get; init; } = 50;
}
