using System.IO;
using EventDesk.App.Notifications;
using EventDesk.Contracts.Reminders;
using EventDesk.Ipc.Client;
using EventDesk.Ipc.Server;
using Xunit;

namespace EventDesk.App.Tests;

public sealed class DesktopReminderDeliveryTests
{
    [Fact]
    public async Task RetriedDeliveryIsAcknowledgedWithoutDisplayingAgainAfterAppRestart()
    {
        string directory = Path.Combine(Path.GetTempPath(), nameof(DesktopReminderDeliveryTests),
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string pipeName = $"EventDesk.Reminder.Test.{Guid.NewGuid():N}";
        DesktopReminderMessage message = new(Guid.NewGuid(), Guid.NewGuid(), "Planning", "Prepare agenda");
        int displayed = 0;

        try
        {
            await RunSessionAsync(new NotificationReceiptStore(directory));
            await RunSessionAsync(new NotificationReceiptStore(directory));
            Assert.Equal(1, displayed);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }

        async Task RunSessionAsync(NotificationReceiptStore receipts)
        {
            using CancellationTokenSource stopping = new();
            NamedPipeDesktopReminderServer server = new(pipeName);
            Task serverTask = server.RunAsync((incoming, _) =>
            {
                Assert.Equal(message.EventDescription, incoming.EventDescription);
                if (receipts.TryRecord(incoming.DeliveryId)) displayed++;
                return Task.CompletedTask;
            }, stopping.Token);

            try
            {
                NamedPipeDesktopReminderClient client = new(pipeName);
                await client.SendAsync(message, TestContext.Current.CancellationToken);
            }
            finally
            {
                await stopping.CancelAsync();
                try { await serverTask; }
                catch (OperationCanceledException) when (stopping.IsCancellationRequested) { }
            }
        }
    }
}
