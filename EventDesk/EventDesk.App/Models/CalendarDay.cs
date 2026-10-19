using System.Globalization;
using EventDesk.Resources.Controls.Calendar;

namespace EventDesk.App.Models;

public sealed class CalendarDay(DateTime date)
{
    public DateTime Date { get; } = date;

    public string DayName { get; } = date.ToString("ddd", CultureInfo.CurrentCulture);

    public string DayNumber { get; } = date.Day.ToString(CultureInfo.CurrentCulture);

    public bool IsToday { get; } = date.Date == DateTime.Today;

    public List<CalendarEventItem> Events { get; } = [];

    public void ArrangeEvents()
    {
        CalendarEventItem[] ordered = Events.OrderBy(item => item.Top).ThenByDescending(item => item.Height).ToArray();
        List<(CalendarEventItem Item, int Lane)> group = [];
        List<double> laneEnds = [];
        double groupEnd = 0;

        foreach (CalendarEventItem item in ordered)
        {
            if (group.Count > 0 && item.Top >= groupEnd)
            {
                ArrangeGroup(group, laneEnds.Count);
                group.Clear();
                laneEnds.Clear();
            }

            int lane = laneEnds.FindIndex(end => end <= item.Top);
            if (lane < 0)
            {
                lane = laneEnds.Count;
                laneEnds.Add(item.Top + item.Height);
            }
            else
            {
                laneEnds[lane] = item.Top + item.Height;
            }

            group.Add((item, lane));
            groupEnd = Math.Max(groupEnd, item.Top + item.Height);
        }

        if (group.Count > 0)
        {
            ArrangeGroup(group, laneEnds.Count);
        }
    }

    private static void ArrangeGroup(List<(CalendarEventItem Item, int Lane)> group, int laneCount)
    {
        double width = (CalendarLayout.DayWidth - 2 * CalendarLayout.EventInset) / laneCount;
        foreach ((CalendarEventItem item, int lane) in group)
        {
            item.SetLayout(CalendarLayout.EventInset + lane * width,
                width - (lane == laneCount - 1 ? 0 : CalendarLayout.EventInset));
        }
    }
}
