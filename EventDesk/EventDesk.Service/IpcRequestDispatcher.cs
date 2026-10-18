using System.Text.Json;
using EventDesk.Application.Events;
using EventDesk.Application.Events.Models;
using EventDesk.Constants;
using EventDesk.Contracts.Events;
using EventDesk.Contracts.Ipc;
using EventDesk.Contracts.Reminders;
using EventDesk.Application.Reminders;
using EventDesk.Domain.Reminders;
using EventDesk.Ipc.Serialization;
using EventDesk.Logger.Abstract;
using EventDesk.Logger.Enums;

namespace EventDesk.Service;

public sealed class IpcRequestDispatcher(CalendarEventService events, IReminderStore reminders, ILoggerWrapper logger)
{
    public async Task<IpcResponse> DispatchAsync(IpcRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return request.Operation switch
            {
                IpcOperation.GetEvents => await GetEventsAsync(request, cancellationToken),
                IpcOperation.CreateEvent => await CreateEventAsync(request, cancellationToken),
                IpcOperation.UpdateEvent => await UpdateEventAsync(request, cancellationToken),
                IpcOperation.DeleteEvent => await DeleteEventAsync(request, cancellationToken),
                IpcOperation.GetReminderHistory => await GetReminderHistoryAsync(request, cancellationToken),
                _ => new IpcResponse(request.CorrelationId, IpcErrorCode.InvalidRequest, null)
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.Write(IpcConstants.IpcRequestCanceledLog, request.CorrelationId);
            throw;
        }
        catch (Exception exception) when (exception is JsonException or ArgumentException)
        {
            return new IpcResponse(request.CorrelationId, IpcErrorCode.InvalidRequest, null);
        }
        catch (KeyNotFoundException)
        {
            return new IpcResponse(request.CorrelationId, IpcErrorCode.NotFound, null);
        }
        catch (Exception exception)
        {
            logger.Write(exception, LogLevel.Error, IpcConstants.IpcRequestFailedLog, request.CorrelationId);
            return new IpcResponse(request.CorrelationId, IpcErrorCode.InternalError, null);
        }
    }

    private async Task<IpcResponse> GetEventsAsync(IpcRequest request, CancellationToken cancellationToken)
    {
        GetEventsRequest payload = IpcJson.FromPayload<GetEventsRequest>(request.Payload);
        IReadOnlyList<CalendarEventModel> result = await events.GetByPeriodAsync(
            payload.From, payload.To, cancellationToken);
        return Success(request.CorrelationId, new GetEventsResponse(result.Select(ToResponse).ToArray()));
    }

    private async Task<IpcResponse> CreateEventAsync(IpcRequest request, CancellationToken cancellationToken)
    {
        CreateEventRequest payload = IpcJson.FromPayload<CreateEventRequest>(request.Payload);
        CalendarEventModel result = await events.CreateAsync(ToApplication(payload.Event), cancellationToken);
        return Success(request.CorrelationId, ToResponse(result));
    }

    private async Task<IpcResponse> UpdateEventAsync(IpcRequest request, CancellationToken cancellationToken)
    {
        UpdateEventRequest payload = IpcJson.FromPayload<UpdateEventRequest>(request.Payload);
        CalendarEventModel result = await events.UpdateAsync(
            payload.Id, ToApplication(payload.Event), cancellationToken);
        return Success(request.CorrelationId, ToResponse(result));
    }

    private async Task<IpcResponse> DeleteEventAsync(IpcRequest request, CancellationToken cancellationToken)
    {
        DeleteEventRequest payload = IpcJson.FromPayload<DeleteEventRequest>(request.Payload);
        await events.DeleteAsync(payload.Id, cancellationToken);
        return Success(request.CorrelationId, new DeleteEventResponse(payload.Id));
    }

