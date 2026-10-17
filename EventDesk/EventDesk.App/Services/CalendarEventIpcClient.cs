using EventDesk.Contracts.Events;
using EventDesk.Contracts.Ipc;
using EventDesk.Ipc.Abstractions;

namespace EventDesk.App.Services;

public sealed class CalendarEventIpcClient(IIpcClient ipcClient) : ICalendarEventClient
{
    public async Task<IReadOnlyList<CalendarEventResponse>> GetByPeriodAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
    {
        GetEventsResponse response = await ipcClient.SendAsync<GetEventsRequest, GetEventsResponse>(
            IpcOperation.GetEvents,
            new GetEventsRequest(from, to),
            cancellationToken);
        return response.Events;
    }
}
