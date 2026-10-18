using EventDesk.Constants;
using EventDesk.Contracts.Ipc;
using EventDesk.Contracts.Reminders;
using EventDesk.Ipc.Abstractions;

namespace EventDesk.App.Services;

public sealed class ReminderHistoryIpcClient(IIpcClient client) : IReminderHistoryClient
{
    public async Task<IReadOnlyList<ReminderHistoryEntry>> GetAsync(CancellationToken cancellationToken)
    {
        ReminderHistoryResponse response = await client.SendAsync<GetReminderHistoryRequest, ReminderHistoryResponse>(
            IpcOperation.GetReminderHistory,
            new GetReminderHistoryRequest(ReminderProcessingConstants.HistoryPageSize), cancellationToken);
        return response.Deliveries;
    }
}
