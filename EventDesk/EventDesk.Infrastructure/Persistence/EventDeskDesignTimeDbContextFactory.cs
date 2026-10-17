using EventDesk.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EventDesk.Infrastructure.Persistence;

public sealed class EventDeskDesignTimeDbContextFactory : IDesignTimeDbContextFactory<EventDeskDbContext>
{
    public EventDeskDbContext CreateDbContext(string[] args)
    {
        string databasePath = Path.Combine(
            Path.GetTempPath(),
            DatabaseConstants.ApplicationDataDirectoryName,
            DatabaseConstants.DatabaseFileName);
        DbContextOptions<EventDeskDbContext> options = new DbContextOptionsBuilder<EventDeskDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;
        return new EventDeskDbContext(options);
    }
}
