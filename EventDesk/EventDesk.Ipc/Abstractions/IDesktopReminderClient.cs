using EventDesk.Contracts.Reminders;

namespace EventDesk.Ipc.Abstractions;

public interface IDesktopReminderClient
{
    Task SendAsync(DesktopReminderMessage message, CancellationToken cancellationToken);
}
