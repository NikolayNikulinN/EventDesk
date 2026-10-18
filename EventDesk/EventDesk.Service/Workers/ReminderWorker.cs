using EventDesk.Application.Reminders;
using EventDesk.Constants;
using EventDesk.Logger.Abstract;
using EventDesk.Logger.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EventDesk.Service.Workers;

public sealed class ReminderWorker(
    IServiceScopeFactory scopes, ReminderProcessingOptions options,
    ILoggerWrapper logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ProcessCycleAsync(stoppingToken);
        using PeriodicTimer timer = new(options.PollInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
                await ProcessCycleAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task ProcessCycleAsync(CancellationToken cancellationToken)
    {
        try
        {
            using IServiceScope scope = scopes.CreateScope();
            await scope.ServiceProvider.GetRequiredService<ReminderProcessor>()
                .ProcessDueAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.Write(exception, LogLevel.Error, ReminderProcessingConstants.ProcessingFailed);
        }
    }
}
