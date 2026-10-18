using EventDesk.Application.Reminders;
using EventDesk.Constants;
using EventDesk.Contracts.Reminders;
using EventDesk.Domain.Reminders;
using EventDesk.Ipc.Abstractions;

namespace EventDesk.Infrastructure.Notifications;

public sealed class ReminderDispatcher(IDesktopReminderClient client) : IReminderDispatcher
{
    public Task DispatchAsync(ReminderClaim claim, CancellationToken cancellationToken) =>
        claim.Channel switch
        {
            ReminderChannel.Desktop => client.SendAsync(new DesktopReminderMessage(
                claim.DeliveryId, claim.EventId, claim.EventTitle), cancellationToken),
            ReminderChannel.Email => throw new NotSupportedException(ReminderProcessingConstants.EmailUnavailable),
            _ => throw new NotSupportedException(ReminderProcessingConstants.EmailUnavailable)
        };
}
