using EventDesk.Application.Reminders;

namespace EventDesk.Application.Email;

public interface IEmailSender
{
    Task SendReminderAsync(ReminderClaim claim, CancellationToken cancellationToken);
}
