using EventDesk.Contracts.Events;

namespace EventDesk.App.Services;

public interface ICalendarEventClient
{
    Task<IReadOnlyList<CalendarEventResponse>> GetByPeriodAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default);
}
