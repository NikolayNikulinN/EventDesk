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

    public Task<CalendarEventResponse> CreateAsync(SaveEventDto calendarEvent, CancellationToken cancellationToken = default) =>
        ipcClient.SendAsync<CreateEventRequest, CalendarEventResponse>(
            IpcOperation.CreateEvent, new CreateEventRequest(calendarEvent), cancellationToken);

    public Task<CalendarEventResponse> UpdateAsync(Guid id, SaveEventDto calendarEvent, CancellationToken cancellationToken = default) =>
        ipcClient.SendAsync<UpdateEventRequest, CalendarEventResponse>(
            IpcOperation.UpdateEvent, new UpdateEventRequest(id, calendarEvent), cancellationToken);

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        await ipcClient.SendAsync<DeleteEventRequest, DeleteEventResponse>(
            IpcOperation.DeleteEvent, new DeleteEventRequest(id), cancellationToken);
}
