using EventDesk.Application.Abstractions.Persistence;
using EventDesk.Application.Events.Models;
using EventDesk.Constants;
using EventDesk.Domain.Events;
using EventDesk.Domain.Reminders;

namespace EventDesk.Application.Events;

public sealed class CalendarEventService(ICalendarEventRepository repository, IUnitOfWork unitOfWork)
{
    public async Task<CalendarEventModel> CreateAsync(SaveCalendarEventModel model, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        CalendarEvent calendarEvent = CalendarEvent.Create(
            Guid.NewGuid(), model.Title, model.StartsAt, model.EndsAt, model.TimeZoneId,
            model.Description, model.ColorHex, model.Recurrence, model.RecurrenceMonth,
            model.RecurrenceDay, model.NotificationIntervalMinutes);

        ApplyReminders(calendarEvent, model.Reminders);
        if (!model.IsEnabled)
        {
            calendarEvent.Disable();
        }

        await repository.AddAsync(calendarEvent, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToModel(calendarEvent);
    }

    public async Task<CalendarEventModel> UpdateAsync(Guid id, SaveCalendarEventModel model, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        CalendarEvent calendarEvent = await GetRequiredAsync(id, cancellationToken);
        foreach (ReminderRule reminder in calendarEvent.Reminders.ToArray())
        {
            calendarEvent.RemoveReminder(reminder);
        }

        calendarEvent.UpdateTitle(model.Title);
        calendarEvent.UpdateDetails(model.Description, model.ColorHex);
        calendarEvent.UpdateScheduleAndRecurrence(model.StartsAt, model.EndsAt, model.TimeZoneId,
            model.Recurrence, model.RecurrenceMonth, model.RecurrenceDay, model.NotificationIntervalMinutes);
        ApplyReminders(calendarEvent, model.Reminders);
        if (model.IsEnabled)
        {
            calendarEvent.Enable();
        }
        else
        {
            calendarEvent.Disable();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToModel(calendarEvent);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        CalendarEvent calendarEvent = await GetRequiredAsync(id, cancellationToken);
        calendarEvent.Delete();
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CalendarEventModel>> GetByPeriodAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
    {
        if (to <= from)
        {
            throw new ArgumentException(ApplicationValidationMessages.PeriodEndMustFollowStart, nameof(to));
        }

        IReadOnlyList<CalendarEvent> events = await repository.GetOverlappingAsync(
            from.ToUniversalTime(), to.ToUniversalTime(), cancellationToken);
        IReadOnlyList<CalendarEvent> recurring = await repository.GetRecurringAsync(cancellationToken);
        List<CalendarEventModel> result = events.Select(ToModel).ToList();
        foreach (CalendarEvent calendarEvent in recurring)
        {
            TimeZoneInfo zone = TimeZoneInfo.FindSystemTimeZoneById(calendarEvent.TimeZoneId);
            DateOnly date = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(from, zone).Date);
            DateOnly lastDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(to, zone).Date);
            while (date <= lastDate)
            {
                cancellationToken.ThrowIfCancellationRequested();
                IReadOnlyList<DateTimeOffset> notificationTimes = calendarEvent.GetRecurringOccurrenceTimes(date);
                if (notificationTimes.Count != 0 && notificationTimes[0] < to && notificationTimes[^1] >= from)
                {
                    result.Add(ToModel(calendarEvent) with
                    {
                        StartsAtUtc = notificationTimes[0],
                        EndsAtUtc = notificationTimes[^1]
                    });
                }

                if (date == lastDate)
                {
                    break;
                }

                date = date.AddDays(1);
            }
        }

        return result.OrderBy(item => item.StartsAtUtc).ThenBy(item => item.Id).ToArray();
    }

    private async Task<CalendarEvent> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
    {
        CalendarEvent? calendarEvent = await repository.GetByIdAsync(id, cancellationToken);
        if (calendarEvent is null || calendarEvent.IsDeleted)
        {
            throw new KeyNotFoundException(ApplicationValidationMessages.EventNotFound);
        }

        return calendarEvent;
    }

    private static void ApplyReminders(CalendarEvent calendarEvent, IReadOnlyList<ReminderRuleModel> reminders)
    {
        ArgumentNullException.ThrowIfNull(reminders);
        foreach (ReminderRuleModel reminder in reminders)
        {
            ArgumentNullException.ThrowIfNull(reminder);
            calendarEvent.AddReminder(ReminderRule.Create(
                reminder.LeadTime, reminder.Channel, reminder.OccurrenceCount, reminder.RepeatInterval));
        }
    }

    private static CalendarEventModel ToModel(CalendarEvent calendarEvent) => new(
        calendarEvent.Id,
        calendarEvent.Title,
        calendarEvent.StartsAtUtc,
        calendarEvent.EndsAtUtc,
        calendarEvent.TimeZoneId,
        calendarEvent.IsEnabled,
        calendarEvent.Reminders.Select(reminder => new ReminderRuleModel(reminder.LeadTime, reminder.Channel, reminder.OccurrenceCount, reminder.RepeatInterval)).ToArray(),
        calendarEvent.Description,
        calendarEvent.ColorHex,
        calendarEvent.RecurrenceKind,
        calendarEvent.RecurrenceMonth,
        calendarEvent.RecurrenceDay,
        calendarEvent.NotificationIntervalMinutes);
}
