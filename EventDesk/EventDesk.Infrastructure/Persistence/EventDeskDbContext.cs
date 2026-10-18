using EventDesk.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace EventDesk.Infrastructure.Persistence;

public sealed class EventDeskDbContext(DbContextOptions<EventDeskDbContext> options) : DbContext(options)
{
    public DbSet<CalendarEvent> CalendarEvents => Set<CalendarEvent>();
    public DbSet<ReminderDeliveryRecord> ReminderDeliveries => Set<ReminderDeliveryRecord>();
    public DbSet<ReminderAttemptRecord> ReminderAttempts => Set<ReminderAttemptRecord>();
    public DbSet<ReminderScheduleCursor> ReminderScheduleCursors => Set<ReminderScheduleCursor>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EventDeskDbContext).Assembly);
    }
}
