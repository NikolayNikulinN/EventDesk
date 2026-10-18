using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Wpf.Ui.Controls;

namespace EventDesk.Resources.Behaviors;

public static class InteractiveCursorBehavior
{
    private static int isRegistered;

    public static void Register()
    {
        if (Interlocked.Exchange(ref isRegistered, 1) != 0)
        {
            return;
        }

        // Template parts and popup items are created after their parent styles are applied.
        foreach (Type type in new[] { typeof(ButtonBase), typeof(ComboBox), typeof(ListBoxItem),
                     typeof(Thumb), typeof(NavigationViewItem) })
        {
            EventManager.RegisterClassHandler(type, FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnLoaded));
        }
    }

    private static void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && IsInteractive(element))
        {
            element.SetCurrentValue(FrameworkElement.CursorProperty, Cursors.Hand);
        }
    }

    private static bool IsInteractive(FrameworkElement element) =>
        element is ButtonBase or ComboBox or ListBoxItem or Thumb or NavigationViewItem;
}
