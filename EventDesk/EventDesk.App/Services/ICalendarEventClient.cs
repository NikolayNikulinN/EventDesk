using EventDesk.Contracts.Events;

namespace EventDesk.App.Services;

public interface ICalendarEventClient
{
    Task<IReadOnlyList<CalendarEventResponse>> GetByPeriodAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default);

    Task<CalendarEventResponse> CreateAsync(SaveEventDto calendarEvent, CancellationToken cancellationToken = default);

    Task<CalendarEventResponse> UpdateAsync(Guid id, SaveEventDto calendarEvent, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
