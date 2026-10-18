using EventDesk.Constants;
using EventDesk.Ipc.Abstractions;
using EventDesk.Logger.Abstract;
using EventDesk.Logger.Enums;
using Microsoft.Extensions.Hosting;

namespace EventDesk.App.Notifications;

public sealed class DesktopReminderReceiverWorker(
    IDesktopReminderServer server, TrayNotificationService tray,
    ILoggerWrapper logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await server.RunAsync(tray.ShowAsync, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.Write(exception, LogLevel.Error, ReminderProcessingConstants.NotificationServerFailed);
            throw;
        }
    }
}
