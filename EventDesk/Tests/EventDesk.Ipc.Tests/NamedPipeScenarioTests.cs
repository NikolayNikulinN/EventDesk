using System.Collections.Concurrent;
using EventDesk.Application;
using EventDesk.Constants;
using EventDesk.Contracts.Events;
using EventDesk.Contracts.Ipc;
using EventDesk.Infrastructure;
using EventDesk.Infrastructure.Persistence;
using EventDesk.Ipc;
using EventDesk.Ipc.Client;
using EventDesk.Ipc.Server;
using EventDesk.Logger.Abstract;
using EventDesk.Logger.Enums;
using EventDesk.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace EventDesk.Ipc.Tests;

public sealed class NamedPipeScenarioTests
{
    [Fact]
    public async Task CalendarEventsRoundTripThroughNamedPipeAndService()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string directory = Path.Combine(Path.GetTempPath(), nameof(NamedPipeScenarioTests), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string databasePath = Path.Combine(directory, DatabaseConstants.DatabaseFileName);
        string pipeName = $"{IpcConstants.PipeName}.{Guid.NewGuid():N}";
        ConcurrentBag<EventDeskDbContext> requestContexts = [];

        try
        {
            HostApplicationBuilder builder = Host.CreateApplicationBuilder();
            builder.Services.AddSingleton<ILoggerWrapper, NullLogger>();
            builder.Services.AddEventDeskApplication();
            builder.Services.AddEventDeskInfrastructure(databasePath);
            builder.Services.AddScoped<IpcRequestDispatcher>(provider =>
            {
                requestContexts.Add(provider.GetRequiredService<EventDeskDbContext>());
                return ActivatorUtilities.CreateInstance<IpcRequestDispatcher>(provider);
            });
            builder.Services.AddSingleton(new NamedPipeIpcServer(
                pipeName, () => PipeServerStreamFactory.Create(pipeName)));
            builder.Services.AddHostedService<PipeServerWorker>();
            using IHost host = builder.Build();

            using (IServiceScope scope = host.Services.CreateScope())
            {
                EventDeskDbContext dbContext = scope.ServiceProvider.GetRequiredService<EventDeskDbContext>();
                await dbContext.Database.MigrateAsync(cancellationToken);
            }

            await host.StartAsync(cancellationToken);
            try
            {
                // A disconnected client must not prevent a later request from using the next pipe instance.
                await using (System.IO.Pipes.NamedPipeClientStream clientProbe = new(
                    IpcConstants.LocalServerName, pipeName, System.IO.Pipes.PipeDirection.InOut,
                    System.IO.Pipes.PipeOptions.Asynchronous))
                {
                    using CancellationTokenSource probeTimeout = new(TimeSpan.FromSeconds(2));
                    await clientProbe.ConnectAsync(probeTimeout.Token);
                }
                NamedPipeIpcClient client = new(pipeName);
                DateTimeOffset start = new(2026, 10, 19, 9, 0, 0, TimeSpan.Zero);
                SaveEventDto original = new(
                    "Planning",
                    start,
                    start.AddHours(1),
                    TimeZoneInfo.Utc.Id,
                    true,
                    [new ReminderRuleDto(TimeSpan.FromMinutes(15), ReminderChannelCode.Desktop, 1, null)]);

                CalendarEventResponse created = await client.SendAsync<CreateEventRequest, CalendarEventResponse>(
                    IpcOperation.CreateEvent, new CreateEventRequest(original), cancellationToken);
                Assert.Equal(original.Title, created.Title);
                Assert.Single(created.Reminders);

                GetEventsRequest period = new(start.AddMinutes(-1), start.AddHours(2));
                GetEventsResponse read = await client.SendAsync<GetEventsRequest, GetEventsResponse>(
                    IpcOperation.GetEvents, period, cancellationToken);
                Assert.Equal(created.Id, Assert.Single(read.Events).Id);

                SaveEventDto changed = original with
                {
                    Title = "Updated planning",
                    Reminders = [new ReminderRuleDto(TimeSpan.FromMinutes(30), ReminderChannelCode.Email, 1, null)]
                };
                CalendarEventResponse updated = await client.SendAsync<UpdateEventRequest, CalendarEventResponse>(
                    IpcOperation.UpdateEvent, new UpdateEventRequest(created.Id, changed), cancellationToken);
                Assert.Equal(changed.Title, updated.Title);
                Assert.Equal(ReminderChannelCode.Email, Assert.Single(updated.Reminders).Channel);

                DeleteEventResponse deleted = await client.SendAsync<DeleteEventRequest, DeleteEventResponse>(
                    IpcOperation.DeleteEvent, new DeleteEventRequest(created.Id), cancellationToken);
                Assert.Equal(created.Id, deleted.Id);
                GetEventsResponse afterDelete = await client.SendAsync<GetEventsRequest, GetEventsResponse>(
                    IpcOperation.GetEvents, period, cancellationToken);
                Assert.Empty(afterDelete.Events);

                IpcException invalidPeriod = await Assert.ThrowsAsync<IpcException>(() =>
                    client.SendAsync<GetEventsRequest, GetEventsResponse>(
                        IpcOperation.GetEvents,
                        new GetEventsRequest(period.To, period.From),
                        cancellationToken));
                Assert.Equal(IpcErrorCode.InvalidRequest, invalidPeriod.ErrorCode);

                IpcException missingEvent = await Assert.ThrowsAsync<IpcException>(() =>
                    client.SendAsync<DeleteEventRequest, DeleteEventResponse>(
                        IpcOperation.DeleteEvent, new DeleteEventRequest(created.Id), cancellationToken));
                Assert.Equal(IpcErrorCode.NotFound, missingEvent.ErrorCode);

                using CancellationTokenSource canceled = new();
                await canceled.CancelAsync();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                    client.SendAsync<GetEventsRequest, GetEventsResponse>(
                        IpcOperation.GetEvents, period, canceled.Token));
                Assert.True(requestContexts.Count >= 7);
                Assert.Equal(
                    requestContexts.Count,
                    requestContexts.Distinct(ReferenceEqualityComparer.Instance).Count());
            }
            finally
            {
                await host.StopAsync(CancellationToken.None);
            }
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ClientTimesOutWhileServiceIsProcessing()
    {
        string pipeName = $"{IpcConstants.PipeName}.{Guid.NewGuid():N}";
        using CancellationTokenSource stopping = new();
        NamedPipeIpcServer server = new(pipeName);
        Task serverTask = server.RunAsync(
            async (request, cancellationToken) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
                return new IpcResponse(request.CorrelationId, IpcErrorCode.None, null);
            },
            stopping.Token);

        try
        {
            NamedPipeIpcClient client = new(pipeName, requestTimeout: TimeSpan.FromMilliseconds(300));
            IpcException exception = await Assert.ThrowsAsync<IpcException>(() =>
                client.SendAsync<GetEventsRequest, GetEventsResponse>(
                    IpcOperation.GetEvents,
                    new GetEventsRequest(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1)),
                    TestContext.Current.CancellationToken));
            Assert.Equal(IpcErrorCode.TimedOut, exception.ErrorCode);
        }
        finally
        {
            await stopping.CancelAsync();
            await serverTask;
        }
    }

    [Fact]
    public async Task CancelingClientRequestCancelsServerHandler()
    {
        string pipeName = $"{IpcConstants.PipeName}.{Guid.NewGuid():N}";
        TaskCompletionSource<bool> handlerStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> handlerCanceled = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using CancellationTokenSource stopping = new();
        NamedPipeIpcServer server = new(pipeName);
        Task serverTask = server.RunAsync(
            async (request, cancellationToken) =>
            {
                handlerStarted.TrySetResult(true);
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    handlerCanceled.TrySetResult(true);
                    throw;
                }

                return new IpcResponse(request.CorrelationId, IpcErrorCode.None, null);
            },
            stopping.Token);

        try
        {
            NamedPipeIpcClient client = new(pipeName);
            using CancellationTokenSource clientCancellation = new();
            Task<GetEventsResponse> clientTask = client.SendAsync<GetEventsRequest, GetEventsResponse>(
                IpcOperation.GetEvents,
                new GetEventsRequest(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1)),
                clientCancellation.Token);
            await handlerStarted.Task.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);
            await clientCancellation.CancelAsync();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => clientTask);
            await handlerCanceled.Task.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);
        }
        finally
        {
            await stopping.CancelAsync();
            await serverTask;
        }
    }

    private sealed class NullLogger : ILoggerWrapper
    {
        public void Write(string message, params object?[] propertyValues) { }
        public void Write(string message, LogLevel level) { }
        public void Write(Exception? exception, LogLevel level, string messageTemplate, params object?[] propertyValues) { }
        public void CloseAndFlush() { }
    }
}
