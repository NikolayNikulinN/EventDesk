namespace EventDesk.Application.Reminders;

public interface IReminderStore
{
    Task PrepareAsync(DateTimeOffset now, CancellationToken cancellationToken);
    Task<ReminderClaim?> ClaimAsync(DateTimeOffset now, TimeSpan lease,
        int maxAttempts, CancellationToken cancellationToken);
    Task<bool> CompleteAsync(ReminderClaim claim, DateTimeOffset now,
        CancellationToken cancellationToken);
    Task<bool> FailAsync(ReminderClaim claim, DateTimeOffset now,
        DateTimeOffset nextAttemptAt, bool permanent, string errorCode,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<ReminderHistoryItem>> GetHistoryAsync(int limit,
        CancellationToken cancellationToken);
}
