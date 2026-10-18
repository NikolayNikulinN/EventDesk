using EventDesk.Contracts.Reminders;

namespace EventDesk.App.Services;

public interface IReminderHistoryClient
{
    Task<IReadOnlyList<ReminderHistoryEntry>> GetAsync(CancellationToken cancellationToken);
}
