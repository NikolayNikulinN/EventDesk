using EventDesk.Contracts.Events;

namespace EventDesk.App.Services;

/// <summary>Finds a visible overlap before the editor saves. This is an advisory UI check.</summary>
public sealed class EventOverlapDetector(ICalendarEventClient eventClient)
{
    public async Task<CalendarEventResponse?> FindFirstAsync(
        SaveEventDto proposed,
        Guid? editedEventId,
        DateTime selectedDate,
        CancellationToken cancellationToken)
    {
        if (!proposed.IsEnabled)
        {
            return null;
        }

        foreach ((DateTimeOffset start, DateTimeOffset end) in GetCandidatePeriods(proposed, selectedDate))
        {
            IReadOnlyList<CalendarEventResponse> existing = await eventClient.GetByPeriodAsync(
                start, end, cancellationToken);
            CalendarEventResponse? conflict = existing.FirstOrDefault(item =>
                item.Id != editedEventId && item.IsEnabled &&
                item.StartsAtUtc < end && item.EndsAtUtc > start);
            if (conflict is not null)
            {
                return conflict;
            }
        }

        return null;
    }

    private static IEnumerable<(DateTimeOffset Start, DateTimeOffset End)> GetCandidatePeriods(
        SaveEventDto proposed, DateTime selectedDate)
    {
        if (proposed.Recurrence == EventRecurrenceCode.None)
        {
            yield return (proposed.StartsAt, proposed.EndsAt);
            yield break;
        }

        TimeZoneInfo zone = TimeZoneInfo.FindSystemTimeZoneById(proposed.TimeZoneId);
        TimeSpan first = TimeZoneInfo.ConvertTime(proposed.StartsAt, zone).TimeOfDay;
        TimeSpan last = TimeZoneInfo.ConvertTime(proposed.EndsAt, zone).TimeOfDay;
        foreach (DateTime date in GetRecurringDates(proposed, selectedDate))
        {
            DateTime localStart = date.Add(first);
            DateTime localEnd = date.Add(last);
            if (zone.IsInvalidTime(localStart) || zone.IsInvalidTime(localEnd))
            {
                continue;
            }

            yield return (WithZoneOffset(localStart, zone), WithZoneOffset(localEnd, zone));
        }
    }

    private static IEnumerable<DateTime> GetRecurringDates(SaveEventDto proposed, DateTime selectedDate)
    {
        DateTime from = selectedDate.Date;
        if (proposed.Recurrence == EventRecurrenceCode.Yearly &&
            proposed.RecurrenceMonth is { } month && proposed.RecurrenceDay is { } day)
        {
            int found = 0;
            for (int year = from.Year; year <= Math.Min(from.Year + 8, 9999) && found < 2; year++)
            {
                if (day > DateTime.DaysInMonth(year, month))
                {
                    continue;
                }

                DateTime candidate = new(year, month, day);
                if (candidate < from)
                {
                    continue;
                }

                found++;
                yield return candidate;
            }
        }
        else if (proposed.Recurrence == EventRecurrenceCode.Monthly && proposed.RecurrenceDay is { } monthlyDay)
        {
            DateTime firstMonth = new(from.Year, from.Month, 1);
            int found = 0;
            for (int offset = 0; offset < 24 && found < 12 && offset <= (9999 - firstMonth.Year) * 12 + 12 - firstMonth.Month; offset++)
            {
                DateTime currentMonth = firstMonth.AddMonths(offset);
                if (monthlyDay > DateTime.DaysInMonth(currentMonth.Year, currentMonth.Month))
                {
                    continue;
                }

                DateTime candidate = new(currentMonth.Year, currentMonth.Month, monthlyDay);
                if (candidate < from)
                {
                    continue;
                }

                found++;
                yield return candidate;
            }
        }
    }

    private static DateTimeOffset WithZoneOffset(DateTime local, TimeZoneInfo zone)
    {
        TimeSpan offset = zone.IsAmbiguousTime(local)
            ? zone.GetAmbiguousTimeOffsets(local).Max()
            : zone.GetUtcOffset(local);
        return new DateTimeOffset(local, offset);
    }
}
