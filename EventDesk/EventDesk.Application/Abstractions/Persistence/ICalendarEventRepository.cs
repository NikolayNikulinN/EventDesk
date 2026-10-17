using EventDesk.Domain.Events;

namespace EventDesk.Application.Abstractions.Persistence;

public interface ICalendarEventRepository
{
    Task<CalendarEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CalendarEvent>> GetOverlappingAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default);

    Task AddAsync(CalendarEvent calendarEvent, CancellationToken cancellationToken = default);
}
