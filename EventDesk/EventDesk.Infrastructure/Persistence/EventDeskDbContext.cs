using EventDesk.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace EventDesk.Infrastructure.Persistence;

public sealed class EventDeskDbContext(DbContextOptions<EventDeskDbContext> options) : DbContext(options)
{
    public DbSet<CalendarEvent> CalendarEvents => Set<CalendarEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EventDeskDbContext).Assembly);
    }
}
