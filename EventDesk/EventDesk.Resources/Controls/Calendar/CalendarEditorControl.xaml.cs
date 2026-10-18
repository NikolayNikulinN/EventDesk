using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace EventDesk.Resources.Controls.Calendar;

public partial class CalendarEditorControl : UserControl
{
    public CalendarEditorControl() => InitializeComponent();

    private void EditorVisibilityChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is true)
        {
            Dispatcher.BeginInvoke(() => EventTitleTextBox.Focus(), DispatcherPriority.Input);
        }
    }
}
