using System.Globalization;
using System.Windows.Media;
using EventDesk.Contracts.Events;
using EventDesk.Resources.Controls.Calendar;
using EventDesk.Resources.Theme;

namespace EventDesk.App.Models;

public sealed class CalendarEventItem
{
    public CalendarEventItem(CalendarEventResponse source, DateTime day, string timeRangeFormat)
    {
        Source = source;
        DateTime start = source.StartsAtUtc.ToLocalTime().DateTime;
        DateTime end = source.EndsAtUtc.ToLocalTime().DateTime;
        DateTime visibleStart = start > day ? start : day;
        DateTime visibleEnd = end < day.AddDays(1) ? end : day.AddDays(1);
        Top = visibleStart.TimeOfDay.TotalMinutes * CalendarLayout.PixelsPerMinute + CalendarLayout.EventInset;
        Height = Math.Max(CalendarLayout.MinimumEventHeight,
            (visibleEnd - visibleStart).TotalMinutes * CalendarLayout.PixelsPerMinute - 2 * CalendarLayout.EventInset);
        Title = source.Title;
        ColorBrush = CalendarColorPalette.CreateBrush(source.ColorHex);
        FillBrush = CalendarColorPalette.CreateBrush(source.ColorHex, CalendarColorPalette.EventFillAlpha);
        TimeRange = string.Format(CultureInfo.CurrentCulture, timeRangeFormat,
            source.StartsAtUtc.ToLocalTime(), source.EndsAtUtc.ToLocalTime());
    }

    public CalendarEventResponse Source { get; }

    public string Title { get; }

    public System.Windows.Media.Brush ColorBrush { get; }

    public System.Windows.Media.Brush FillBrush { get; }

    public string TimeRange { get; }

    public bool IsEnabled => Source.IsEnabled;

    public double Top { get; }

    public double Height { get; }

    public double Left { get; private set; } = CalendarLayout.EventInset;

    public double Width { get; private set; } = CalendarLayout.DayWidth - 2 * CalendarLayout.EventInset;

    public void SetLayout(double left, double width)
    {
        Left = left;
        Width = width;
    }
}
