using EventDesk.Application.Events;
using EventDesk.Application.Events.Models;
using EventDesk.Domain.Reminders;
using EventDesk.Infrastructure.Persistence;
using EventDesk.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EventDesk.Infrastructure.Tests.Persistence;

public sealed class CalendarEventPersistenceTests
{
    [Fact]
    public async Task CreateUpdateQueryAndDeletePersistAcrossScopes()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string directory = Path.Combine(Path.GetTempPath(), "EventDeskTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string databasePath = Path.Combine(directory, "EventDesk.db");

        try
        {
            DbContextOptions<EventDeskDbContext> options = new DbContextOptionsBuilder<EventDeskDbContext>()
                .UseSqlite($"Data Source={databasePath};Foreign Keys=True")
                .Options;
            await using (EventDeskDbContext setup = new(options))
            {
                await setup.Database.MigrateAsync(cancellationToken);
            }

            DateTimeOffset start = new(2026, 10, 17, 9, 0, 0, TimeSpan.Zero);
            SaveCalendarEventModel original = new(
                "Planning",
                start,
                start.AddHours(1),
                TimeZoneInfo.Utc.Id,
                true,
                [new ReminderRuleModel(TimeSpan.FromMinutes(15), ReminderChannel.Desktop, 1, null)]);

            Guid id;
            await using (EventDeskDbContext createContext = new(options))
            {
                CalendarEventService service = CreateService(createContext);
                CalendarEventModel created = await service.CreateAsync(original, cancellationToken);
                id = created.Id;
                Assert.Equal(start, created.StartsAtUtc);
            }

            await using (EventDeskDbContext readContext = new(options))
            {
                CalendarEventService service = CreateService(readContext);
                IReadOnlyList<CalendarEventModel> matching = await service.GetByPeriodAsync(
                    start.AddMinutes(30), start.AddHours(2), cancellationToken);
                Assert.Equal(id, Assert.Single(matching).Id);
                Assert.Single(matching[0].Reminders);
                Assert.Empty(await service.GetByPeriodAsync(start.AddHours(1), start.AddHours(2), cancellationToken));
            }

            await using (EventDeskDbContext updateContext = new(options))
            {
                CalendarEventService service = CreateService(updateContext);
                SaveCalendarEventModel changed = original with
                {
                    Title = "Updated planning",
                    StartsAt = start.AddHours(2),
                    EndsAt = start.AddHours(3),
                    IsEnabled = false,
                    Reminders = [new ReminderRuleModel(TimeSpan.FromMinutes(30), ReminderChannel.Email, 1, null)]
                };
                await service.UpdateAsync(id, changed, cancellationToken);
            }

            await using (EventDeskDbContext verifyContext = new(options))
            {
                CalendarEventService service = CreateService(verifyContext);
                Assert.Empty(await service.GetByPeriodAsync(start, start.AddHours(1), cancellationToken));
                CalendarEventModel updated = Assert.Single(await service.GetByPeriodAsync(
                    start.AddHours(2), start.AddHours(3), cancellationToken));
                Assert.Equal("Updated planning", updated.Title);
                Assert.False(updated.IsEnabled);
                Assert.Equal(ReminderChannel.Email, Assert.Single(updated.Reminders).Channel);
                await service.DeleteAsync(id, cancellationToken);
            }

            await using (EventDeskDbContext deletedContext = new(options))
            {
                CalendarEventService service = CreateService(deletedContext);
                Assert.Empty(await service.GetByPeriodAsync(start, start.AddHours(4), cancellationToken));
                Assert.True((await deletedContext.CalendarEvents.SingleAsync(cancellationToken)).IsDeleted);
                Assert.Single(await deletedContext.Database.SqlQueryRaw<int>(
                    "SELECT 1 AS Value FROM ReminderRules").ToListAsync(cancellationToken));
            }
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            Directory.Delete(directory, recursive: true);
        }
    }

    private static CalendarEventService CreateService(EventDeskDbContext dbContext) =>
        new(new CalendarEventRepository(dbContext), new UnitOfWork(dbContext));
}
