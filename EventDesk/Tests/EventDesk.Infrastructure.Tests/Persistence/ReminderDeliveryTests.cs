using EventDesk.Application.Reminders;
using EventDesk.Domain.Events;
using EventDesk.Domain.Reminders;
using EventDesk.Infrastructure.Persistence;
using EventDesk.Logger.Abstract;
using EventDesk.Logger.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EventDesk.Infrastructure.Tests.Persistence;

public sealed class ReminderDeliveryTests
{
    [Fact]
    public async Task ProcessorRetriesAfterRestartAndDoesNotSendCompletedDeliveryAgain()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string databasePath = CreatePath();
        DateTimeOffset now = new(2026, 10, 18, 10, 0, 0, TimeSpan.Zero);
        ReminderProcessingOptions processing = new() { RetryDelay = TimeSpan.FromMinutes(1) };
        int deliveries = 0;
        try
        {
            DbContextOptions<EventDeskDbContext> options = Options(databasePath);
            await SeedAsync(options, now, cancellationToken);
            await using (EventDeskDbContext first = new(options))
            {
                ReminderProcessor processor = new(new ReminderStore(first),
                    new TestDispatcher((_, _) => throw new IOException()),
                    new FixedClock(now), processing, new NullLogger());
                await processor.ProcessDueAsync(cancellationToken);
            }

            await using (EventDeskDbContext restarted = new(options))
            {
                ReminderProcessor processor = new(new ReminderStore(restarted),
                    new TestDispatcher((_, _) => { deliveries++; return Task.CompletedTask; }),
                    new FixedClock(now.AddMinutes(2)), processing, new NullLogger());
                await processor.ProcessDueAsync(cancellationToken);
                Assert.Equal(ReminderDeliveryStatus.Delivered,
                    Assert.Single(await new ReminderStore(restarted)
                        .GetHistoryAsync(10, cancellationToken)).Status);
            }

            await using (EventDeskDbContext restartedAgain = new(options))
            {
                ReminderProcessor processor = new(new ReminderStore(restartedAgain),
                    new TestDispatcher((_, _) => { deliveries++; return Task.CompletedTask; }),
                    new FixedClock(now.AddMinutes(3)), processing, new NullLogger());
                await processor.ProcessDueAsync(cancellationToken);
                Assert.Equal(1, deliveries);
                Assert.Equal(2, await restartedAgain.ReminderAttempts.CountAsync(cancellationToken));
            }
        }
        finally { Cleanup(databasePath); }
    }

    [Fact]
    public async Task CompletedReminderIsNotDeliveredAgainAfterRestart()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string databasePath = CreatePath();
        DateTimeOffset now = new(2026, 10, 18, 10, 0, 0, TimeSpan.Zero);
        try
        {
            DbContextOptions<EventDeskDbContext> options = Options(databasePath);
            await SeedAsync(options, now, cancellationToken);
            Guid deliveryId;
            await using (EventDeskDbContext first = new(options))
            {
                ReminderStore store = new(first);
                await store.PrepareAsync(now, cancellationToken);
                ReminderClaim claim = (await store.ClaimAsync(now, TimeSpan.FromMinutes(2), 3,
                    cancellationToken))!;
                Assert.Equal("Prepare agenda", claim.EventDescription);
                deliveryId = claim.DeliveryId;
                Assert.True(await store.CompleteAsync(claim, now, cancellationToken));
            }

            await using (EventDeskDbContext restarted = new(options))
            {
                ReminderStore store = new(restarted);
                await store.PrepareAsync(now.AddMinutes(3), cancellationToken);
                Assert.Null(await store.ClaimAsync(now.AddMinutes(3), TimeSpan.FromMinutes(2),
                    3, cancellationToken));
                ReminderHistoryItem delivery = Assert.Single(await store.GetHistoryAsync(10, cancellationToken));
                Assert.Equal(deliveryId, delivery.DeliveryId);
                Assert.Equal(ReminderDeliveryStatus.Delivered, delivery.Status);
                Assert.Equal(1, delivery.AttemptCount);
                Assert.Equal(ReminderAttemptStatus.Succeeded,
                    Assert.Single(await restarted.ReminderAttempts.ToListAsync(cancellationToken)).Status);
            }
        }
        finally { Cleanup(databasePath); }
    }

    [Fact]
    public async Task InterruptedAttemptIsRetriedAfterLeaseExpires()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string databasePath = CreatePath();
        DateTimeOffset now = new(2026, 10, 18, 10, 0, 0, TimeSpan.Zero);
        try
        {
            DbContextOptions<EventDeskDbContext> options = Options(databasePath);
            await SeedAsync(options, now, cancellationToken);
            ReminderClaim firstClaim;
            await using (EventDeskDbContext first = new(options))
            {
                ReminderStore store = new(first);
                await store.PrepareAsync(now, cancellationToken);
                firstClaim = (await store.ClaimAsync(now, TimeSpan.FromMinutes(2), 3,
                    cancellationToken))!;
            }

            await using (EventDeskDbContext restarted = new(options))
            {
                ReminderStore store = new(restarted);
                Assert.Null(await store.ClaimAsync(now.AddMinutes(1), TimeSpan.FromMinutes(2),
                    3, cancellationToken));
                ReminderClaim retry = (await store.ClaimAsync(now.AddMinutes(3),
                    TimeSpan.FromMinutes(2), 3, cancellationToken))!;
                Assert.Equal(firstClaim.DeliveryId, retry.DeliveryId);
                Assert.Equal(2, retry.AttemptNumber);
                Assert.False(await store.CompleteAsync(firstClaim, now.AddMinutes(3), cancellationToken));
                Assert.True(await store.CompleteAsync(retry, now.AddMinutes(3), cancellationToken));
                List<ReminderAttemptRecord> attempts = await restarted.ReminderAttempts
                    .OrderBy(item => item.Number).ToListAsync(cancellationToken);
                Assert.Equal(ReminderAttemptStatus.Interrupted, attempts[0].Status);
                Assert.Equal(ReminderAttemptStatus.Succeeded, attempts[1].Status);
            }
        }
        finally { Cleanup(databasePath); }
    }

    [Fact]
    public async Task FailedDeliveryWaitsForRetryAndKeepsAttemptHistory()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string databasePath = CreatePath();
        DateTimeOffset now = new(2026, 10, 18, 10, 0, 0, TimeSpan.Zero);
        try
        {
            DbContextOptions<EventDeskDbContext> options = Options(databasePath);
            await SeedAsync(options, now, cancellationToken);
            await using EventDeskDbContext context = new(options);
            ReminderStore store = new(context);
            await store.PrepareAsync(now, cancellationToken);
            ReminderClaim first = (await store.ClaimAsync(now, TimeSpan.FromMinutes(2), 3,
                cancellationToken))!;
            Assert.True(await store.FailAsync(first, now, now.AddMinutes(5), false,
                "EndpointUnavailable", cancellationToken));
            Assert.Null(await store.ClaimAsync(now.AddMinutes(4), TimeSpan.FromMinutes(2),
                3, cancellationToken));
            ReminderClaim retry = (await store.ClaimAsync(now.AddMinutes(5),
                TimeSpan.FromMinutes(2), 3, cancellationToken))!;
            Assert.Equal(2, retry.AttemptNumber);
            Assert.True(await store.CompleteAsync(retry, now.AddMinutes(5), cancellationToken));
            Assert.Equal(2, (await store.GetHistoryAsync(10, cancellationToken))[0].AttemptCount);
            Assert.Equal(ReminderAttemptStatus.Failed,
                (await context.ReminderAttempts.OrderBy(item => item.Number)
                    .ToListAsync(cancellationToken))[0].Status);
        }
        finally { Cleanup(databasePath); }
    }

    private static async Task SeedAsync(DbContextOptions<EventDeskDbContext> options,
        DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using EventDeskDbContext context = new(options);
        await context.Database.MigrateAsync(cancellationToken);
        CalendarEvent calendarEvent = CalendarEvent.Create(Guid.NewGuid(), "Review",
            now.AddMinutes(1), now.AddMinutes(31), TimeZoneInfo.Utc.Id,
            description: "Prepare agenda");
        calendarEvent.AddReminder(ReminderRule.Create(TimeSpan.FromMinutes(5),
            ReminderChannel.Desktop));
        context.CalendarEvents.Add(calendarEvent);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static DbContextOptions<EventDeskDbContext> Options(string path) =>
        new DbContextOptionsBuilder<EventDeskDbContext>()
            .UseSqlite($"Data Source={path};Foreign Keys=True").Options;

    private static string CreatePath()
    {
        string directory = Path.Combine(Path.GetTempPath(), "EventDeskTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, "EventDesk.db");
    }

    private static void Cleanup(string path)
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class TestDispatcher(
        Func<ReminderClaim, CancellationToken, Task> dispatch) : IReminderDispatcher
    {
        public Task DispatchAsync(ReminderClaim claim, CancellationToken cancellationToken) =>
            dispatch(claim, cancellationToken);
    }

    private sealed class NullLogger : ILoggerWrapper
    {
        public void Write(string message, params object?[] propertyValues) { }
        public void Write(string message, LogLevel level) { }
        public void Write(Exception? exception, LogLevel level,
            string messageTemplate, params object?[] propertyValues) { }
        public void CloseAndFlush() { }
    }
}
