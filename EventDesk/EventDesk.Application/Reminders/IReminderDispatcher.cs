namespace EventDesk.Application.Reminders;

public interface IReminderDispatcher
{
    Task DispatchAsync(ReminderClaim claim, CancellationToken cancellationToken);
}
