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
            model.Description, model.ColorHex);

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
        calendarEvent.Reschedule(model.StartsAt, model.EndsAt, model.TimeZoneId);
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
        return events.Select(ToModel).ToArray();
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
        calendarEvent.ColorHex);
}
