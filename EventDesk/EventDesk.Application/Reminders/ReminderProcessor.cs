using EventDesk.Constants;
using EventDesk.Logger.Abstract;
using EventDesk.Logger.Enums;

namespace EventDesk.Application.Reminders;

public sealed class ReminderProcessor(
    IReminderStore store, IReminderDispatcher dispatcher, TimeProvider clock,
    ReminderProcessingOptions options, ILoggerWrapper logger)
{
    public async Task ProcessDueAsync(CancellationToken cancellationToken)
    {
        DateTimeOffset now = clock.GetUtcNow();
        await store.PrepareAsync(now, cancellationToken);
        for (int count = 0; count < options.BatchSize; count++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ReminderClaim? claim = await store.ClaimAsync(clock.GetUtcNow(),
                options.LeaseDuration, options.MaxAttempts, cancellationToken);
            if (claim is null) break;

            try
            {
                using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(options.LeaseDuration - TimeSpan.FromSeconds(5));
                await dispatcher.DispatchAsync(claim, timeout.Token);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // The lease expires after a service shutdown; the next run records the interruption.
                throw;
            }
            catch (Exception exception)
            {
                logger.Write(exception, LogLevel.Warning, ReminderProcessingConstants.DeliveryFailed,
                    claim.DeliveryId, claim.AttemptNumber);
                now = clock.GetUtcNow();
                using CancellationTokenSource finish = new(TimeSpan.FromSeconds(5));
                await store.FailAsync(claim, now, now + options.RetryDelay,
                    exception is NotSupportedException || claim.AttemptNumber >= options.MaxAttempts,
                    GetErrorCode(exception), finish.Token);
                continue;
            }

            using CancellationTokenSource completion = new(TimeSpan.FromSeconds(5));
            await store.CompleteAsync(claim, clock.GetUtcNow(), completion.Token);
        }
    }

    private static string GetErrorCode(Exception exception) => exception switch
    {
        NotSupportedException => ReminderProcessingConstants.UnsupportedChannel,
        OperationCanceledException or TimeoutException => ReminderProcessingConstants.DeliveryTimedOut,
        IOException => ReminderProcessingConstants.EndpointUnavailable,
        _ => ReminderProcessingConstants.DeliveryError
    };
}
