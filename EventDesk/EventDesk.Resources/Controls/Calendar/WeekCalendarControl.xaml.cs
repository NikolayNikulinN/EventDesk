using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace EventDesk.Resources.Controls.Calendar;

public partial class WeekCalendarControl : UserControl
{
    public static readonly DependencyProperty DayWidthProperty = DependencyProperty.Register(
        nameof(DayWidth), typeof(double), typeof(WeekCalendarControl),
        new PropertyMetadata(CalendarLayout.DayWidth));

    public static readonly DependencyProperty HourHeightProperty = DependencyProperty.Register(
        nameof(HourHeight), typeof(double), typeof(WeekCalendarControl),
        new PropertyMetadata(CalendarLayout.HourHeight));

    public static readonly DependencyProperty TimelineHeightProperty = DependencyProperty.Register(
        nameof(TimelineHeight), typeof(double), typeof(WeekCalendarControl),
        new PropertyMetadata(CalendarLayout.DayHeight));

    public WeekCalendarControl() => InitializeComponent();

    public double DayWidth
    {
        get => (double)GetValue(DayWidthProperty);
        private set => SetValue(DayWidthProperty, value);
    }

    public double HourHeight
    {
        get => (double)GetValue(HourHeightProperty);
        private set => SetValue(HourHeightProperty, value);
    }

    public double TimelineHeight
    {
        get => (double)GetValue(TimelineHeightProperty);
        private set => SetValue(TimelineHeightProperty, value);
    }

    private void TimelineLoaded(object sender, RoutedEventArgs e)
    {
        UpdateDimensions();
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded,
            () => TimelineScrollViewer.ScrollToVerticalOffset(CalendarLayout.InitialScrollHour * HourHeight));
    }

    private void TimelineSizeChanged(object sender, SizeChangedEventArgs e) => UpdateDimensions();

    private void HeaderScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (e.HorizontalChange != 0 && Math.Abs(TimelineScrollViewer.HorizontalOffset - e.HorizontalOffset) > 0.01)
        {
            TimelineScrollViewer.ScrollToHorizontalOffset(e.HorizontalOffset);
        }
    }

    private void TimelineScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (e.HorizontalChange != 0 && Math.Abs(HeaderScrollViewer.HorizontalOffset - e.HorizontalOffset) > 0.01)
        {
            HeaderScrollViewer.ScrollToHorizontalOffset(e.HorizontalOffset);
        }

        if (e.ViewportWidthChange != 0 || e.ViewportHeightChange != 0)
        {
            UpdateDimensions();
        }
    }

    private void UpdateDimensions()
    {
        double availableWidth = TimelineScrollViewer.ViewportWidth;
        double availableHeight = TimelineScrollViewer.ViewportHeight;
        if (availableWidth <= 0 || availableHeight <= 0)
        {
            return;
        }

        // The scroll viewer reports its usable viewport after accounting for visible scroll bars.
        double dayWidth = Math.Max(CalendarLayout.DayWidth,
            (availableWidth - CalendarLayout.TimeRulerWidth - 1) / CalendarLayout.DaysPerWeek);
        double hourHeight = Math.Max(CalendarLayout.MinimumHourHeight,
            (availableHeight - 1) / CalendarLayout.HourCount);

        if (Math.Abs(DayWidth - dayWidth) > 0.01)
        {
            DayWidth = dayWidth;
        }

        if (Math.Abs(HourHeight - hourHeight) > 0.01)
        {
            HourHeight = hourHeight;
            TimelineHeight = hourHeight * CalendarLayout.HourCount;
        }
    }
}
