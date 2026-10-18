using EventDesk.Contracts.Reminders;

namespace EventDesk.Ipc.Abstractions;

public interface IDesktopReminderServer
{
    Task RunAsync(Func<DesktopReminderMessage, CancellationToken, Task> handler,
        CancellationToken cancellationToken);
}
