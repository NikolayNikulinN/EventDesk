using EventDesk.Application.Abstractions.Persistence;
using EventDesk.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace EventDesk.Infrastructure.Persistence.Repositories;

public sealed class CalendarEventRepository(EventDeskDbContext dbContext) : ICalendarEventRepository
{
    public Task<CalendarEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.CalendarEvents.SingleOrDefaultAsync(calendarEvent => calendarEvent.Id == id, cancellationToken);

    public async Task<IReadOnlyList<CalendarEvent>> GetOverlappingAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default) =>
        await dbContext.CalendarEvents
            .AsNoTracking()
            .Where(calendarEvent => !calendarEvent.IsDeleted
                && calendarEvent.RecurrenceKind == EventRecurrenceKind.None
                && calendarEvent.StartsAtUtc < toUtc
                && calendarEvent.EndsAtUtc > fromUtc)
            .OrderBy(calendarEvent => calendarEvent.StartsAtUtc)
            .ThenBy(calendarEvent => calendarEvent.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CalendarEvent>> GetRecurringAsync(CancellationToken cancellationToken = default) =>
        await dbContext.CalendarEvents
            .AsNoTracking()
            .Where(calendarEvent => !calendarEvent.IsDeleted &&
                calendarEvent.RecurrenceKind != EventRecurrenceKind.None)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(CalendarEvent calendarEvent, CancellationToken cancellationToken = default) =>
        await dbContext.CalendarEvents.AddAsync(calendarEvent, cancellationToken);
}
