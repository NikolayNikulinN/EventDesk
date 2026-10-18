using System.Windows;
using EventDesk.App.ViewModels.Pages;

namespace EventDesk.App.Views.Dialogs;

public partial class CalendarEditorDialog : Window
{
    public CalendarEditorDialog(CalendarViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
    }
}
