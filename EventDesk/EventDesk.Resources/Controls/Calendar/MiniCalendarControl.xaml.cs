using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace EventDesk.Resources.Controls.Calendar;

public partial class MiniCalendarControl : UserControl
{
    public MiniCalendarControl() => InitializeComponent();

    private void MiniCalendarLoaded(object sender, RoutedEventArgs e)
    {
        CalendarItem? calendarItem = FindVisualChild<CalendarItem>(MiniCalendar);
        if (calendarItem is null)
        {
            return;
        }

        calendarItem.ApplyTemplate();
        foreach (string partName in new[] { "PART_HeaderButton", "PART_PreviousButton", "PART_NextButton" })
        {
            if (calendarItem.Template?.FindName(partName, calendarItem) is FrameworkElement button)
            {
                button.Cursor = Cursors.Hand;
            }
        }
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, index);
            if (child is T match)
            {
                return match;
            }

            if (FindVisualChild<T>(child) is { } descendant)
            {
                return descendant;
            }
        }

        return null;
    }
}