    private async Task<IpcResponse> GetReminderHistoryAsync(IpcRequest request, CancellationToken cancellationToken)
    {
        GetReminderHistoryRequest payload = IpcJson.FromPayload<GetReminderHistoryRequest>(request.Payload);
        IReadOnlyList<ReminderHistoryItem> history = await reminders.GetHistoryAsync(payload.Limit, cancellationToken);
        return Success(request.CorrelationId, new ReminderHistoryResponse(history.Select(item =>
            new ReminderHistoryEntry(item.DeliveryId, item.EventTitle,
                (ReminderHistoryChannelCode)item.Channel,
                item.DueAtUtc, (ReminderHistoryStatusCode)item.Status, item.AttemptCount,
                item.DeliveredAtUtc, item.LastErrorCode)).ToArray()));
    }

    private static IpcResponse Success<T>(Guid correlationId, T payload) =>
        new(correlationId, IpcErrorCode.None, IpcJson.ToPayload(payload));

    private static SaveCalendarEventModel ToApplication(SaveEventDto source)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(source.Reminders);
        return new SaveCalendarEventModel(
            source.Title,
            source.StartsAt,
            source.EndsAt,
            source.TimeZoneId,
            source.IsEnabled,
            source.Reminders.Select(ToApplicationReminder).ToArray(),
            source.Description,
            source.ColorHex,
            ToDomainRecurrence(source.Recurrence),
            source.RecurrenceMonth,
            source.RecurrenceDay,
            source.NotificationIntervalMinutes);
    }

    private static ReminderRuleModel ToApplicationReminder(ReminderRuleDto rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        return new ReminderRuleModel(
            rule.LeadTime, ToDomainChannel(rule.Channel), rule.OccurrenceCount, rule.RepeatInterval);
    }

    private static CalendarEventResponse ToResponse(CalendarEventModel source) => new(
        source.Id,
        source.Title,
        source.StartsAtUtc,
        source.EndsAtUtc,
        source.TimeZoneId,
        source.IsEnabled,
        source.Reminders.Select(rule => new ReminderRuleDto(
            rule.LeadTime, ToContractChannel(rule.Channel), rule.OccurrenceCount, rule.RepeatInterval)).ToArray(),
        source.Description,
        source.ColorHex,
        ToContractRecurrence(source.Recurrence),
        source.RecurrenceMonth,
        source.RecurrenceDay,
        source.NotificationIntervalMinutes);

    private static EventDesk.Domain.Events.EventRecurrenceKind ToDomainRecurrence(EventRecurrenceCode recurrence) => recurrence switch
    {
        EventRecurrenceCode.None => EventDesk.Domain.Events.EventRecurrenceKind.None,
        EventRecurrenceCode.Yearly => EventDesk.Domain.Events.EventRecurrenceKind.Yearly,
        EventRecurrenceCode.Monthly => EventDesk.Domain.Events.EventRecurrenceKind.Monthly,
        _ => throw new ArgumentOutOfRangeException(nameof(recurrence))
    };

    private static EventRecurrenceCode ToContractRecurrence(EventDesk.Domain.Events.EventRecurrenceKind recurrence) => recurrence switch
    {
        EventDesk.Domain.Events.EventRecurrenceKind.None => EventRecurrenceCode.None,
        EventDesk.Domain.Events.EventRecurrenceKind.Yearly => EventRecurrenceCode.Yearly,
        EventDesk.Domain.Events.EventRecurrenceKind.Monthly => EventRecurrenceCode.Monthly,
        _ => throw new ArgumentOutOfRangeException(nameof(recurrence))
    };

    private static ReminderChannel ToDomainChannel(ReminderChannelCode channel) => channel switch
    {
        ReminderChannelCode.Desktop => ReminderChannel.Desktop,
        ReminderChannelCode.Email => ReminderChannel.Email,
        _ => throw new ArgumentOutOfRangeException(nameof(channel))
    };

    private static ReminderChannelCode ToContractChannel(ReminderChannel channel) => channel switch
    {
        ReminderChannel.Desktop => ReminderChannelCode.Desktop,
        ReminderChannel.Email => ReminderChannelCode.Email,
        _ => throw new ArgumentOutOfRangeException(nameof(channel))
    };
}
